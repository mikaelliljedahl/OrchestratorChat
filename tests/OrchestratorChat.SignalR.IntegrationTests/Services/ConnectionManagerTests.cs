using Microsoft.Extensions.Logging;
using Moq;
using OrchestratorChat.SignalR.Services;
using Xunit;

namespace OrchestratorChat.SignalR.IntegrationTests.Services
{
    /// <summary>
    /// Tests for ConnectionManager service
    /// </summary>
    public class ConnectionManagerTests
    {
        private readonly Mock<ILogger<ConnectionManager>> _mockLogger;
        private readonly ConnectionManager _connectionManager;

        public ConnectionManagerTests()
        {
            _mockLogger = new Mock<ILogger<ConnectionManager>>();
            _connectionManager = new ConnectionManager(_mockLogger.Object);
        }

        [Fact]
        public void AddConnection_ShouldMapConnectionToUser()
        {
            // Arrange
            var connectionId = "conn-1";
            var userId = "user-1";

            // Act
            _connectionManager.AddConnection(connectionId, userId);

            // Assert
            var retrievedUserId = _connectionManager.GetUserId(connectionId);
            Assert.Equal(userId, retrievedUserId);

            var connectionIds = _connectionManager.GetConnectionIds(userId);
            Assert.Contains(connectionId, connectionIds);

            Assert.True(_connectionManager.IsUserOnline(userId));
        }

        [Fact]
        public void AddConnection_WithMultipleConnections_ShouldTrackAll()
        {
            // Arrange
            var userId = "user-1";
            var connectionId1 = "conn-1";
            var connectionId2 = "conn-2";

            // Act
            _connectionManager.AddConnection(connectionId1, userId);
            _connectionManager.AddConnection(connectionId2, userId);

            // Assert
            var connectionIds = _connectionManager.GetConnectionIds(userId);
            Assert.Equal(2, connectionIds.Count());
            Assert.Contains(connectionId1, connectionIds);
            Assert.Contains(connectionId2, connectionIds);

            Assert.Equal(userId, _connectionManager.GetUserId(connectionId1));
            Assert.Equal(userId, _connectionManager.GetUserId(connectionId2));
        }

        [Fact]
        public void RemoveConnection_ShouldUnmapConnection()
        {
            // Arrange
            var connectionId = "conn-1";
            var userId = "user-1";
            _connectionManager.AddConnection(connectionId, userId);

            // Act
            _connectionManager.RemoveConnection(connectionId);

            // Assert
            Assert.Null(_connectionManager.GetUserId(connectionId));
            Assert.False(_connectionManager.IsUserOnline(userId));
            Assert.Empty(_connectionManager.GetConnectionIds(userId));
        }

        [Fact]
        public void RemoveConnection_WithMultipleConnections_ShouldOnlyRemoveOne()
        {
            // Arrange
            var userId = "user-1";
            var connectionId1 = "conn-1";
            var connectionId2 = "conn-2";
            _connectionManager.AddConnection(connectionId1, userId);
            _connectionManager.AddConnection(connectionId2, userId);

            // Act
            _connectionManager.RemoveConnection(connectionId1);

            // Assert
            Assert.Null(_connectionManager.GetUserId(connectionId1));
            Assert.Equal(userId, _connectionManager.GetUserId(connectionId2));
            Assert.True(_connectionManager.IsUserOnline(userId));

            var connectionIds = _connectionManager.GetConnectionIds(userId);
            Assert.Single(connectionIds);
            Assert.Contains(connectionId2, connectionIds);
        }

        [Fact]
        public void RemoveConnection_WithNonExistentConnection_ShouldNotThrow()
        {
            // Act & Assert
            // Act & Assert - Should not throw exception
            _connectionManager.RemoveConnection("non-existent");
        }

        [Fact]
        public void GetUserId_WithNonExistentConnection_ShouldReturnNull()
        {
            // Act
            var userId = _connectionManager.GetUserId("non-existent");

            // Assert
            Assert.Null(userId);
        }

        [Fact]
        public void GetConnectionIds_WithNonExistentUser_ShouldReturnEmptyList()
        {
            // Act
            var connectionIds = _connectionManager.GetConnectionIds("non-existent");

            // Assert
            Assert.NotNull(connectionIds);
            Assert.Empty(connectionIds);
        }

        [Fact]
        public void IsUserOnline_WithNonExistentUser_ShouldReturnFalse()
        {
            // Act
            var isOnline = _connectionManager.IsUserOnline("non-existent");

            // Assert
            Assert.False(isOnline);
        }

        [Fact]
        public async Task AddUserToSessionAsync_ShouldAddConnectionToSession()
        {
            // Arrange
            var connectionId = "conn-1";
            var sessionId = "session-1";
            var userId = "user-1";
            _connectionManager.AddConnection(connectionId, userId);

            // Act
            var result = await _connectionManager.AddUserToSessionAsync(connectionId, sessionId);

            // Assert
            Assert.True(result);
            var userSessions = _connectionManager.GetUserSessions(connectionId);
            Assert.Contains(sessionId, userSessions);

            var sessionUsers = _connectionManager.GetSessionUsers(sessionId);
            Assert.Contains(connectionId, sessionUsers);
        }

