namespace OrchestratorChat.SignalR.Contracts.Responses
{
    /// <summary>
    /// Update about tool execution progress
    /// </summary>
    public class ToolExecutionUpdate
    {
        /// <summary>
        /// Name of the tool being executed
        /// </summary>
        public string ToolName { get; set; } = string.Empty;

        /// <summary>
        /// Current status of the execution
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Progress percentage (0.0 to 1.0)
        /// </summary>
        public double Progress { get; set; }

        /// <summary>
        /// Progress message
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}