namespace OrchestratorChat.SignalR.Contracts.Responses
{
    /// <summary>
    /// Information about a SignalR connection
    /// </summary>
    public class ConnectionInfo
    {
        /// <summary>
        /// Unique connection ID
        /// </summary>
        public string ConnectionId { get; set; } = string.Empty;

        /// <summary>
        /// When the connection was established
        /// </summary>
        public DateTime ConnectedAt { get; set; }
    }
}