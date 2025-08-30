using OrchestratorChat.Core.Messages;

namespace OrchestratorChat.SignalR.Contracts.Requests
{
    /// <summary>
    /// Request to send a message to a specific agent
    /// </summary>
    public class AgentMessageRequest
    {
        /// <summary>
        /// Target agent ID
        /// </summary>
        public string AgentId { get; set; } = string.Empty;

        /// <summary>
        /// Session ID for the message
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Message content
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Optional attachments
        /// </summary>
        public List<Attachment> Attachments { get; set; } = new();

        /// <summary>
        /// Optional command ID for tracking request/response correlation
        /// </summary>
        public string? CommandId { get; set; }
    }
}