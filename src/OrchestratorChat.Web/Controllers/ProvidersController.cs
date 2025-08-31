using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using OrchestratorChat.Web.Models;
using OrchestratorChat.Web.Services;
using OrchestratorChat.Saturn.Providers.Anthropic;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace OrchestratorChat.Web.Controllers;

/// <summary>
/// Controller for provider verification endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProvidersController : ControllerBase
{
    private readonly IProviderVerificationService _providerVerificationService;
    private readonly ILogger<ProvidersController> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IHttpClientFactory _httpClientFactory;
    
    public ProvidersController(
        IProviderVerificationService providerVerificationService,
        ILogger<ProvidersController> logger,
        IMemoryCache memoryCache,
        IHttpClientFactory httpClientFactory)
    {
        _providerVerificationService = providerVerificationService;
        _logger = logger;
        _memoryCache = memoryCache;
        _httpClientFactory = httpClientFactory;
    }
    
    /// <summary>
    /// Gets the current status of all providers
    /// </summary>
    /// <returns>Provider status information</returns>
    [HttpGet("status")]
    public async Task<ActionResult<ProviderStatusResponse>> GetProviderStatus()
    {
        try
        {
            var status = await _providerVerificationService.GetProviderStatusAsync();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get provider status");
            return StatusCode(500, new { error = "Failed to retrieve provider status" });
        }
    }
    
    /// <summary>
    /// Validates and stores OpenRouter API key
    /// </summary>
    /// <param name="request">The API key validation request</param>
    /// <returns>Validation result</returns>
    [HttpPost("openrouter/validate")]
    public async Task<ActionResult<ValidationResult>> ValidateOpenRouterKey([FromBody] ValidateApiKeyRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        try
        {
            var result = await _providerVerificationService.ValidateOpenRouterKeyAsync(request.ApiKey);
            
            if (result.IsValid)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate OpenRouter API key");
            return StatusCode(500, new ValidationResult
            {
                IsValid = false,
                Status = ProviderStatus.Missing,
                ErrorMessage = "Internal server error during validation"
            });
        }
    }
    
    /// <summary>
    /// Validates and stores Anthropic API key
    /// </summary>
    /// <param name="request">The API key validation request</param>
    /// <returns>Validation result</returns>
    [HttpPost("anthropic/validate")]
    public async Task<ActionResult<ValidationResult>> ValidateAnthropicKey([FromBody] ValidateApiKeyRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        try
        {
            var result = await _providerVerificationService.ValidateAnthropicKeyAsync(request.ApiKey);
            
            if (result.IsValid)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Anthropic API key");
            return StatusCode(500, new ValidationResult
            {
                IsValid = false,
                Status = ProviderStatus.Missing,
                ErrorMessage = "Internal server error during validation"
            });
        }
    }
    
    /// <summary>
    /// Gets Anthropic authentication status
    /// </summary>
    /// <returns>Token presence, expiry, and scopes without exposing secrets</returns>
    [HttpGet("anthropic/status")]
    public async Task<ActionResult<object>> GetAnthropicStatus()
    {
        // Enforce HTTPS in production
        if (!Request.IsHttps && !HttpContext.Request.Host.Host.Contains("localhost"))
        {
            return BadRequest(new { error = "HTTPS required" });
        }
        
        try
        {
            var tokenStore = new TokenStore();
            var tokens = await tokenStore.LoadTokensAsync();
            
            if (tokens == null)
            {
                return Ok(new { connected = false, expiresAt = (string?)null, scopes = Array.Empty<string>() });
            }
            
            var isExpired = tokens.IsExpired;
            var connected = !isExpired;
            var expiresAt = tokens.ExpiresAt.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var scopes = tokens.Scope ?? Array.Empty<string>();
            
            return Ok(new { connected, expiresAt, scopes });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Anthropic status");
            return StatusCode(500, new { error = "Failed to retrieve authentication status" });
        }
    }
    
    /// <summary>
    /// Starts Anthropic OAuth flow by generating authorization URL
    /// </summary>
    /// <returns>OAuth authorization URL and state information</returns>
    [HttpPost("anthropic/start")]
    public IActionResult StartAnthropicOAuth()
    {
        // Enforce HTTPS in production
        if (!Request.IsHttps && !HttpContext.Request.Host.Host.Contains("localhost"))
        {
            return BadRequest(new { error = "HTTPS required" });
        }
        
        try
        {
            // Generate PKCE pair
            var pkcePair = PKCEGenerator.Generate();
            
            // Generate state token for CSRF protection
            var stateBytes = new byte[32];
            RandomNumberGenerator.Fill(stateBytes);
            var state = Convert.ToBase64String(stateBytes).Replace("=", "").Replace("+", "-").Replace("/", "_");
            
            // Store PKCE verifier and state in memory cache with 10-minute expiration
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };
            
            _memoryCache.Set($"pkce_{state}", pkcePair.Verifier, cacheOptions);
            _memoryCache.Set($"oauth_state_{state}", true, cacheOptions);
            
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
            
            return Ok(new { authUrl, state });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Anthropic OAuth flow");
            return StatusCode(500, new { error = "Failed to start OAuth flow" });
        }
    }
    
    /// <summary>
    /// Handles OAuth callback and completes authentication flow
    /// </summary>
    /// <param name="code">Authorization code from OAuth provider</param>
    /// <param name="state">State parameter for CSRF protection</param>
    /// <param name="error">Error parameter if OAuth failed</param>
    /// <returns>Callback redirect page</returns>
    [HttpGet("/oauth/anthropic/callback")]
    public async Task<IActionResult> AnthropicOAuthCallback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error)
    {
        // Handle OAuth errors
        if (!string.IsNullOrEmpty(error))
        {
            return Content($@"
<!DOCTYPE html>
<html>
<head>
    <title>OAuth Error</title>
</head>
<body>
    <script>
        if (window.opener) {{
            window.opener.postMessage({{ type: 'oauth-error', error: '{error}' }}, '*');
        }}
        window.close();
    </script>
</body>
</html>", "text/html");
        }
        
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            return Content($@"
<!DOCTYPE html>
<html>
<head>
    <title>OAuth Error</title>
</head>
<body>
    <script>
        if (window.opener) {{
            window.opener.postMessage({{ type: 'oauth-error', error: 'Missing required parameters' }}, '*');
        }}
        window.close();
    </script>
</body>
</html>", "text/html");
        }
        
        try
        {
            // Validate state parameter from memory cache
            if (!_memoryCache.TryGetValue($"oauth_state_{state}", out _))
            {
                return Content($@"
<!DOCTYPE html>
<html>
<head>
    <title>OAuth Error</title>
</head>
<body>
    <script>
        if (window.opener) {{
            window.opener.postMessage({{ type: 'oauth-error', error: 'Invalid or expired state parameter' }}, '*');
        }}
        window.close();
    </script>
</body>
</html>", "text/html");
            }
            
            // Get PKCE verifier from memory cache
            if (!_memoryCache.TryGetValue($"pkce_{state}", out string? pkceVerifier) || string.IsNullOrEmpty(pkceVerifier))
            {
                return Content($@"
<!DOCTYPE html>
<html>
<head>
    <title>OAuth Error</title>
</head>
<body>
    <script>
        if (window.opener) {{
            window.opener.postMessage({{ type: 'oauth-error', error: 'PKCE verifier not found or expired' }}, '*');
        }}
        window.close();
    </script>
</body>
</html>", "text/html");
            }
            
            // Exchange authorization code for tokens
            const string REDIRECT_URI = "https://console.anthropic.com/oauth/code/callback";
            var tokens = await ExchangeCodeForTokensAsync(code, pkceVerifier, REDIRECT_URI, state);
            
            if (tokens == null)
            {
                return Content($@"
<!DOCTYPE html>
<html>
<head>
    <title>OAuth Error</title>
</head>
<body>
    <script>
        if (window.opener) {{
            window.opener.postMessage({{ type: 'oauth-error', error: 'Failed to exchange authorization code for tokens' }}, '*');
        }}
        window.close();
    </script>
</body>
</html>", "text/html");
            }
            
            // Store tokens using TokenStore
            var tokenStore = HttpContext.RequestServices.GetRequiredService<OrchestratorChat.Saturn.Providers.Anthropic.ITokenStore>();
            await tokenStore.SaveTokensAsync(tokens);
            
            // Clean up cache entries
            _memoryCache.Remove($"pkce_{state}");
            _memoryCache.Remove($"oauth_state_{state}");
            
            // Return success page that closes itself and notifies opener
            return Content($@"
<!DOCTYPE html>
<html>
<head>
    <title>OAuth Success</title>
</head>
<body>
    <script>
        if (window.opener) {{
            window.opener.postMessage({{ type: 'oauth-success', message: 'Authentication successful' }}, '*');
        }}
        window.close();
    </script>
</body>
</html>", "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete Anthropic OAuth flow");
            return Content($@"
<!DOCTYPE html>
<html>
<head>
    <title>OAuth Error</title>
</head>
<body>
    <script>
        if (window.opener) {{
            window.opener.postMessage({{ type: 'oauth-error', error: 'Internal server error during OAuth completion' }}, '*');
        }}
        window.close();
    </script>
</body>
</html>", "text/html");
        }
    }

    /// <summary>
    /// Submits an authorization code manually after user copies it from Anthropic
    /// </summary>
    [HttpPost("anthropic/submit-code")]
    public async Task<IActionResult> SubmitAnthropicCode([FromBody] SubmitCodeRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request?.Code))
            {
                return BadRequest(new { error = "Authorization code is required" });
            }

            if (string.IsNullOrEmpty(request?.State))
            {
                return BadRequest(new { error = "State parameter is required" });
            }
            
            // Trim whitespace from the code in case it was copied with spaces
            var codeInput = request.Code.Trim();
            
            // Parse code and state if combined with # (like SaturnFork does)
            string code = codeInput;
            string? receivedState = null;
            
            if (codeInput.Contains("#"))
            {
                var parts = codeInput.Split('#', 2);
                code = parts[0];
                if (parts.Length > 1)
                {
                    receivedState = parts[1];
                    // Override the state from the request with the one from the code
                    if (!string.IsNullOrEmpty(receivedState))
                    {
                        request.State = receivedState;
                    }
                }
            }
            
            _logger.LogInformation("Received code for submission - Length: {Length}, First chars: {First}", 
                code.Length, code.Length > 10 ? code.Substring(0, 10) + "..." : code);

            // Validate state token
            if (!_memoryCache.TryGetValue<bool>($"oauth_state_{request.State}", out _))
            {
                _logger.LogWarning("Invalid or expired state token: {State}", request.State);
                return BadRequest(new { error = "Invalid or expired state token" });
            }

            // Get PKCE verifier from cache
            if (!_memoryCache.TryGetValue<string>($"pkce_{request.State}", out var pkceVerifier))
            {
                _logger.LogError("PKCE verifier not found for state: {State}", request.State);
                return BadRequest(new { error = "OAuth session expired" });
            }
            
            _logger.LogInformation("PKCE verifier found - Length: {Length}", pkceVerifier?.Length ?? 0);

            // Exchange authorization code for tokens
            const string REDIRECT_URI = "https://console.anthropic.com/oauth/code/callback";
            var tokens = await ExchangeCodeForTokensAsync(code, pkceVerifier, REDIRECT_URI, request.State);
            
            if (tokens == null)
            {
                _logger.LogError("Token exchange returned null");
                return BadRequest(new { error = "Failed to exchange authorization code for tokens" });
            }

            _logger.LogInformation("Token exchange successful, attempting to store tokens");
            
            try
            {
                // Store tokens
                var tokenStore = HttpContext.RequestServices.GetRequiredService<OrchestratorChat.Saturn.Providers.Anthropic.ITokenStore>();
                await tokenStore.SaveTokensAsync(tokens);
                _logger.LogInformation("Tokens stored successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store tokens");
                return StatusCode(500, new { error = $"Failed to store tokens: {ex.Message}" });
            }

            // Clean up cache
            _memoryCache.Remove($"oauth_state_{request.State}");
            _memoryCache.Remove($"pkce_{request.State}");

            return Ok(new { success = true, message = "OAuth authentication completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit Anthropic code");
            return StatusCode(500, new { error = "Failed to complete OAuth flow" });
        }
    }
    
    
    /// <summary>
    /// Clears stored Anthropic OAuth tokens
    /// </summary>
    /// <returns>Logout result</returns>
    [HttpPost("anthropic/logout")]
    public async Task<IActionResult> LogoutAnthropicOAuth()
    {
        // Enforce HTTPS in production
        if (!Request.IsHttps && !HttpContext.Request.Host.Host.Contains("localhost"))
        {
            return BadRequest(new { error = "HTTPS required" });
        }
        
        try
        {
            var tokenStore = new TokenStore();
            await tokenStore.ClearTokensAsync();
            return Ok(new { success = true, message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to logout from Anthropic OAuth");
            return StatusCode(500, new { error = "Failed to logout" });
        }
    }
    
    
    /// <summary>
    /// Exchanges authorization code for tokens
    /// </summary>
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
            
            var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
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
            
            var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(responseContent);
            
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