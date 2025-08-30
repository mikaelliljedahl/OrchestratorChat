using OrchestratorChat.SignalR.Contracts.Responses;

namespace OrchestratorChat.SignalR.Clients
{
    /// <summary>
    /// Client methods for agent hub
    /// </summary>
    public interface IAgentClient
    {
        /// <summary>
        /// Delivers agent response to client
        /// </summary>
        /// <param name="response">The agent response</param>
        Task ReceiveAgentResponse(AgentResponseDto response);

        /// <summary>
        /// Updates client on agent status changes
        /// </summary>
        /// <param name="status">Agent status information</param>
        Task AgentStatusUpdate(AgentStatusDto status);

        /// <summary>
        /// Updates client on tool execution progress
        /// </summary>
        /// <param name="update">Tool execution update</param>
        Task ToolExecutionUpdate(ToolExecutionUpdate update);

        /// <summary>
        /// Notifies client of an error
        /// </summary>
        /// <param name="error">Error details</param>
        Task ReceiveError(ErrorResponse error);
    }
}