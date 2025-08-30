namespace OrchestratorChat.Core.Orchestration;

/// <summary>
/// Interface for orchestrating multiple agents to achieve complex goals
/// </summary>
public interface IOrchestrator
{
    /// <summary>
    /// Create an orchestration plan based on the request
    /// </summary>
    /// <param name="request">The orchestration request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created orchestration plan</returns>
    Task<OrchestrationPlan> CreatePlanAsync(
        OrchestrationRequest request,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute an orchestration plan
    /// </summary>
    /// <param name="plan">The plan to execute</param>
    /// <param name="progress">Progress reporting callback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the orchestration</returns>
    Task<OrchestrationResult> ExecutePlanAsync(
        OrchestrationPlan plan,
        IProgress<OrchestrationProgress> progress = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate that an orchestration plan can be executed
    /// </summary>
    /// <param name="plan">The plan to validate</param>
    /// <returns>True if the plan is valid and can be executed, false otherwise</returns>
    Task<bool> ValidatePlanAsync(OrchestrationPlan plan);
    
    /// <summary>
    /// Cancel a running orchestration execution
    /// </summary>
    /// <param name="executionId">ID of the execution to cancel</param>
    /// <returns>Task representing the async operation</returns>
    Task CancelExecutionAsync(string executionId);
    
    /// <summary>
    /// Get the status of a running orchestration execution
    /// </summary>
    /// <param name="executionId">ID of the execution</param>
    /// <returns>The execution status</returns>
    Task<object?> GetExecutionStatusAsync(string executionId);
}