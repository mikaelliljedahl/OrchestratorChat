namespace OrchestratorChat.Web.Models;

/// <summary>
/// Result of API key validation
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Whether the validation was successful
    /// </summary>
    public bool IsValid { get; set; }
    
    /// <summary>
    /// Status of the validated key
    /// </summary>
    public ProviderStatus Status { get; set; } = ProviderStatus.Missing;
    
    /// <summary>
    /// Error message if validation failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}