using OrchestratorChat.Core.Orchestration;

namespace OrchestratorChat.SignalR.Contracts.Requests
{
    /// <summary>
    /// Request to send a message for orchestration
    /// </summary>
    public class OrchestrationMessageRequest
    {
        /// <summary>
        /// Session ID for the orchestration
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// The message/goal for orchestration
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// List of agent IDs to use for orchestration
        /// </summary>
        public List<string> AgentIds { get; set; } = new();

        /// <summary>
        /// Orchestration strategy to use
        /// </summary>
        public OrchestrationStrategy Strategy { get; set; }
    }
}