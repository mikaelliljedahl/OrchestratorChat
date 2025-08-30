namespace OrchestratorChat.Core.Tools;

/// <summary>
/// Represents a request to execute a specific tool with parameters
/// </summary>
public class ToolCall
{
    /// <summary>
    /// Unique identifier for this tool call
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Name of the tool to execute
    /// </summary>
    public string ToolName { get; init; } = string.Empty;
    
    /// <summary>
    /// Parameters to pass to the tool
    /// </summary>
    public Dictionary<string, object> Parameters { get; init; } = new();
    
    /// <summary>
    /// ID of the agent making this tool call
    /// </summary>
    public string? AgentId { get; init; }
    
    /// <summary>
    /// ID of the session this tool call belongs to
    /// </summary>
    public string? SessionId { get; init; }
    
    /// <summary>
    /// Timestamp when the tool call was created
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}