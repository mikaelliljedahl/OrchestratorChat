using System.ComponentModel.DataAnnotations;

namespace OrchestratorChat.Web.Models;

/// <summary>
/// Request model for OAuth callback data
/// </summary>
public class OAuthCallbackRequest
{
    /// <summary>
    /// Authorization code from OAuth provider
    /// </summary>
    [Required]
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// State parameter for CSRF protection
    /// </summary>
    [Required]
    public string State { get; set; } = string.Empty;
}