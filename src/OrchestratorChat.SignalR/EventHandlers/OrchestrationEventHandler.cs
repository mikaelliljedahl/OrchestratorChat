using Microsoft.Extensions.Logging;
using OrchestratorChat.Core.Orchestration;
using OrchestratorChat.SignalR.Events;
using OrchestratorChat.SignalR.Services;

namespace OrchestratorChat.SignalR.EventHandlers;

/// <summary>
/// Event handler for orchestration-related events
/// </summary>
public class OrchestrationEventHandler : IEventHandler<OrchestrationStepCompletedEvent>
{
    private readonly IMessageRouter _messageRouter;
    private readonly ILogger<OrchestrationEventHandler> _logger;

    /// <summary>
    /// Initializes a new instance of OrchestrationEventHandler
    /// </summary>
    public OrchestrationEventHandler(
        IMessageRouter messageRouter,
        ILogger<OrchestrationEventHandler> logger)
    {
        _messageRouter = messageRouter;
        _logger = logger;
    }

    /// <summary>
    /// Handles orchestration step completed events
    /// </summary>
    /// <param name="event">Orchestration step completed event</param>
    public async Task HandleAsync(OrchestrationStepCompletedEvent @event)
    {
        try
        {
            _logger.LogDebug("Handling orchestration step completed event for session {SessionId}, step {StepOrder}",
                @event.SessionId, @event.Step?.Order);

            // Create progress update from the completed step
            var progress = new OrchestrationProgress
            {
                CurrentStep = @event.Step?.Order ?? 0,
                CurrentAgent = @event.Step?.AgentId ?? string.Empty,
                CurrentTask = @event.Step?.Task ?? string.Empty,
                ElapsedTime = @event.ExecutionTime
                // Note: TotalSteps and PercentComplete would need to be calculated 
                // based on the overall orchestration plan context
            };

            // Route the orchestration progress update
            await _messageRouter.RouteOrchestrationUpdateAsync(@event.SessionId, progress);

            // Also broadcast the step completion details
            await _messageRouter.BroadcastToSessionAsync(
                @event.SessionId,
                "OrchestrationStepCompleted",
                new
                {
                    Step = @event.Step,
                    Success = @event.Success,
                    ErrorMessage = @event.ErrorMessage,
                    Output = @event.Output,
                    ExecutionTime = @event.ExecutionTime,
                    Timestamp = @event.Timestamp
                });

            _logger.LogDebug("Successfully handled orchestration step completed event for session {SessionId}", 
                @event.SessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle orchestration step completed event for session {SessionId}", 
                @event.SessionId);
        }
    }
}