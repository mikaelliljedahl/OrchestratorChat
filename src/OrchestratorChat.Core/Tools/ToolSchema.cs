namespace OrchestratorChat.Core.Tools;

/// <summary>
/// Defines the schema for a tool, including its parameters
/// </summary>
public class ToolSchema
{
    /// <summary>
    /// Type of the schema (typically "object")
    /// </summary>
    public string Type { get; set; } = "object";
    
    /// <summary>
    /// Dictionary of parameter names and their schemas
    /// </summary>
    public Dictionary<string, ParameterSchema> Properties { get; set; } = new();
    
    /// <summary>
    /// List of required parameter names
    /// </summary>
    public List<string> Required { get; set; } = new();
}