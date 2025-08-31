namespace OrchestratorChat.Core.Tools;

/// <summary>
/// Defines a tool that can be executed by agents
/// </summary>
public class ToolDefinition
{
    /// <summary>
    /// Unique name of the tool
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Human-readable description of what the tool does
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Schema defining the tool's parameters
    /// </summary>
    public ToolSchema Schema { get; set; } = new();
    
    /// <summary>
    /// Whether this tool requires user approval before execution
    /// </summary>
    public bool RequiresApproval { get; set; }
    
    /// <summary>
    /// Categories this tool belongs to for organization
    /// </summary>
    public List<string> Categories { get; set; } = new();
    
    /// <summary>
    /// Additional metadata about the tool
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}