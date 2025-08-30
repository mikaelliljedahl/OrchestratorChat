using FluentAssertions;
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
            retrievedUserId.Should().Be(userId);

            var connectionIds = _connectionManager.GetConnectionIds(userId);
            connectionIds.Should().Contain(connectionId);

            _connectionManager.IsUserOnline(userId).Should().BeTrue();
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
            connectionIds.Should().HaveCount(2);
            connectionIds.Should().Contain(connectionId1, connectionId2);

            _connectionManager.GetUserId(connectionId1).Should().Be(userId);
            _connectionManager.GetUserId(connectionId2).Should().Be(userId);
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
            _connectionManager.GetUserId(connectionId).Should().BeNull();
            _connectionManager.IsUserOnline(userId).Should().BeFalse();
            _connectionManager.GetConnectionIds(userId).Should().BeEmpty();
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
            _connectionManager.GetUserId(connectionId1).Should().BeNull();
            _connectionManager.GetUserId(connectionId2).Should().Be(userId);
            _connectionManager.IsUserOnline(userId).Should().BeTrue();

            var connectionIds = _connectionManager.GetConnectionIds(userId);
            connectionIds.Should().HaveCount(1);
            connectionIds.Should().Contain(connectionId2);
        }

        [Fact]
        public void RemoveConnection_WithNonExistentConnection_ShouldNotThrow()
        {
            // Act & Assert
            Action act = () => _connectionManager.RemoveConnection("non-existent");
            act.Should().NotThrow();
        }

        [Fact]
        public void GetUserId_WithNonExistentConnection_ShouldReturnNull()
        {
            // Act
            var userId = _connectionManager.GetUserId("non-existent");

            // Assert
            userId.Should().BeNull();
        }

        [Fact]
        public void GetConnectionIds_WithNonExistentUser_ShouldReturnEmptyList()
        {
            // Act
            var connectionIds = _connectionManager.GetConnectionIds("non-existent");

            // Assert
            connectionIds.Should().NotBeNull();
            connectionIds.Should().BeEmpty();
        }

        [Fact]
        public void IsUserOnline_WithNonExistentUser_ShouldReturnFalse()
        {
            // Act
            var isOnline = _connectionManager.IsUserOnline("non-existent");

            // Assert
            isOnline.Should().BeFalse();
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
            result.Should().BeTrue();
            var userSessions = _connectionManager.GetUserSessions(connectionId);
            userSessions.Should().Contain(sessionId);

            var sessionUsers = _connectionManager.GetSessionUsers(sessionId);
            sessionUsers.Should().Contain(connectionId);
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
            userSessions.Should().HaveCount(2);
            userSessions.Should().Contain(sessionId1, sessionId2);
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
            result.Should().BeTrue();
            var userSessions = _connectionManager.GetUserSessions(connectionId);
            userSessions.Should().NotContain(sessionId);

            var sessionUsers = _connectionManager.GetSessionUsers(sessionId);
            sessionUsers.Should().NotContain(connectionId);
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
            userSessions.Should().HaveCount(1);
            userSessions.Should().Contain(sessionId2);
        }

        [Fact]
        public void GetUserSessions_WithNonExistentConnection_ShouldReturnEmptyList()
        {
            // Act
            var sessions = _connectionManager.GetUserSessions("non-existent");

            // Assert
            sessions.Should().NotBeNull();
            sessions.Should().BeEmpty();
        }

        [Fact]
        public void GetSessionUsers_WithNonExistentSession_ShouldReturnEmptyList()
        {
            // Act
            var users = _connectionManager.GetSessionUsers("non-existent");

            // Assert
            users.Should().NotBeNull();
            users.Should().BeEmpty();
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
            sessionUsers.Should().HaveCount(2);
            sessionUsers.Should().Contain(connectionId1, connectionId2);

            var user1Sessions = _connectionManager.GetUserSessions(connectionId1);
            var user2Sessions = _connectionManager.GetUserSessions(connectionId2);
            user1Sessions.Should().Contain(sessionId);
            user2Sessions.Should().Contain(sessionId);
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
            sessionUsers.Should().HaveCount(1);
            sessionUsers.Should().Contain(connectionId2);

            // User 1 should no longer be trackable
            _connectionManager.GetUserId(connectionId1).Should().BeNull();
            _connectionManager.IsUserOnline(userId1).Should().BeFalse();
        }
    }
}