using OrchestratorChat.Core.Orchestration;

namespace OrchestratorChat.Core.Events;

/// <summary>
/// Event raised when orchestration execution completes
/// </summary>
public class OrchestrationCompletedEvent : IEvent
{
    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    public string Id { get; }
    
    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    public DateTime Timestamp { get; }
    
    /// <summary>
    /// Source that generated the event
    /// </summary>
    public string Source { get; }
    
    /// <summary>
    /// ID of the orchestration execution
    /// </summary>
    public string ExecutionId { get; }
    
    /// <summary>
    /// The result of the orchestration
    /// </summary>
    public OrchestrationResult Result { get; }
    
    /// <summary>
    /// Initializes a new instance of the OrchestrationCompletedEvent
    /// </summary>
    /// <param name="executionId">ID of the execution</param>
    /// <param name="result">The orchestration result</param>
    public OrchestrationCompletedEvent(string executionId, OrchestrationResult result)
    {
        Id = Guid.NewGuid().ToString();
        ExecutionId = executionId ?? throw new ArgumentNullException(nameof(executionId));
        Result = result ?? throw new ArgumentNullException(nameof(result));
        Timestamp = DateTime.UtcNow;
        Source = "Orchestrator";
    }
}