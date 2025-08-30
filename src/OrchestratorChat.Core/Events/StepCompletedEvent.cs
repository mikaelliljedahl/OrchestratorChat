using OrchestratorChat.Core.Orchestration;

namespace OrchestratorChat.Core.Events;

/// <summary>
/// Event raised when an orchestration step completes
/// </summary>
public class StepCompletedEvent : IEvent
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
    /// The completed step
    /// </summary>
    public OrchestrationStep Step { get; }
    
    /// <summary>
    /// Result of the step execution
    /// </summary>
    public StepResult Result { get; }
    
    /// <summary>
    /// Initializes a new instance of the StepCompletedEvent
    /// </summary>
    /// <param name="executionId">ID of the execution</param>
    /// <param name="step">The completed step</param>
    /// <param name="result">The step result</param>
    public StepCompletedEvent(string executionId, OrchestrationStep step, StepResult result)
    {
        Id = Guid.NewGuid().ToString();
        ExecutionId = executionId ?? throw new ArgumentNullException(nameof(executionId));
        Step = step ?? throw new ArgumentNullException(nameof(step));
        Result = result ?? throw new ArgumentNullException(nameof(result));
        Timestamp = DateTime.UtcNow;
        Source = "Orchestrator";
    }
}