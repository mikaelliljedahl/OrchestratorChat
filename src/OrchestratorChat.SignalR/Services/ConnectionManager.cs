using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace OrchestratorChat.SignalR.Services
{
    /// <summary>
    /// Implementation of connection management for SignalR
    /// </summary>
    public class ConnectionManager : IConnectionManager
    {
        private readonly ConcurrentDictionary<string, string> _connectionToUser = new();
        private readonly ConcurrentDictionary<string, HashSet<string>> _userToConnections = new();
        private readonly ConcurrentDictionary<string, HashSet<string>> _userSessions = new();
        private readonly ConcurrentDictionary<string, HashSet<string>> _sessionUsers = new();
        private readonly ILogger<ConnectionManager> _logger;

        /// <summary>
        /// Initializes a new instance of ConnectionManager
        /// </summary>
        /// <param name="logger">Logger instance</param>
        public ConnectionManager(ILogger<ConnectionManager> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public void AddConnection(string connectionId, string userId)
        {
            _connectionToUser[connectionId] = userId;

            _userToConnections.AddOrUpdate(userId,
                new HashSet<string> { connectionId },
                (key, oldValue) =>
                {
                    lock (oldValue)
                    {
                        oldValue.Add(connectionId);
                        return oldValue;
                    }
                });

            _logger.LogInformation("User {UserId} connected with connection {ConnectionId}", userId, connectionId);
        }

        /// <inheritdoc />
        public void RemoveConnection(string connectionId)
        {
            if (_connectionToUser.TryRemove(connectionId, out var userId))
            {
                if (_userToConnections.TryGetValue(userId, out var connections))
                {
                    lock (connections)
                    {
                        connections.Remove(connectionId);
                        if (connections.Count == 0)
                        {
                            _userToConnections.TryRemove(userId, out _);
                        }
                    }
                }

                _logger.LogInformation("User {UserId} disconnected connection {ConnectionId}", userId, connectionId);
            }
        }

        /// <inheritdoc />
        public string? GetUserId(string connectionId)
        {
            return _connectionToUser.GetValueOrDefault(connectionId);
        }

        /// <inheritdoc />
        public List<string> GetConnectionIds(string userId)
        {
            if (_userToConnections.TryGetValue(userId, out var connections))
            {
                lock (connections)
                {
                    return connections.ToList();
                }
            }
            return new List<string>();
        }

        /// <inheritdoc />
        public bool IsUserOnline(string userId)
        {
            return _userToConnections.ContainsKey(userId);
        }

        /// <inheritdoc />
        public async Task<bool> AddUserToSessionAsync(string connectionId, string sessionId)
        {
            try
            {
                // Add connection to session
                _userSessions.AddOrUpdate(connectionId,
                    new HashSet<string> { sessionId },
                    (key, oldValue) =>
                    {
                        lock (oldValue)
                        {
                            oldValue.Add(sessionId);
                            return oldValue;
                        }
                    });

                // Add connection to session users
                _sessionUsers.AddOrUpdate(sessionId,
                    new HashSet<string> { connectionId },
                    (key, oldValue) =>
                    {
                        lock (oldValue)
                        {
                            oldValue.Add(connectionId);
                            return oldValue;
                        }
                    });

                var userId = GetUserId(connectionId);
                _logger.LogDebug("Added user {UserId} (connection {ConnectionId}) to session {SessionId}", 
                    userId, connectionId, sessionId);

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add connection {ConnectionId} to session {SessionId}", 
                    connectionId, sessionId);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> RemoveUserFromSessionAsync(string connectionId, string sessionId)
        {
            try
            {
                // Remove connection from session
                if (_userSessions.TryGetValue(connectionId, out var sessions))
                {
                    lock (sessions)
                    {
                        sessions.Remove(sessionId);
                        if (sessions.Count == 0)
                        {
                            _userSessions.TryRemove(connectionId, out _);
                        }
                    }
                }

                // Remove connection from session users
                if (_sessionUsers.TryGetValue(sessionId, out var users))
                {
                    lock (users)
                    {
                        users.Remove(connectionId);
                        if (users.Count == 0)
                        {
                            _sessionUsers.TryRemove(sessionId, out _);
                        }
                    }
                }

                var userId = GetUserId(connectionId);
                _logger.LogDebug("Removed user {UserId} (connection {ConnectionId}) from session {SessionId}", 
                    userId, connectionId, sessionId);

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove connection {ConnectionId} from session {SessionId}", 
                    connectionId, sessionId);
                return false;
            }
        }

        /// <inheritdoc />
        public List<string> GetUserSessions(string connectionId)
        {
            if (_userSessions.TryGetValue(connectionId, out var sessions))
            {
                lock (sessions)
                {
                    return sessions.ToList();
                }
            }
            return new List<string>();
        }

        /// <inheritdoc />
        public List<string> GetSessionUsers(string sessionId)
        {
            if (_sessionUsers.TryGetValue(sessionId, out var users))
            {
                lock (users)
                {
                    return users.ToList();
                }
            }
            return new List<string>();
        }
    }
}