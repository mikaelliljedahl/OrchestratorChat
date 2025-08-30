using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using OrchestratorChat.Core.Messages;
using OrchestratorChat.Core.Orchestration;
using OrchestratorChat.SignalR.Clients;
using OrchestratorChat.SignalR.Contracts.Responses;
using OrchestratorChat.SignalR.Hubs;

namespace OrchestratorChat.SignalR.Services;

/// <summary>
/// Service for routing messages between SignalR hubs and clients
/// </summary>
public class MessageRouter : IMessageRouter
{
    private readonly IHubContext<AgentHub, IAgentClient> _agentHubContext;
    private readonly IHubContext<OrchestratorHub, IOrchestratorClient> _orchestratorHubContext;
    private readonly ILogger<MessageRouter> _logger;

    /// <summary>
    /// Initializes a new instance of MessageRouter
    /// </summary>
    public MessageRouter(
        IHubContext<AgentHub, IAgentClient> agentHubContext,
        IHubContext<OrchestratorHub, IOrchestratorClient> orchestratorHubContext,
        ILogger<MessageRouter> logger)
    {
        _agentHubContext = agentHubContext;
        _orchestratorHubContext = orchestratorHubContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task RouteAgentMessageAsync(string sessionId, AgentMessage message)
    {
        try
        {
            _logger.LogDebug("Routing agent message from {AgentId} to session {SessionId}", 
                message.AgentId, sessionId);

            var responseDto = new AgentResponseDto
            {
                AgentId = message.AgentId,
                SessionId = sessionId,
                Response = new AgentResponse
                {
                    Content = message.Content,
                    Type = ResponseType.Text,
                    IsComplete = true,
                    Metadata = message.Metadata
                }
            };

            // Send to agent subscribers
            await _agentHubContext.Clients.Group($"agent-{message.AgentId}")
                .ReceiveAgentResponse(responseDto);

            // Send to session participants
            await _agentHubContext.Clients.Group($"session-{sessionId}")
                .ReceiveAgentResponse(responseDto);

            _logger.LogDebug("Successfully routed agent message from {AgentId} to session {SessionId}", 
                message.AgentId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to route agent message from {AgentId} to session {SessionId}", 
                message.AgentId, sessionId);
        }
    }

    /// <inheritdoc />
    public async Task RouteOrchestrationUpdateAsync(string sessionId, OrchestrationProgress progress)
    {
        try
        {
            _logger.LogDebug("Routing orchestration progress update to session {SessionId}", sessionId);

            await _orchestratorHubContext.Clients.Group($"session-{sessionId}")
                .OrchestrationProgress(progress);

            _logger.LogDebug("Successfully routed orchestration progress to session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to route orchestration progress to session {SessionId}", sessionId);
        }
    }

    /// <inheritdoc />
    public async Task BroadcastToSessionAsync(string sessionId, string method, object data)
    {
        try
        {
            _logger.LogDebug("Broadcasting {Method} to session {SessionId}", method, sessionId);

            // Use the IClientProxy approach for sending custom messages
            if (method.StartsWith("Agent"))
            {
                var clients = _agentHubContext.Clients.Group($"session-{sessionId}");
                await ((IClientProxy)clients).SendAsync(method, data);
            }
            else if (method.StartsWith("Orchestration"))
            {
                var clients = _orchestratorHubContext.Clients.Group($"session-{sessionId}");
                await ((IClientProxy)clients).SendAsync(method, data);
            }
            else
            {
                // Default to orchestrator hub for generic methods
                var clients = _orchestratorHubContext.Clients.Group($"session-{sessionId}");
                await ((IClientProxy)clients).SendAsync(method, data);
            }

            _logger.LogDebug("Successfully broadcasted {Method} to session {SessionId}", method, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast {Method} to session {SessionId}", method, sessionId);
        }
    }

    /// <inheritdoc />
    public async Task RouteToolExecutionUpdateAsync(string sessionId, string agentId, ToolExecutionUpdate update)
    {
        try
        {
            _logger.LogDebug("Routing tool execution update for agent {AgentId} to session {SessionId}", 
                agentId, sessionId);

            // Send to agent subscribers
            await _agentHubContext.Clients.Group($"agent-{agentId}")
                .ToolExecutionUpdate(update);

            // Send to session participants
            await _agentHubContext.Clients.Group($"session-{sessionId}")
                .ToolExecutionUpdate(update);

            _logger.LogDebug("Successfully routed tool execution update for agent {AgentId} to session {SessionId}", 
                agentId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to route tool execution update for agent {AgentId} to session {SessionId}", 
                agentId, sessionId);
        }
    }
}