using OrchestratorChat.Core.Sessions;

namespace OrchestratorChat.SignalR.Contracts.Responses
{
    /// <summary>
    /// Response for session creation request
    /// </summary>
    public class SessionCreatedResponse
    {
        /// <summary>
        /// Whether the session was created successfully
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// ID of the created session
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// The created session object
        /// </summary>
        public Session? Session { get; set; }

        /// <summary>
        /// Error message if creation failed
        /// </summary>
        public string? Error { get; set; }
    }
}