using OrchestratorChat.Core.Messages;
using OrchestratorChat.Core.Orchestration;
using OrchestratorChat.SignalR.Contracts.Responses;

namespace OrchestratorChat.SignalR.Services;

/// <summary>
/// Interface for routing messages between SignalR hubs and clients
/// </summary>
public interface IMessageRouter
{
    /// <summary>
    /// Routes an agent message to the appropriate clients
    /// </summary>
    /// <param name="sessionId">Session ID to route the message to</param>
    /// <param name="message">The agent message to route</param>
    Task RouteAgentMessageAsync(string sessionId, AgentMessage message);
    
    /// <summary>
    /// Routes orchestration progress updates to session clients
    /// </summary>
    /// <param name="sessionId">Session ID to send updates to</param>
    /// <param name="progress">Progress information</param>
    Task RouteOrchestrationUpdateAsync(string sessionId, OrchestrationProgress progress);
    
    /// <summary>
    /// Broadcasts a message to all clients in a session
    /// </summary>
    /// <param name="sessionId">Session ID to broadcast to</param>
    /// <param name="method">SignalR method name to invoke</param>
    /// <param name="data">Data to send with the message</param>
    Task BroadcastToSessionAsync(string sessionId, string method, object data);
    
    /// <summary>
    /// Routes tool execution updates to session clients
    /// </summary>
    /// <param name="sessionId">Session ID to send updates to</param>
    /// <param name="agentId">ID of the agent executing the tool</param>
    /// <param name="update">Tool execution update information</param>
    Task RouteToolExecutionUpdateAsync(string sessionId, string agentId, ToolExecutionUpdate update);
}