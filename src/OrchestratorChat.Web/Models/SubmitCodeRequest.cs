namespace OrchestratorChat.Web.Models;

/// <summary>
/// Request model for manually submitting an OAuth authorization code
/// </summary>
public class SubmitCodeRequest
{
    /// <summary>
    /// The authorization code copied from Anthropic's callback page
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// The state parameter from the original OAuth request
    /// </summary>
    public string State { get; set; } = string.Empty;
}