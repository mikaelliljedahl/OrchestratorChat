using OrchestratorChat.Core.Events;
using OrchestratorChat.Core.Messages;

namespace OrchestratorChat.Core.Sessions;

/// <summary>
/// Interface for session data repository operations
/// </summary>
public interface ISessionRepository
{
    Task<Session> CreateSessionAsync(Session session);
    Task<Session?> GetSessionByIdAsync(string sessionId);
    Task<List<Session>> GetRecentSessionsAsync(int count);
    Task<List<Session>> GetActiveSessionsAsync();
    Task<bool> UpdateSessionAsync(Session session);
    Task<bool> DeleteSessionAsync(string sessionId);
    Task AddMessageAsync(string sessionId, AgentMessage message);
    Task UpdateSessionContextAsync(string sessionId, Dictionary<string, object> context);
    Task<SessionSnapshot> CreateSnapshotAsync(string sessionId, SessionSnapshot snapshot);
    Task<SessionSnapshot?> GetSnapshotAsync(string snapshotId);
}

/// <summary>
/// Implementation of session management functionality
/// </summary>
public class SessionManager : ISessionManager
{
    private readonly ISessionRepository _repository;
    private readonly IEventBus _eventBus;
    private string? _currentSessionId;

    /// <summary>
    /// Initializes a new instance of the SessionManager
    /// </summary>
    /// <param name="repository">Session repository for data operations</param>
    /// <param name="eventBus">Event bus for publishing events</param>
    public SessionManager(ISessionRepository repository, IEventBus eventBus)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    /// <summary>
    /// Creates a new session with the specified request
    /// </summary>
    /// <param name="request">Request for creating a new session</param>
    /// <returns>The created session</returns>
    public async Task<Session> CreateSessionAsync(CreateSessionRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var sessionId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        var session = new Session
        {
            Id = sessionId,
            Name = request.Name,
            Type = request.Type,
            Status = SessionStatus.Active,
            CreatedAt = now,
            LastActivityAt = now,
            ParticipantAgentIds = new List<string>(request.AgentIds),
            WorkingDirectory = request.WorkingDirectory,
            Messages = new List<AgentMessage>(),
            Context = new Dictionary<string, object>()
        };

        var createdSession = await _repository.CreateSessionAsync(session);
        _currentSessionId = sessionId;

        // Publish event
        await _eventBus.PublishAsync(new SessionCreatedEvent(createdSession));

        return createdSession;
    }

    /// <summary>
    /// Retrieves a session by its ID
    /// </summary>
    /// <param name="sessionId">ID of the session to retrieve</param>
    /// <returns>The session if found, null otherwise</returns>
    public async Task<Session?> GetSessionAsync(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return null;

        return await _repository.GetSessionByIdAsync(sessionId);
    }

    /// <summary>
    /// Gets the current active session
    /// </summary>
    /// <returns>The current session if found, null otherwise</returns>
    public async Task<Session?> GetCurrentSessionAsync()
    {
        if (string.IsNullOrEmpty(_currentSessionId))
            return null;

        return await GetSessionAsync(_currentSessionId);
    }

    /// <summary>
    /// Gets recent sessions
    /// </summary>
    /// <param name="count">Number of recent sessions to retrieve</param>
    /// <returns>List of recent sessions</returns>
    public async Task<List<Session>> GetRecentSessions(int count)
    {
        if (count <= 0)
            return new List<Session>();

        return await _repository.GetRecentSessionsAsync(count);
    }

    /// <summary>
    /// Gets recent sessions asynchronously
    /// </summary>
    /// <param name="count">Number of recent sessions to retrieve</param>
    /// <returns>List of recent sessions</returns>
    public async Task<List<Session>> GetRecentSessionsAsync(int count)
    {
        return await GetRecentSessions(count);
    }

