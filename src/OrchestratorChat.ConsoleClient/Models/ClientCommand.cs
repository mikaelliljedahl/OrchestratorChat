namespace OrchestratorChat.ConsoleClient.Models
{
    /// <summary>
    /// Represents a command sent from the HTTP API to an agent
    /// </summary>
    public class ClientCommand
    {
        /// <summary>
        /// Unique identifier for this command
        /// </summary>
        public string CommandId { get; set; } = string.Empty;

        /// <summary>
        /// ID of the agent to send the message to
        /// </summary>
        public string AgentId { get; set; } = string.Empty;

        /// <summary>
        /// The message content to send to the agent
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Working directory for the command context
        /// </summary>
        public string WorkingDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Additional metadata for the command
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}