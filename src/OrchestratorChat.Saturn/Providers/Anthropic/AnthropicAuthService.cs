using System.Text;
using System.Text.Json;
using System.Web;

namespace OrchestratorChat.Saturn.Providers.Anthropic;

/// <summary>
/// Anthropic OAuth 2.0 authentication service with PKCE support
/// </summary>
public class AnthropicAuthService : IDisposable
{
    // OAuth Configuration
    private const string CLIENT_ID = "9d1c250a-e61b-44d9-88ed-5944d1962f5e";
    private const string AUTH_URL_CLAUDE = "https://claude.ai/oauth/authorize";
    private const string AUTH_URL_CONSOLE = "https://console.anthropic.com/oauth/authorize";
    private const string TOKEN_URL = "https://console.anthropic.com/v1/oauth/token";
    private const string REDIRECT_URI = "https://console.anthropic.com/oauth/code/callback";
    private const string SCOPES = "org:create_api_key user:profile user:inference";

    private readonly HttpClient _httpClient;
    private readonly TokenStore _tokenStore;
    private bool _disposed = false;

    /// <summary>
    /// Initializes a new instance of AnthropicAuthService
    /// </summary>
    public AnthropicAuthService()
    {
        _httpClient = new HttpClient();
        _tokenStore = new TokenStore();
    }

    /// <summary>
    /// Performs OAuth 2.0 authentication flow with PKCE
    /// </summary>
    /// <param name="useClaudeMax">True to use Claude.ai auth URL, false for Console</param>
    /// <returns>True if authentication succeeded</returns>
    public async Task<bool> AuthenticateAsync(bool useClaudeMax = true)
    {
        try
        {
            // Generate PKCE pair
            var pkcePair = PKCEGenerator.Generate();
            
            // Generate state token for CSRF protection
            var stateBytes = new byte[16];
            System.Security.Cryptography.RandomNumberGenerator.Fill(stateBytes);
            var state = Convert.ToBase64String(stateBytes).Replace("=", "").Replace("+", "-").Replace("/", "_");

            // Build OAuth authorization URL
            var authUrl = useClaudeMax ? AUTH_URL_CLAUDE : AUTH_URL_CONSOLE;
            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            queryParams["client_id"] = CLIENT_ID;
            queryParams["redirect_uri"] = REDIRECT_URI;
            queryParams["response_type"] = "code";
            queryParams["scope"] = SCOPES;
            queryParams["state"] = state;
            queryParams["code_challenge"] = pkcePair.Challenge;
            queryParams["code_challenge_method"] = "S256";

            var fullAuthUrl = $"{authUrl}?{queryParams}";

            // Open browser for user authentication
            Console.WriteLine("Opening browser for authentication...");
            Console.WriteLine($"If the browser doesn't open automatically, visit: {fullAuthUrl}");
            
            if (!BrowserLauncher.OpenUrl(fullAuthUrl))
            {
                Console.WriteLine("Failed to open browser automatically. Please open the URL manually.");
            }

            // Wait for user to provide authorization code
            Console.WriteLine();
            Console.WriteLine("After authenticating, you'll be redirected to a callback URL.");
            Console.WriteLine("Copy the authorization code from the URL and paste it here:");
            Console.Write("Authorization code: ");
            
            var authorizationCode = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(authorizationCode))
            {
                Console.WriteLine("No authorization code provided.");
                return false;
            }

            // Exchange authorization code for tokens
            var tokens = await ExchangeCodeForTokensAsync(authorizationCode, pkcePair.Verifier);
            if (tokens == null)
            {
                Console.WriteLine("Failed to exchange authorization code for tokens.");
                return false;
            }

            // Store tokens securely
            await _tokenStore.SaveTokensAsync(tokens);
            
            Console.WriteLine("Authentication successful! Tokens stored securely.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Authentication failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Refreshes expired tokens using refresh token
    /// </summary>
    /// <param name="refreshToken">Refresh token to use</param>
    /// <returns>New tokens if refresh succeeded</returns>
    public async Task<StoredTokens?> RefreshTokensAsync(string refreshToken)
    {
        try
        {
            var requestBody = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["client_id"] = CLIENT_ID
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(TOKEN_URL, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Token refresh failed: {response.StatusCode} - {errorContent}");
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                Console.WriteLine("Invalid token response received.");
                return null;
            }

            var tokens = new StoredTokens
            {
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken ?? refreshToken, // Keep existing if not provided
                ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn ?? 3600),
                CreatedAt = DateTime.UtcNow,
                TokenType = tokenResponse.TokenType ?? "Bearer",
                Scope = tokenResponse.Scope?.Split(' ')
            };

            // Store refreshed tokens
            await _tokenStore.SaveTokensAsync(tokens);
            
            return tokens;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Token refresh failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets valid tokens, refreshing if necessary
    /// </summary>
    /// <returns>Valid tokens or null if authentication required</returns>
    public async Task<StoredTokens?> GetValidTokensAsync()
    {
        try
        {
            var tokens = await _tokenStore.LoadTokensAsync();
            if (tokens == null)
            {
                return null; // No tokens stored
            }

            if (!tokens.IsExpired && !tokens.NeedsRefresh)
            {
                return tokens; // Tokens are still valid
            }

            // Try to refresh tokens
            if (!string.IsNullOrEmpty(tokens.RefreshToken))
            {
                var refreshedTokens = await RefreshTokensAsync(tokens.RefreshToken);
                if (refreshedTokens != null)
                {
                    return refreshedTokens;
                }
            }

            // Refresh failed, tokens are expired
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Logs out by clearing stored tokens
    /// </summary>
    public void Logout()
    {
        try
        {
            _tokenStore.DeleteTokens();
            Console.WriteLine("Logged out successfully. Tokens cleared.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Logout failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Exchanges authorization code for access tokens
    /// </summary>
    private async Task<StoredTokens?> ExchangeCodeForTokensAsync(string authorizationCode, string codeVerifier)
    {
        try
        {
            var requestBody = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = CLIENT_ID,
                ["code"] = authorizationCode,
                ["redirect_uri"] = REDIRECT_URI,
                ["code_verifier"] = codeVerifier
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(TOKEN_URL, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Token exchange failed: {response.StatusCode} - {errorContent}");
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                Console.WriteLine("Invalid token response received.");
                return null;
            }

            return new StoredTokens
            {
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn ?? 3600),
                CreatedAt = DateTime.UtcNow,
                TokenType = tokenResponse.TokenType ?? "Bearer",
                Scope = tokenResponse.Scope?.Split(' ')
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Token exchange failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Disposes the HTTP client
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose method
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    /// Token response model for OAuth token exchange
    /// </summary>
    private class TokenResponse
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public string? TokenType { get; set; }
        public int? ExpiresIn { get; set; }
        public string? Scope { get; set; }
    }
}