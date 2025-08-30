using Microsoft.Extensions.Logging;
using NSubstitute;
using OrchestratorChat.Core.Events;
using OrchestratorChat.Core.Messages;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Core.Tests.Fixtures;
using OrchestratorChat.Core.Tests.TestHelpers;
using Xunit;

namespace OrchestratorChat.Core.Tests.Unit.Sessions;

/// <summary>
/// Comprehensive unit tests for SessionManager functionality
/// </summary>
public class SessionManagerTests : IDisposable
{
    private readonly ISessionRepository _mockRepository;
    private readonly IEventBus _mockEventBus;
    private readonly SessionManager _sessionManager;

    public SessionManagerTests()
    {
        _mockRepository = Substitute.For<ISessionRepository>();
        _mockEventBus = Substitute.For<IEventBus>();
        _sessionManager = new SessionManager(_mockRepository, _mockEventBus);
    }

    public void Dispose()
    {
        // No cleanup needed for unit tests
    }

    #region Session Creation Tests

    [Fact]
    public async Task CreateSessionAsync_WithValidData_ReturnsNewSession()
    {
        // Arrange
        var request = new CreateSessionRequest
        {
            Name = "Test Session",
            Type = SessionType.MultiAgent,
            AgentIds = ["agent1", "agent2"],
            WorkingDirectory = "/test/directory"
        };

        var expectedSession = TestDataBuilder.Session()
            .WithName(request.Name)
            .WithType(request.Type)
            .WithParticipantAgents("agent1", "agent2")
            .WithWorkingDirectory(request.WorkingDirectory)
            .WithStatus(SessionStatus.Active)
            .Build();

        _mockRepository.CreateSessionAsync(Arg.Any<Session>())
            .Returns(callInfo => callInfo.Arg<Session>());

        // Act
        var result = await _sessionManager.CreateSessionAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.Type, result.Type);
        Assert.Equal(SessionStatus.Active, result.Status);
        Assert.Equal(request.AgentIds, result.ParticipantAgentIds);
        Assert.Equal(request.WorkingDirectory, result.WorkingDirectory);
        Assert.NotEqual(Guid.Empty.ToString(), result.Id);
        Assert.True(Math.Abs((DateTime.UtcNow - result.CreatedAt).TotalSeconds) < 1);
        Assert.True(Math.Abs((DateTime.UtcNow - result.LastActivityAt).TotalSeconds) < 1);
        Assert.Empty(result.Messages);
        Assert.Empty(result.Context);

        // Verify repository was called
        _mockRepository.Received(1).CreateSessionAsync(Arg.Any<Session>());
        
