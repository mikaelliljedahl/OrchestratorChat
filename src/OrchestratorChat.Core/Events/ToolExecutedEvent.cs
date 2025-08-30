using OrchestratorChat.Core.Tools;

namespace OrchestratorChat.Core.Events;

/// <summary>
/// Event raised when a tool is executed
/// </summary>
public class ToolExecutedEvent : IEvent
{
    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Source that generated the event
    /// </summary>
    public string Source { get; set; }
    
    /// <summary>
    /// The tool call that was executed
    /// </summary>
    public ToolCall Call { get; set; }
    
    /// <summary>
    /// Result of the tool execution
    /// </summary>
    public ToolExecutionResult Result { get; set; }
}