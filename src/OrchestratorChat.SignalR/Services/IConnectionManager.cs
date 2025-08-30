namespace OrchestratorChat.SignalR.Services
{
    /// <summary>
    /// Interface for managing SignalR connections
    /// </summary>
    public interface IConnectionManager
    {
        /// <summary>
        /// Add a connection for a user
        /// </summary>
        /// <param name="connectionId">The connection ID</param>
        /// <param name="userId">The user ID</param>
        void AddConnection(string connectionId, string userId);

        /// <summary>
        /// Remove a connection
        /// </summary>
        /// <param name="connectionId">The connection ID to remove</param>
        void RemoveConnection(string connectionId);

        /// <summary>
        /// Get the user ID for a connection
        /// </summary>
        /// <param name="connectionId">The connection ID</param>
        /// <returns>User ID or null if not found</returns>
        string? GetUserId(string connectionId);

        /// <summary>
        /// Get all connection IDs for a user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>List of connection IDs</returns>
        List<string> GetConnectionIds(string userId);

        /// <summary>
        /// Check if a user is online
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>True if the user has active connections</returns>
        bool IsUserOnline(string userId);

        /// <summary>
        /// Add a user to a session
        /// </summary>
        /// <param name="connectionId">The connection ID</param>
        /// <param name="sessionId">The session ID</param>
        /// <returns>True if successfully added</returns>
        Task<bool> AddUserToSessionAsync(string connectionId, string sessionId);

        /// <summary>
        /// Remove a user from a session
        /// </summary>
        /// <param name="connectionId">The connection ID</param>
        /// <param name="sessionId">The session ID</param>
        /// <returns>True if successfully removed</returns>
        Task<bool> RemoveUserFromSessionAsync(string connectionId, string sessionId);

        /// <summary>
        /// Get all sessions for a user connection
        /// </summary>
        /// <param name="connectionId">The connection ID</param>
        /// <returns>List of session IDs</returns>
        List<string> GetUserSessions(string connectionId);

        /// <summary>
        /// Get all users in a session
        /// </summary>
        /// <param name="sessionId">The session ID</param>
        /// <returns>List of connection IDs for users in the session</returns>
        List<string> GetSessionUsers(string sessionId);
    }
}