        // Verify event was published
        await _mockEventBus.Received(1).PublishAsync(Arg.Any<SessionCreatedEvent>());
    }

    [Fact]
    public async Task CreateSessionAsync_WithDuplicateName_HandlesGracefully()
    {
        // Arrange
        var request = new CreateSessionRequest
        {
            Name = "Duplicate Session",
            Type = SessionType.SingleAgent,
            AgentIds = ["agent1"]
        };

        _mockRepository.CreateSessionAsync(Arg.Any<Session>())
            .Returns(callInfo => callInfo.Arg<Session>());

        // Act
        var result = await _sessionManager.CreateSessionAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Name, result.Name);
        
        // Should handle gracefully by creating a new session with same name
        _mockRepository.Received(1).CreateSessionAsync(Arg.Any<Session>());
        await _mockEventBus.Received(1).PublishAsync(Arg.Any<SessionCreatedEvent>());
    }

    [Fact]
    public async Task CreateSessionAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => _sessionManager.CreateSessionAsync(null!));
        Assert.Contains("request", ex.Message);

        // Verify no repository calls were made
        _mockRepository.DidNotReceive().CreateSessionAsync(Arg.Any<Session>());
        await _mockEventBus.DidNotReceive().PublishAsync(Arg.Any<SessionCreatedEvent>());
    }

    [Fact]
    public async Task CreateSessionAsync_WithEmptyName_CreatesSessionWithEmptyName()
    {
        // Arrange
        var request = new CreateSessionRequest
        {
            Name = "",
            Type = SessionType.SingleAgent,
            AgentIds = ["agent1"]
        };

        _mockRepository.CreateSessionAsync(Arg.Any<Session>())
            .Returns(callInfo => callInfo.Arg<Session>());

        // Act
        var result = await _sessionManager.CreateSessionAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("", result.Name);
        
        _mockRepository.Received(1).CreateSessionAsync(Arg.Any<Session>());
    }

    [Fact]
    public async Task CreateSessionAsync_WithInvalidAgentIds_CreatesSessionWithAllAgents()
    {
        // Arrange
        var request = new CreateSessionRequest
        {
            Name = "Test Session",
            Type = SessionType.MultiAgent,
            AgentIds = ["valid-agent", "invalid-agent", "another-agent"]
        };

        _mockRepository.CreateSessionAsync(Arg.Any<Session>())
            .Returns(callInfo => callInfo.Arg<Session>());

        // Act
        var result = await _sessionManager.CreateSessionAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.AgentIds, result.ParticipantAgentIds);
        
        // SessionManager doesn't validate agent IDs - it accepts what's provided
        _mockRepository.Received(1).CreateSessionAsync(Arg.Any<Session>());
    }

    #endregion

    #region Session Retrieval Tests

    [Fact]
    public async Task GetSessionAsync_WithExistingId_ReturnsSession()
    {
        // Arrange
        var sessionId = "test-session-id";
        var expectedSession = TestDataBuilder.DefaultSession(sessionId);
        
        _mockRepository.GetSessionByIdAsync(sessionId)
            .Returns(expectedSession);

        // Act
        var result = await _sessionManager.GetSessionAsync(sessionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(sessionId, result!.Id);
        Assert.Equal(expectedSession.Name, result.Name);
        Assert.Equal(sessionId, result.Id);
        
        _mockRepository.Received(1).GetSessionByIdAsync(sessionId);
    }

    [Fact]
    public async Task GetSessionAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var sessionId = "non-existent-id";
        
        _mockRepository.GetSessionByIdAsync(sessionId)
            .Returns((Session?)null);

        // Act
        var result = await _sessionManager.GetSessionAsync(sessionId);

        // Assert
        Assert.Null(result);
        
        _mockRepository.Received(1).GetSessionByIdAsync(sessionId);
    }

    [Fact]
    public async Task GetSessionAsync_WithNullId_ReturnsNull()
    {
        // Act
        var result = await _sessionManager.GetSessionAsync(null!);

        // Assert
        Assert.Null(result);
        
        // Should not call repository with null ID
        _mockRepository.DidNotReceive().GetSessionByIdAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task GetCurrentSessionAsync_WithActiveSession_ReturnsCurrent()
    {
        // Arrange - First create a session to set current session
        var request = new CreateSessionRequest
        {
            Name = "Current Session",
            Type = SessionType.SingleAgent,
            AgentIds = ["agent1"]
        };

        var createdSession = TestDataBuilder.DefaultSession();
        
        _mockRepository.CreateSessionAsync(Arg.Any<Session>())
            .Returns(createdSession);
        _mockRepository.GetSessionByIdAsync(createdSession.Id)
            .Returns(createdSession);

        // Create session to set current
        await _sessionManager.CreateSessionAsync(request);

        // Act
        var result = await _sessionManager.GetCurrentSessionAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdSession.Id, result!.Id);
        Assert.Equal(createdSession.Name, result.Name);
        
        _mockRepository.Received(1).GetSessionByIdAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task GetCurrentSessionAsync_WithNoActiveSession_ReturnsNull()
    {
        // Act (no current session set)
        var result = await _sessionManager.GetCurrentSessionAsync();

        // Assert
        Assert.Null(result);
        
        // Should not call repository when no current session
        _mockRepository.DidNotReceive().GetSessionByIdAsync(Arg.Any<string>());
    }

    #endregion

    #region Session History Tests

    [Fact]
    public async Task GetRecentSessions_WithMultipleSessions_ReturnsOrderedList()
    {
        // Arrange
        var sessions = new List<Session>
        {
            TestDataBuilder.Session()
                .WithName("Session 1")
                .WithLastActivityAt(DateTime.UtcNow.AddMinutes(-5))
                .Build(),
            TestDataBuilder.Session()
                .WithName("Session 2")  
                .WithLastActivityAt(DateTime.UtcNow.AddMinutes(-10))
                .Build(),
            TestDataBuilder.Session()
                .WithName("Session 3")
                .WithLastActivityAt(DateTime.UtcNow.AddMinutes(-15))
                .Build()
        };

        _mockRepository.GetRecentSessionsAsync(5)
            .Returns(sessions);

        // Act
        var result = await _sessionManager.GetRecentSessions(5);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal(sessions, result);
        
        _mockRepository.Received(1).GetRecentSessionsAsync(5);
    }

    [Fact]
    public async Task GetRecentSessions_WithFewerSessionsThanRequested_ReturnsAll()
    {
        // Arrange
        var sessions = new List<Session>
        {
            TestDataBuilder.DefaultSession("session1"),
            TestDataBuilder.DefaultSession("session2")
        };

        _mockRepository.GetRecentSessionsAsync(10)
            .Returns(sessions);

        // Act
        var result = await _sessionManager.GetRecentSessions(10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(sessions, result);
        
        _mockRepository.Received(1).GetRecentSessionsAsync(10);
    }

    [Fact]
    public async Task GetRecentSessions_WithZeroCount_ReturnsEmptyList()
    {
        // Act
        var result = await _sessionManager.GetRecentSessions(0);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        
        // Should not call repository for zero count
        _mockRepository.DidNotReceive().GetRecentSessionsAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task GetRecentSessions_WithNegativeCount_ReturnsEmptyList()
    {
        // Act
        var result = await _sessionManager.GetRecentSessions(-5);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        
        // Should not call repository for negative count
        _mockRepository.DidNotReceive().GetRecentSessionsAsync(Arg.Any<int>());
    }

    #endregion

    #region Message Management Tests

    [Fact]
    public async Task AddMessageAsync_ToExistingSession_AddsMessage()
    {
        // Arrange
        var sessionId = "test-session";
        var message = TestDataBuilder.DefaultUserMessage(sessionId, "Test message content");

        _mockRepository.AddMessageAsync(sessionId, Arg.Any<AgentMessage>())
            .Returns(Task.CompletedTask);

        // Act
        await _sessionManager.AddMessageAsync(sessionId, message);

        // Assert
        Assert.Equal(sessionId, message.SessionId);
        Assert.True(Math.Abs((DateTime.UtcNow - message.Timestamp).TotalSeconds) < 1);
        
        _mockRepository.Received(1).AddMessageAsync(sessionId, message);
        await _mockEventBus.Received(1).PublishAsync(Arg.Any<MessageAddedEvent>());
    }

    [Fact]
    public async Task AddMessageAsync_ToNonExistentSession_StillAddsMessage()
    {
        // Arrange
        var sessionId = "non-existent-session";
        var message = TestDataBuilder.DefaultUserMessage(sessionId);

        _mockRepository.AddMessageAsync(sessionId, Arg.Any<AgentMessage>())
            .Returns(Task.CompletedTask);

        // Act
        await _sessionManager.AddMessageAsync(sessionId, message);

        // Assert
        // SessionManager doesn't validate session existence - delegates to repository
        _mockRepository.Received(1).AddMessageAsync(sessionId, message);
        await _mockEventBus.Received(1).PublishAsync(Arg.Any<MessageAddedEvent>());
    }

    [Fact]
    public async Task AddMessageAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var sessionId = "test-session";

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => _sessionManager.AddMessageAsync(sessionId, null!));
        Assert.Contains("message", ex.Message);

        _mockRepository.DidNotReceive().AddMessageAsync(Arg.Any<string>(), Arg.Any<AgentMessage>());
        await _mockEventBus.DidNotReceive().PublishAsync(Arg.Any<MessageAddedEvent>());
    }

    [Fact]
    public async Task AddMessageAsync_WithNullSessionId_ThrowsArgumentNullException()
    {
        // Arrange
        var message = TestDataBuilder.DefaultUserMessage("session", "Test message");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => _sessionManager.AddMessageAsync(null!, message));
        Assert.Contains("sessionId", ex.Message);

        _mockRepository.DidNotReceive().AddMessageAsync(Arg.Any<string>(), Arg.Any<AgentMessage>());
        await _mockEventBus.DidNotReceive().PublishAsync(Arg.Any<MessageAddedEvent>());
    }

    [Fact]
    public async Task AddMessageAsync_WithEmptySessionId_ThrowsArgumentNullException()
    {
        // Arrange
        var message = TestDataBuilder.DefaultUserMessage("session", "Test message");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => _sessionManager.AddMessageAsync("", message));
        Assert.Contains("sessionId", ex.Message);

        _mockRepository.DidNotReceive().AddMessageAsync(Arg.Any<string>(), Arg.Any<AgentMessage>());
        await _mockEventBus.DidNotReceive().PublishAsync(Arg.Any<MessageAddedEvent>());
    }

    #endregion

    #region Session Lifecycle Tests

    [Fact]
    public async Task EndSessionAsync_WithActiveSession_MarksCompleted()
    {
        // Arrange
        var sessionId = "test-session";
        var activeSession = TestDataBuilder.Session()
            .WithId(sessionId)
            .WithStatus(SessionStatus.Active)
            .Build();

        _mockRepository.GetSessionByIdAsync(sessionId)
            .Returns(activeSession);
        _mockRepository.UpdateSessionAsync(Arg.Any<Session>())
            .Returns(true);

        // Act
        var result = await _sessionManager.EndSessionAsync(sessionId);

        // Assert
        Assert.True(result);
        Assert.Equal(SessionStatus.Completed, activeSession.Status);
        Assert.True(Math.Abs((DateTime.UtcNow - activeSession.LastActivityAt).TotalSeconds) < 1);
        
        _mockRepository.Received(1).GetSessionByIdAsync(sessionId);
        _mockRepository.Received(1).UpdateSessionAsync(activeSession);
        await _mockEventBus.Received(1).PublishAsync(Arg.Any<SessionEndedEvent>());
    }

    [Fact]
    public async Task EndSessionAsync_WithAlreadyEndedSession_UpdatesSuccessfully()
    {
        // Arrange
        var sessionId = "test-session";
        var completedSession = TestDataBuilder.Session()
            .WithId(sessionId)
            .WithStatus(SessionStatus.Completed)
            .Build();

        _mockRepository.GetSessionByIdAsync(sessionId)
            .Returns(completedSession);
        _mockRepository.UpdateSessionAsync(Arg.Any<Session>())
            .Returns(true);

        // Act
        var result = await _sessionManager.EndSessionAsync(sessionId);

        // Assert
        Assert.True(result);
        Assert.Equal(SessionStatus.Completed, completedSession.Status); // Still completed
        
        _mockRepository.Received(1).GetSessionByIdAsync(sessionId);
        _mockRepository.Received(1).UpdateSessionAsync(completedSession);
        await _mockEventBus.Received(1).PublishAsync(Arg.Any<SessionEndedEvent>());
    }

    [Fact]
    public async Task EndSessionAsync_WithNonExistentSession_ReturnsFalse()
    {
        // Arrange
        var sessionId = "non-existent-session";

        _mockRepository.GetSessionByIdAsync(sessionId)
            .Returns((Session?)null);

        // Act
        var result = await _sessionManager.EndSessionAsync(sessionId);

        // Assert
        Assert.False(result);
        
        _mockRepository.Received(1).GetSessionByIdAsync(sessionId);
        _mockRepository.DidNotReceive().UpdateSessionAsync(Arg.Any<Session>());
        await _mockEventBus.DidNotReceive().PublishAsync(Arg.Any<SessionEndedEvent>());
    }

    #endregion

    #region Additional Tests for Edge Cases

    [Fact]
    public async Task UpdateSessionContextAsync_WithValidData_UpdatesContext()
    {
        // Arrange
        var sessionId = "test-session";
        var context = new Dictionary<string, object>
        {
            ["key1"] = "value1",
            ["key2"] = 42
        };

        _mockRepository.UpdateSessionContextAsync(sessionId, context)
            .Returns(Task.CompletedTask);

        // Act
        await _sessionManager.UpdateSessionContextAsync(sessionId, context);

        // Assert
        _mockRepository.Received(1).UpdateSessionContextAsync(sessionId, context);
    }

    [Fact]
    public async Task CreateSnapshotAsync_WithValidSession_CreatesSnapshot()
    {
        // Arrange
        var sessionId = "test-session";
        var session = TestDataBuilder.DefaultSession(sessionId);
        var expectedSnapshot = new SessionSnapshot
        {
            Id = "snapshot-id",
            SessionId = sessionId,
            CreatedAt = DateTime.UtcNow,
            Description = $"Snapshot of session {session.Name}",
            SessionState = session,
            AgentStates = new Dictionary<string, AgentState>()
        };

        _mockRepository.GetSessionByIdAsync(sessionId)
            .Returns(session);
        _mockRepository.CreateSnapshotAsync(sessionId, Arg.Any<SessionSnapshot>())
            .Returns(expectedSnapshot);

        // Act
        var result = await _sessionManager.CreateSnapshotAsync(sessionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(sessionId, result.SessionId);
        Assert.Equal(session.Id, result.SessionState.Id);
        Assert.Equal(session.Name, result.SessionState.Name);
        
        _mockRepository.Received(1).GetSessionByIdAsync(sessionId);
        _mockRepository.Received(1).CreateSnapshotAsync(sessionId, Arg.Any<SessionSnapshot>());
    }

    [Fact]
    public async Task EndSessionAsync_WithNullOrEmptySessionId_ReturnsFalse()
    {
        // Act & Assert - Null
        var result1 = await _sessionManager.EndSessionAsync(null!);
        Assert.False(result1);

        // Act & Assert - Empty
        var result2 = await _sessionManager.EndSessionAsync("");
        Assert.False(result2);

        _mockRepository.DidNotReceive().GetSessionByIdAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task GetActiveSessionsAsync_ReturnsActiveSessionsList()
    {
        // Arrange
        var activeSessions = new List<Session>
        {
            TestDataBuilder.Session().WithStatus(SessionStatus.Active).Build(),
            TestDataBuilder.Session().WithStatus(SessionStatus.Active).Build()
        };

        _mockRepository.GetActiveSessionsAsync()
            .Returns(activeSessions);

        // Act
        var result = await _sessionManager.GetActiveSessionsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(activeSessions, result);
        
        _mockRepository.Received(1).GetActiveSessionsAsync();
    }

    #endregion
}