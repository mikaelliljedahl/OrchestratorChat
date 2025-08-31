namespace OrchestratorChat.Core.Authentication;

/// <summary>
/// Service for managing Anthropic OAuth authentication flow
/// </summary>
public interface IAnthropicOAuthService
{
    /// <summary>
    /// Starts the OAuth authentication flow by generating authorization URL and storing state
    /// </summary>
    /// <returns>OAuth authorization URL and state information</returns>
    Task<OAuthStartResult> StartAuthAsync();
    
    /// <summary>
    /// Handles OAuth callback by validating state and exchanging code for tokens
    /// </summary>
    /// <param name="code">Authorization code from OAuth provider</param>
    /// <param name="state">State parameter for CSRF protection</param>
    /// <returns>Result of OAuth callback processing</returns>
    Task<OAuthCallbackResult> HandleCallbackAsync(string code, string state);
    
    /// <summary>
    /// Submits authorization code manually after user copies it from Anthropic
    /// </summary>
    /// <param name="code">Authorization code</param>
    /// <param name="state">State parameter for validation</param>
    /// <returns>Result of code submission</returns>
    Task<OAuthCallbackResult> SubmitCodeAsync(string code, string state);
    
    /// <summary>
    /// Gets the current OAuth authentication status
    /// </summary>
    /// <returns>Authentication status without exposing sensitive data</returns>
    Task<OAuthStatus> GetStatusAsync();
    
    /// <summary>
    /// Clears stored OAuth tokens (logout)
    /// </summary>
    /// <returns>Task representing the operation</returns>
    Task LogoutAsync();
}

/// <summary>
/// Result of starting OAuth flow
/// </summary>
public class OAuthStartResult
{
    /// <summary>
    /// OAuth authorization URL for user to visit
    /// </summary>
    public string AuthUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// State token for CSRF protection
    /// </summary>
    public string State { get; set; } = string.Empty;
}

/// <summary>
/// Result of OAuth callback processing
/// </summary>
public class OAuthCallbackResult
{
    /// <summary>
    /// Whether the callback was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Error message if unsuccessful
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Success message if successful
    /// </summary>
    public string? SuccessMessage { get; set; }
}

/// <summary>
/// Current OAuth authentication status
/// </summary>
public class OAuthStatus
{
    /// <summary>
    /// Whether the user is connected/authenticated
    /// </summary>
    public bool Connected { get; set; }
    
    /// <summary>
    /// Token expiration time in ISO format
    /// </summary>
    public string? ExpiresAt { get; set; }
    
    /// <summary>
    /// OAuth scopes granted
    /// </summary>
    public string[] Scopes { get; set; } = Array.Empty<string>();
}