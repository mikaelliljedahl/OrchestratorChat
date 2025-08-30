namespace OrchestratorChat.SignalR.Contracts.Responses
{
    /// <summary>
    /// Error response sent via SignalR
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// Error message
        /// </summary>
        public string Error { get; set; } = string.Empty;

        /// <summary>
        /// Agent ID if the error is agent-specific
        /// </summary>
        public string? AgentId { get; set; }

        /// <summary>
        /// Session ID if the error is session-specific
        /// </summary>
        public string? SessionId { get; set; }

        /// <summary>
        /// Timestamp when the error occurred
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}