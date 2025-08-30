namespace OrchestratorChat.ConsoleClient.Models
{
    /// <summary>
    /// Represents a response from an agent command execution
    /// </summary>
    public class ClientResponse
    {
        /// <summary>
        /// Unique identifier matching the original command
        /// </summary>
        public string CommandId { get; set; } = string.Empty;

        /// <summary>
        /// ID of the session where the command was executed
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Status of the command execution
        /// </summary>
        public string Status { get; set; } = string.Empty; // "started", "progress", "completed", "error"

        /// <summary>
        /// Response content from the agent
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp of when this response was created
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}