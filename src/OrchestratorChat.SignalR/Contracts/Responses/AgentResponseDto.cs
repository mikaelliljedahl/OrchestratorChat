using OrchestratorChat.Core.Messages;

namespace OrchestratorChat.SignalR.Contracts.Responses
{
    /// <summary>
    /// DTO for agent responses sent via SignalR
    /// </summary>
    public class AgentResponseDto
    {
        /// <summary>
        /// ID of the agent that sent the response
        /// </summary>
        public string AgentId { get; set; } = string.Empty;

        /// <summary>
        /// Session ID for the response
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// The agent response
        /// </summary>
        public AgentResponse Response { get; set; } = new();

        /// <summary>
        /// Timestamp when the response was sent
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}