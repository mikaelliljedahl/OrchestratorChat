using OrchestratorChat.Core.Messages;

namespace OrchestratorChat.Core.Sessions;

/// <summary>
/// Interface for managing chat sessions
/// </summary>
public interface ISessionManager
{
    /// <summary>
    /// Create a new session with the specified request
    /// </summary>
    /// <param name="request">Request for creating a new session</param>
    /// <returns>The created session</returns>
    Task<Session> CreateSessionAsync(CreateSessionRequest request);
    
    /// <summary>
    /// Retrieve a session by its ID
    /// </summary>
    /// <param name="sessionId">ID of the session to retrieve</param>
    /// <returns>The session if found, null otherwise</returns>
    Task<Session?> GetSessionAsync(string sessionId);
    
    /// <summary>
    /// Get the current active session
    /// </summary>
    /// <returns>The current session if found, null otherwise</returns>
    Task<Session?> GetCurrentSessionAsync();
    
    /// <summary>
    /// Get recent sessions
    /// </summary>
    /// <param name="count">Number of recent sessions to retrieve</param>
    /// <returns>List of recent sessions</returns>
    Task<List<Session>> GetRecentSessions(int count);
    
    /// <summary>
    /// Get all currently active sessions
    /// </summary>
    /// <returns>List of active sessions</returns>
    Task<List<Session>> GetActiveSessionsAsync();
    
    /// <summary>
    /// Update an existing session
    /// </summary>
    /// <param name="session">Session with updated data</param>
    /// <returns>True if update was successful, false otherwise</returns>
    Task<bool> UpdateSessionAsync(Session session);
    
    /// <summary>
    /// End a session and mark it as completed
    /// </summary>
    /// <param name="sessionId">ID of the session to end</param>
    /// <returns>True if the session was ended successfully, false otherwise</returns>
    Task<bool> EndSessionAsync(string sessionId);
    
    /// <summary>
    /// Add a message to a session
    /// </summary>
    /// <param name="sessionId">ID of the session</param>
    /// <param name="message">The message to add</param>
    /// <returns>Task representing the async operation</returns>
    Task AddMessageAsync(string sessionId, AgentMessage message);
    
    /// <summary>
    /// Update session context data
    /// </summary>
    /// <param name="sessionId">ID of the session</param>
    /// <param name="context">Context data to update</param>
    /// <returns>Task representing the async operation</returns>
    Task UpdateSessionContextAsync(string sessionId, Dictionary<string, object> context);
    
    /// <summary>
    /// Create a snapshot of a session's current state
    /// </summary>
    /// <param name="sessionId">ID of the session to snapshot</param>
    /// <returns>The created snapshot</returns>
    Task<SessionSnapshot> CreateSnapshotAsync(string sessionId);
    
    /// <summary>
    /// Restore a session from a snapshot
    /// </summary>
    /// <param name="snapshotId">ID of the snapshot to restore from</param>
    /// <returns>The restored session</returns>
    Task<Session> RestoreFromSnapshotAsync(string snapshotId);
}