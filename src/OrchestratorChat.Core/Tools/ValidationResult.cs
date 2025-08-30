namespace OrchestratorChat.Core.Tools;

/// <summary>
/// Result of validating a tool call
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Whether the validation passed
    /// </summary>
    public bool IsValid { get; set; }
    
    /// <summary>
    /// List of validation errors, if any
    /// </summary>
    public List<string> Errors { get; set; } = new();
    
    /// <summary>
    /// List of validation warnings, if any
    /// </summary>
    public List<string> Warnings { get; set; } = new();
    
    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    /// <returns>Valid ValidationResult</returns>
    public static ValidationResult Success() => new() { IsValid = true };
    
    /// <summary>
    /// Creates a failed validation result with errors
    /// </summary>
    /// <param name="errors">List of validation errors</param>
    /// <returns>Invalid ValidationResult</returns>
    public static ValidationResult Failure(params string[] errors) => new()
    {
        IsValid = false,
        Errors = new List<string>(errors)
    };
}