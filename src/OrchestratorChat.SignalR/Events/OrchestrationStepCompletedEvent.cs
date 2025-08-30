using OrchestratorChat.Core.Events;
using OrchestratorChat.Core.Orchestration;

namespace OrchestratorChat.SignalR.Events;

/// <summary>
/// Event raised when an orchestration step completes
/// </summary>
public class OrchestrationStepCompletedEvent : IEvent
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
    public string Source { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the session this step belongs to
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
    
    /// <summary>
    /// The orchestration step that completed
    /// </summary>
    public OrchestrationStep Step { get; set; } = new();
    
    /// <summary>
    /// Whether the step completed successfully
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Error message if the step failed
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// Output from the step execution
    /// </summary>
    public string Output { get; set; } = string.Empty;
    
    /// <summary>
    /// Time taken to execute the step
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }
}