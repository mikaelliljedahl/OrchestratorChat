namespace OrchestratorChat.Web.Models;

/// <summary>
/// Status of Anthropic OAuth authentication
/// </summary>
public class AnthropicOAuthStatus
{
    /// <summary>
    /// Whether the user is currently authenticated
    /// </summary>
    public bool Connected { get; set; }
    
    /// <summary>
    /// Token expiration timestamp (ISO 8601 format)
    /// </summary>
    public string? ExpiresAt { get; set; }
    
    /// <summary>
    /// Granted OAuth scopes
    /// </summary>
    public string[] Scopes { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Whether the authentication is valid (connected and not expired)
    /// </summary>
    public bool IsValid => Connected;
}