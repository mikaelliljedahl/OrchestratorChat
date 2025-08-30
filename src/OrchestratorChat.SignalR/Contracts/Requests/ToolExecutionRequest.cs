namespace OrchestratorChat.SignalR.Contracts.Requests
{
    /// <summary>
    /// Request to execute a tool on an agent
    /// </summary>
    public class ToolExecutionRequest
    {
        /// <summary>
        /// Target agent ID
        /// </summary>
        public string AgentId { get; set; } = string.Empty;

        /// <summary>
        /// Session ID for the tool execution
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Name of the tool to execute
        /// </summary>
        public string ToolName { get; set; } = string.Empty;

        /// <summary>
        /// Parameters for the tool execution
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();
    }
}