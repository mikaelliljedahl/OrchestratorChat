namespace OrchestratorChat.Core.Configuration;

/// <summary>
/// Security configuration settings
/// </summary>
public class SecuritySettings
{
    /// <summary>
    /// Whether authentication is required
    /// </summary>
    public bool RequireAuthentication { get; set; } = true;
    
    /// <summary>
    /// JWT secret key
    /// </summary>
    public string JwtSecret { get; set; } = string.Empty;
    
    /// <summary>
    /// Token expiration time in minutes
    /// </summary>
    public int TokenExpirationMinutes { get; set; } = 60;
    
    /// <summary>
    /// List of allowed origins for CORS
    /// </summary>
    public List<string> AllowedOrigins { get; set; } = new();
}