using System.ComponentModel.DataAnnotations;

namespace OrchestratorChat.Web.Models;

/// <summary>
/// Request model for API key validation
/// </summary>
public class ValidateApiKeyRequest
{
    /// <summary>
    /// The API key to validate and store
    /// </summary>
    [Required]
    public string ApiKey { get; set; } = string.Empty;
}