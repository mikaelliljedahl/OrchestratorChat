namespace OrchestratorChat.Core.Tools;

/// <summary>
/// Defines the schema for a tool parameter
/// </summary>
public class ParameterSchema
{
    /// <summary>
    /// Type of the parameter (e.g., "string", "number", "boolean")
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Human-readable description of the parameter
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Default value for the parameter
    /// </summary>
    public object? Default { get; set; }
    
    /// <summary>
    /// List of allowed values for enum-type parameters
    /// </summary>
    public List<object>? Enum { get; set; }
    
    /// <summary>
    /// Validation pattern for string parameters
    /// </summary>
    public object? Pattern { get; set; }
}