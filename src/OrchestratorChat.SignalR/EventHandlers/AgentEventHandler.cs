using Microsoft.Extensions.Logging;
using OrchestratorChat.Core.Events;
using OrchestratorChat.SignalR.Services;

namespace OrchestratorChat.SignalR.EventHandlers;

/// <summary>
/// Event handler for agent-related events
/// </summary>
public class AgentEventHandler : IEventHandler<AgentStatusChangedEvent>, IEventHandler<MessageReceivedEvent>
{
    private readonly IMessageRouter _messageRouter;
    private readonly ILogger<AgentEventHandler> _logger;

    /// <summary>
    /// Initializes a new instance of AgentEventHandler
    /// </summary>
    public AgentEventHandler(
        IMessageRouter messageRouter,
        ILogger<AgentEventHandler> logger)
    {
        _messageRouter = messageRouter;
        _logger = logger;
    }

    /// <summary>
    /// Handles agent status change events
    /// </summary>
    /// <param name="event">Agent status changed event</param>
    public async Task HandleAsync(AgentStatusChangedEvent @event)
    {
        try
        {
            _logger.LogDebug("Handling agent status change event for agent {AgentId}: {OldStatus} -> {NewStatus}",
                @event.AgentId, @event.OldStatus, @event.NewStatus);

            // Route status update to all sessions where this agent is involved
            // For now, we'll broadcast to the agent's group - in a real implementation,
            // you might track which sessions the agent belongs to
            await _messageRouter.BroadcastToSessionAsync(
                "global", // Could be enhanced to track specific sessions per agent
                "AgentStatusUpdate",
                new
                {
                    AgentId = @event.AgentId,
                    OldStatus = @event.OldStatus.ToString(),
                    NewStatus = @event.NewStatus.ToString(),
                    Reason = @event.Reason,
                    Timestamp = @event.Timestamp
                });

            _logger.LogDebug("Successfully handled agent status change for agent {AgentId}", @event.AgentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle agent status change event for agent {AgentId}", @event.AgentId);
        }
    }

    /// <summary>
    /// Handles message received events
    /// </summary>
    /// <param name="event">Message received event</param>
    public async Task HandleAsync(MessageReceivedEvent @event)
    {
        try
        {
            _logger.LogDebug("Handling message received event for session {SessionId} from agent {AgentId}",
                @event.SessionId, @event.Message.AgentId);

            // Route the message to the appropriate session participants
            await _messageRouter.RouteAgentMessageAsync(@event.SessionId, @event.Message);

            _logger.LogDebug("Successfully handled message received event for session {SessionId}", @event.SessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle message received event for session {SessionId}", @event.SessionId);
        }
    }
}