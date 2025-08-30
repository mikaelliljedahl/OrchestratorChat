namespace OrchestratorChat.SignalR.Contracts.Responses
{
    /// <summary>
    /// Response for tool execution request
    /// </summary>
    public class ToolExecutionResponse
    {
        /// <summary>
        /// Whether the tool execution was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Output from the tool execution
        /// </summary>
        public string? Output { get; set; }

        /// <summary>
        /// Error message if execution failed
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Time taken to execute the tool
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }
    }
}