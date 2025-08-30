using OrchestratorChat.Core.Agents;

namespace OrchestratorChat.SignalR.Contracts.Responses
{
    /// <summary>
    /// DTO for agent status updates sent via SignalR
    /// </summary>
    public class AgentStatusDto
    {
        /// <summary>
        /// ID of the agent
        /// </summary>
        public string AgentId { get; set; } = string.Empty;

        /// <summary>
        /// Current status of the agent
        /// </summary>
        public AgentStatus Status { get; set; }

        /// <summary>
        /// Agent capabilities
        /// </summary>
        public AgentCapabilities? Capabilities { get; set; }

        /// <summary>
        /// Timestamp of the status update
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}