        [Fact]
        public async Task AddUserToSessionAsync_WithMultipleSessions_ShouldTrackAll()
        {
            // Arrange
            var connectionId = "conn-1";
            var sessionId1 = "session-1";
            var sessionId2 = "session-2";
            var userId = "user-1";
            _connectionManager.AddConnection(connectionId, userId);

            // Act
            await _connectionManager.AddUserToSessionAsync(connectionId, sessionId1);
            await _connectionManager.AddUserToSessionAsync(connectionId, sessionId2);

            // Assert
            var userSessions = _connectionManager.GetUserSessions(connectionId);
            Assert.Equal(2, userSessions.Count());
            Assert.Contains(sessionId1, userSessions);
            Assert.Contains(sessionId2, userSessions);
        }

        [Fact]
        public async Task RemoveUserFromSessionAsync_ShouldRemoveConnectionFromSession()
        {
            // Arrange
            var connectionId = "conn-1";
            var sessionId = "session-1";
            var userId = "user-1";
            _connectionManager.AddConnection(connectionId, userId);
            await _connectionManager.AddUserToSessionAsync(connectionId, sessionId);

            // Act
            var result = await _connectionManager.RemoveUserFromSessionAsync(connectionId, sessionId);

            // Assert
            Assert.True(result);
            var userSessions = _connectionManager.GetUserSessions(connectionId);
            Assert.DoesNotContain(sessionId, userSessions);

            var sessionUsers = _connectionManager.GetSessionUsers(sessionId);
            Assert.DoesNotContain(connectionId, sessionUsers);
        }

        [Fact]
        public async Task RemoveUserFromSessionAsync_WithMultipleSessions_ShouldOnlyRemoveOne()
        {
            // Arrange
            var connectionId = "conn-1";
            var sessionId1 = "session-1";
            var sessionId2 = "session-2";
            var userId = "user-1";
            _connectionManager.AddConnection(connectionId, userId);
            await _connectionManager.AddUserToSessionAsync(connectionId, sessionId1);
            await _connectionManager.AddUserToSessionAsync(connectionId, sessionId2);

            // Act
            await _connectionManager.RemoveUserFromSessionAsync(connectionId, sessionId1);

            // Assert
            var userSessions = _connectionManager.GetUserSessions(connectionId);
            Assert.Single(userSessions);
            Assert.Contains(sessionId2, userSessions);
        }

        [Fact]
        public void GetUserSessions_WithNonExistentConnection_ShouldReturnEmptyList()
        {
            // Act
            var sessions = _connectionManager.GetUserSessions("non-existent");

            // Assert
            Assert.NotNull(sessions);
            Assert.Empty(sessions);
        }

        [Fact]
        public void GetSessionUsers_WithNonExistentSession_ShouldReturnEmptyList()
        {
            // Act
            var users = _connectionManager.GetSessionUsers("non-existent");

            // Assert
            Assert.NotNull(users);
            Assert.Empty(users);
        }

        [Fact]
        public async Task SessionManagement_WithMultipleUsers_ShouldTrackCorrectly()
        {
            // Arrange
            var connectionId1 = "conn-1";
            var connectionId2 = "conn-2";
            var userId1 = "user-1";
            var userId2 = "user-2";
            var sessionId = "session-1";

            _connectionManager.AddConnection(connectionId1, userId1);
            _connectionManager.AddConnection(connectionId2, userId2);

            // Act
            await _connectionManager.AddUserToSessionAsync(connectionId1, sessionId);
            await _connectionManager.AddUserToSessionAsync(connectionId2, sessionId);

            // Assert
            var sessionUsers = _connectionManager.GetSessionUsers(sessionId);
            Assert.Equal(2, sessionUsers.Count());
            Assert.Contains(connectionId1, sessionUsers);
            Assert.Contains(connectionId2, sessionUsers);

            var user1Sessions = _connectionManager.GetUserSessions(connectionId1);
            var user2Sessions = _connectionManager.GetUserSessions(connectionId2);
            Assert.Contains(sessionId, user1Sessions);
            Assert.Contains(sessionId, user2Sessions);
        }

        [Fact]
        public async Task ConnectionCleanup_WhenUserDisconnects_ShouldMaintainSessionIntegrity()
        {
            // Arrange
            var connectionId1 = "conn-1";
            var connectionId2 = "conn-2";
            var userId1 = "user-1";
            var userId2 = "user-2";
            var sessionId = "session-1";

            _connectionManager.AddConnection(connectionId1, userId1);
            _connectionManager.AddConnection(connectionId2, userId2);
            await _connectionManager.AddUserToSessionAsync(connectionId1, sessionId);
            await _connectionManager.AddUserToSessionAsync(connectionId2, sessionId);

            // Act - User 1 disconnects
            _connectionManager.RemoveConnection(connectionId1);

            // Assert - Session should still have user 2
            var sessionUsers = _connectionManager.GetSessionUsers(sessionId);
            Assert.Single(sessionUsers);
            Assert.Contains(connectionId2, sessionUsers);

            // User 1 should no longer be trackable
            Assert.Null(_connectionManager.GetUserId(connectionId1));
            Assert.False(_connectionManager.IsUserOnline(userId1));
        }
    }
}