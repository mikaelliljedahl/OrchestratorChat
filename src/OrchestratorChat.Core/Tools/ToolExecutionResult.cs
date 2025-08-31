namespace OrchestratorChat.Core.Tools;

/// <summary>
/// Result of executing a tool
/// </summary>
public class ToolExecutionResult
{
    /// <summary>
    /// Whether the tool executed successfully
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Output produced by the tool
    /// </summary>
    public string Output { get; set; } = string.Empty;
    
    /// <summary>
    /// Error message if execution failed
    /// </summary>
    public string Error { get; set; } = string.Empty;
    
    /// <summary>
    /// Time taken to execute the tool
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }
    
    /// <summary>
    /// Additional metadata about the execution
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}