    /// <summary>
    /// Gets all currently active sessions
    /// </summary>
    /// <returns>List of active sessions</returns>
    public async Task<List<Session>> GetActiveSessionsAsync()
    {
        return await _repository.GetActiveSessionsAsync();
    }

    /// <summary>
    /// Updates an existing session
    /// </summary>
    /// <param name="session">Session with updated data</param>
    /// <returns>True if update was successful, false otherwise</returns>
    public async Task<bool> UpdateSessionAsync(Session session)
    {
        if (session == null)
            return false;

        return await _repository.UpdateSessionAsync(session);
    }

    /// <summary>
    /// Ends a session and marks it as completed
    /// </summary>
    /// <param name="sessionId">ID of the session to end</param>
    /// <returns>True if the session was ended successfully, false otherwise</returns>
    public async Task<bool> EndSessionAsync(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return false;

        // Get the session first to update it
        var session = await _repository.GetSessionByIdAsync(sessionId);
        if (session == null)
            return false;

        // Update session status
        session.Status = SessionStatus.Completed;
        session.LastActivityAt = DateTime.UtcNow;

        var success = await _repository.UpdateSessionAsync(session);
        if (success)
        {
            if (_currentSessionId == sessionId)
            {
                _currentSessionId = null;
            }

            // Publish event
            await _eventBus.PublishAsync(new SessionEndedEvent(sessionId, "Session ended by user"));
        }

        return success;
    }

    /// <summary>
    /// Adds a message to a session
    /// </summary>
    /// <param name="sessionId">ID of the session</param>
    /// <param name="message">The message to add</param>
    /// <returns>Task representing the async operation</returns>
    public async Task AddMessageAsync(string sessionId, AgentMessage message)
    {
        if (string.IsNullOrEmpty(sessionId))
            throw new ArgumentNullException(nameof(sessionId));
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        message.Timestamp = DateTime.UtcNow;
        message.SessionId = sessionId;

        await _repository.AddMessageAsync(sessionId, message);

        // Publish event
        await _eventBus.PublishAsync(new MessageAddedEvent(sessionId, message));
    }

    /// <summary>
    /// Updates session context data
    /// </summary>
    /// <param name="sessionId">ID of the session</param>
    /// <param name="context">Context data to update</param>
    /// <returns>Task representing the async operation</returns>
    public async Task UpdateSessionContextAsync(string sessionId, Dictionary<string, object> context)
    {
        if (string.IsNullOrEmpty(sessionId))
            throw new ArgumentNullException(nameof(sessionId));
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        await _repository.UpdateSessionContextAsync(sessionId, context);
    }

    /// <summary>
    /// Creates a snapshot of a session's current state
    /// </summary>
    /// <param name="sessionId">ID of the session to snapshot</param>
    /// <returns>The created snapshot</returns>
    public async Task<SessionSnapshot> CreateSnapshotAsync(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            throw new ArgumentNullException(nameof(sessionId));

        var session = await _repository.GetSessionByIdAsync(sessionId);
        if (session == null)
            throw new InvalidOperationException($"Session {sessionId} not found");

        var snapshotId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        var snapshot = new SessionSnapshot
        {
            Id = snapshotId,
            SessionId = sessionId,
            CreatedAt = now,
            Description = $"Snapshot of session {session.Name}",
            SessionState = session
        };

        return await _repository.CreateSnapshotAsync(sessionId, snapshot);
    }

    /// <summary>
    /// Restores a session from a snapshot
    /// </summary>
    /// <param name="snapshotId">ID of the snapshot to restore from</param>
    /// <returns>The restored session</returns>
    public async Task<Session> RestoreFromSnapshotAsync(string snapshotId)
    {
        if (string.IsNullOrEmpty(snapshotId))
            throw new ArgumentNullException(nameof(snapshotId));

        var snapshot = await _repository.GetSnapshotAsync(snapshotId);
        if (snapshot == null)
            throw new InvalidOperationException($"Snapshot {snapshotId} not found");

        return snapshot.SessionState;
    }

}