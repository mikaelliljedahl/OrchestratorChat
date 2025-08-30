using OrchestratorChat.Core.Orchestration;

namespace OrchestratorChat.Core.Events;

/// <summary>
/// Event raised when orchestration execution starts
/// </summary>
public class OrchestrationStartedEvent : IEvent
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
    /// The orchestration plan being executed
    /// </summary>
    public OrchestrationPlan Plan { get; }
    
    /// <summary>
    /// Initializes a new instance of the OrchestrationStartedEvent
    /// </summary>
    /// <param name="executionId">ID of the execution</param>
    /// <param name="plan">The orchestration plan</param>
    public OrchestrationStartedEvent(string executionId, OrchestrationPlan plan)
    {
        Id = Guid.NewGuid().ToString();
        ExecutionId = executionId ?? throw new ArgumentNullException(nameof(executionId));
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        Timestamp = DateTime.UtcNow;
        Source = "Orchestrator";
    }
}