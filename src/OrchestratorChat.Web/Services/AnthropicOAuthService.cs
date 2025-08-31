using Microsoft.Extensions.Caching.Memory;
using OrchestratorChat.Core.Authentication;
using OrchestratorChat.Saturn.Providers.Anthropic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

namespace OrchestratorChat.Web.Services;

/// <summary>
/// Service for managing Anthropic OAuth authentication flow
/// </summary>
public class AnthropicOAuthService : IAnthropicOAuthService
{
    private readonly IOAuthStateStore _stateStore;
    private readonly ITokenStore _tokenStore;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AnthropicOAuthService> _logger;

    public AnthropicOAuthService(
        IOAuthStateStore stateStore,
        ITokenStore tokenStore,
        IHttpClientFactory httpClientFactory,
        ILogger<AnthropicOAuthService> logger)
    {
        _stateStore = stateStore;
        _tokenStore = tokenStore;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Starts OAuth flow with server-side PKCE generation and secure state storage
    /// SECURITY: All cryptographic material is generated server-side and never exposed to client
    /// </summary>
    /// <returns>Authorization URL and state for client redirection</returns>
    public async Task<OAuthStartResult> StartAuthAsync()
    {
        try
        {
            // Generate PKCE pair server-side (never trust client-provided verifiers)
            var pkcePair = PKCEGenerator.Generate();
            _logger.LogInformation("SECURITY: Generated new PKCE verifier server-side. Challenge length: {ChallengeLength}", 
                pkcePair.Challenge?.Length ?? 0);

            // Generate state token for CSRF protection
            var stateBytes = new byte[32];
            RandomNumberGenerator.Fill(stateBytes);
            var state = Convert.ToBase64String(stateBytes).Replace("=", "").Replace("+", "-").Replace("/", "_");

            // Store PKCE verifier and state with 10-minute expiration
            var stateData = new OAuthStateData
            {
                PkceVerifier = pkcePair.Verifier,
                CreatedAt = DateTime.UtcNow
            };

            await _stateStore.StoreAsync(state, stateData, TimeSpan.FromMinutes(10));
            _logger.LogInformation("SECURITY: Stored OAuth state and PKCE verifier server-side. State: {State}, Expires in 10 minutes", state);

            // Build OAuth authorization URL with Anthropic's official callback
            const string CLIENT_ID = "9d1c250a-e61b-44d9-88ed-5944d1962f5e";
            const string AUTH_URL = "https://claude.ai/oauth/authorize";
            const string REDIRECT_URI = "https://console.anthropic.com/oauth/code/callback";
            const string SCOPES = "org:create_api_key user:profile user:inference";

            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            queryParams["client_id"] = CLIENT_ID;
            queryParams["redirect_uri"] = REDIRECT_URI;
            queryParams["response_type"] = "code";
            queryParams["scope"] = SCOPES;
            queryParams["state"] = state;
            queryParams["code_challenge"] = pkcePair.Challenge;
            queryParams["code_challenge_method"] = "S256";

            var authUrl = $"{AUTH_URL}?{queryParams}";

            return new OAuthStartResult
            {
                AuthUrl = authUrl,
                State = state
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Anthropic OAuth flow");
            throw;
        }
    }

    /// <summary>
    /// Handles OAuth callback with mandatory state validation and one-time PKCE verifier use
    /// SECURITY: Validates state against server-stored value and uses server-stored PKCE verifier
    /// </summary>
    /// <param name="code">Authorization code from OAuth provider</param>
    /// <param name="state">State parameter that must match server-stored value</param>
    /// <returns>Callback result with success/failure status</returns>
    public async Task<OAuthCallbackResult> HandleCallbackAsync(string code, string state)
    {
        try
        {
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                return new OAuthCallbackResult
                {
                    Success = false,
                    ErrorMessage = "Missing required parameters"
                };
            }

            // Validate state parameter and get PKCE verifier
            var stateData = await _stateStore.RetrieveAsync(state);
            if (stateData == null)
            {
                _logger.LogWarning("SECURITY: OAuth state validation failed - invalid or expired state parameter. State: {State}, Remote IP: {RemoteIP}", 
                    state, "Unknown"); // IP would be available in HTTP context in real scenarios
                return new OAuthCallbackResult
                {
                    Success = false,
                    ErrorMessage = "Invalid or expired state parameter"
                };
            }

            _logger.LogInformation("SECURITY: OAuth state validation successful for state: {State}", state);

            // Exchange authorization code for tokens
            const string REDIRECT_URI = "https://console.anthropic.com/oauth/code/callback";
            var tokens = await ExchangeCodeForTokensAsync(code, stateData.PkceVerifier ?? string.Empty, REDIRECT_URI, state);

            if (tokens == null)
            {
                return new OAuthCallbackResult
                {
                    Success = false,
                    ErrorMessage = "Failed to exchange authorization code for tokens"
                };
            }

            // Store tokens using TokenStore
            await _tokenStore.SaveTokensAsync(tokens);

            // Clean up state (already removed by RetrieveAsync for one-time use)
            // Additional cleanup for safety
            await _stateStore.RemoveAsync(state);
            _logger.LogInformation("SECURITY: OAuth state cleaned up after successful token exchange. State: {State}", state);

            return new OAuthCallbackResult
            {
                Success = true,
                SuccessMessage = "Authentication successful"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle Anthropic OAuth callback");
            return new OAuthCallbackResult
            {
                Success = false,
                ErrorMessage = "Internal server error during OAuth completion"
            };
        }
    }

    /// <summary>
    /// Handles manual code submission with mandatory state validation and one-time PKCE verifier use
    /// SECURITY: Validates state against server-stored value and uses server-stored PKCE verifier only
    /// </summary>
    /// <param name="code">Authorization code from OAuth provider (manually copied by user)</param>
    /// <param name="state">State parameter that must match server-stored value</param>
    /// <returns>Submission result with success/failure status</returns>
    public async Task<OAuthCallbackResult> SubmitCodeAsync(string code, string state)
    {
        try
        {
            if (string.IsNullOrEmpty(code))
            {
                return new OAuthCallbackResult
                {
                    Success = false,
                    ErrorMessage = "Authorization code is required"
                };
            }

            if (string.IsNullOrEmpty(state))
            {
                return new OAuthCallbackResult
                {
                    Success = false,
                    ErrorMessage = "State parameter is required"
                };
            }

            // Trim whitespace from the code in case it was copied with spaces
            var codeInput = code.Trim();

            // Parse code and state if combined with # (like SaturnFork does)
            string actualCode = codeInput;
            string? receivedState = null;

            if (codeInput.Contains("#"))
            {
                var parts = codeInput.Split('#', 2);
                actualCode = parts[0];
                if (parts.Length > 1)
                {
                    receivedState = parts[1];
                    // Override the state with the one from the code
                    if (!string.IsNullOrEmpty(receivedState))
                    {
                        state = receivedState;
                    }
                }
            }

            _logger.LogInformation("Received code for submission - Length: {Length}, First chars: {First}",
                actualCode.Length, actualCode.Length > 10 ? actualCode.Substring(0, 10) + "..." : actualCode);

            // Validate state token and get PKCE verifier
            var stateData = await _stateStore.RetrieveAsync(state);
            if (stateData == null)
            {
                _logger.LogWarning("SECURITY: OAuth state validation failed - invalid or expired state token. State: {State}, Code Length: {CodeLength}", 
                    state, actualCode?.Length ?? 0);
                return new OAuthCallbackResult
                {
                    Success = false,
                    ErrorMessage = "Invalid or expired state token"
                };
            }

            _logger.LogInformation("SECURITY: OAuth state validation successful for manual code submission. State: {State}", state);

            _logger.LogInformation("PKCE verifier found - Length: {Length}", stateData.PkceVerifier?.Length ?? 0);

            // Exchange authorization code for tokens
            const string REDIRECT_URI = "https://console.anthropic.com/oauth/code/callback";
            var tokens = await ExchangeCodeForTokensAsync(actualCode, stateData.PkceVerifier ?? string.Empty, REDIRECT_URI, state);

            if (tokens == null)
            {
                _logger.LogError("Token exchange returned null");
                return new OAuthCallbackResult
                {
                    Success = false,
                    ErrorMessage = "Failed to exchange authorization code for tokens"
                };
            }

            _logger.LogInformation("Token exchange successful, attempting to store tokens");

            try
            {
                // Store tokens
                await _tokenStore.SaveTokensAsync(tokens);
                _logger.LogInformation("Tokens stored successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store tokens");
                return new OAuthCallbackResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to store tokens: {ex.Message}"
                };
            }

            // Clean up cache (already removed by RetrieveAsync for one-time use)
            // Additional cleanup for safety
            await _stateStore.RemoveAsync(state);
            _logger.LogInformation("SECURITY: OAuth state cleaned up after successful manual token exchange. State: {State}", state);

            return new OAuthCallbackResult
            {
                Success = true,
                SuccessMessage = "OAuth authentication completed successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit Anthropic code");
            return new OAuthCallbackResult
            {
                Success = false,
                ErrorMessage = "Failed to complete OAuth flow"
            };
        }
    }

    public async Task<OAuthStatus> GetStatusAsync()
    {
        try
        {
            var tokens = await _tokenStore.LoadTokensAsync();

            if (tokens == null)
            {
                return new OAuthStatus
                {
                    Connected = false,
                    ExpiresAt = null,
                    Scopes = Array.Empty<string>()
                };
            }

            var isExpired = tokens.IsExpired;
            var connected = !isExpired;
            var expiresAt = tokens.ExpiresAt.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var scopes = tokens.Scope ?? Array.Empty<string>();

            return new OAuthStatus
            {
                Connected = connected,
                ExpiresAt = expiresAt,
                Scopes = scopes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Anthropic status");
            throw;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _tokenStore.ClearTokensAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to logout from Anthropic OAuth");
            throw;
        }
    }

    /// <summary>
    /// Exchanges authorization code for tokens using server-stored PKCE verifier
    /// SECURITY: This method ONLY accepts server-generated PKCE verifiers - never client-provided ones
    /// </summary>
    /// <param name="authorizationCode">OAuth authorization code from provider</param>
    /// <param name="codeVerifier">Server-generated PKCE verifier (retrieved from secure storage)</param>
    /// <param name="redirectUri">OAuth redirect URI</param>
    /// <param name="state">State parameter for additional verification</param>
    /// <returns>Stored tokens if exchange successful, null otherwise</returns>
    private async Task<StoredTokens?> ExchangeCodeForTokensAsync(string authorizationCode, string codeVerifier, string redirectUri, string? state = null)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();

            const string CLIENT_ID = "9d1c250a-e61b-44d9-88ed-5944d1962f5e";
            const string TOKEN_URL = "https://console.anthropic.com/v1/oauth/token";

            // Use anonymous object like SaturnFork does
            var requestBody = new
            {
                grant_type = "authorization_code",
                code = authorizationCode,
                state = state ?? string.Empty,
                client_id = CLIENT_ID,
                redirect_uri = redirectUri,
                code_verifier = codeVerifier
            };

            var json = JsonSerializer.Serialize(requestBody);
            _logger.LogInformation("Token exchange request: {Json}", json);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(TOKEN_URL, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Token exchange failed: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
                _logger.LogError("Request details - Code length: {CodeLength}, State: {State}, RedirectUri: {RedirectUri}",
                    authorizationCode?.Length ?? 0, state ?? "null", redirectUri);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Token exchange response: {Response}", responseContent);

            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                _logger.LogWarning("Invalid token response received - AccessToken is null or empty");
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
            _logger.LogError(ex, "Token exchange failed");
            return null;
        }
    }

    /// <summary>
    /// Token response model for OAuth token exchange
    /// </summary>
    private class TokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("scope")]
        public string? Scope { get; set; }
    }
}