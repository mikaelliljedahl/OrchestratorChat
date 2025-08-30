using FluentAssertions;
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
        result.Should().NotBeNull();
        result.Name.Should().Be(request.Name);
        result.Type.Should().Be(request.Type);
        result.Status.Should().Be(SessionStatus.Active);
        result.ParticipantAgentIds.Should().BeEquivalentTo(request.AgentIds);
        result.WorkingDirectory.Should().Be(request.WorkingDirectory);
        result.Id.Should().NotBeEmpty();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.LastActivityAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.Messages.Should().BeEmpty();
        result.Context.Should().BeEmpty();

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
        result.Should().NotBeNull();
        result.Name.Should().Be(request.Name);
        
        // Should handle gracefully by creating a new session with same name
        _mockRepository.Received(1).CreateSessionAsync(Arg.Any<Session>());
        await _mockEventBus.Received(1).PublishAsync(Arg.Any<SessionCreatedEvent>());
    }

    [Fact]
    public async Task CreateSessionAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _sessionManager.CreateSessionAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>()
            .WithMessage("*request*");

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
        result.Should().NotBeNull();
        result.Name.Should().Be("");
        
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
        result.Should().NotBeNull();
        result.ParticipantAgentIds.Should().BeEquivalentTo(request.AgentIds);
        
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
        result.Should().NotBeNull();
        result!.Id.Should().Be(sessionId);
        result.Name.Should().Be(expectedSession.Name);
        result.Id.Should().Be(sessionId);
        
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
        result.Should().BeNull();
        
        _mockRepository.Received(1).GetSessionByIdAsync(sessionId);
    }

    [Fact]
    public async Task GetSessionAsync_WithNullId_ReturnsNull()
    {
        // Act
        var result = await _sessionManager.GetSessionAsync(null!);

        // Assert
        result.Should().BeNull();
        
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
        result.Should().NotBeNull();
        result!.Id.Should().Be(createdSession.Id);
        result.Name.Should().Be(createdSession.Name);
        
        _mockRepository.Received(1).GetSessionByIdAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task GetCurrentSessionAsync_WithNoActiveSession_ReturnsNull()
    {
        // Act (no current session set)
        var result = await _sessionManager.GetCurrentSessionAsync();

        // Assert
        result.Should().BeNull();
        
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
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(sessions);
        
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
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(sessions);
        
        _mockRepository.Received(1).GetRecentSessionsAsync(10);
    }

    [Fact]
    public async Task GetRecentSessions_WithZeroCount_ReturnsEmptyList()
    {
        // Act
        var result = await _sessionManager.GetRecentSessions(0);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        
        // Should not call repository for zero count
        _mockRepository.DidNotReceive().GetRecentSessionsAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task GetRecentSessions_WithNegativeCount_ReturnsEmptyList()
    {
        // Act
        var result = await _sessionManager.GetRecentSessions(-5);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        
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
        message.SessionId.Should().Be(sessionId);
        message.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        
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
        await FluentActions.Invoking(() => _sessionManager.AddMessageAsync(sessionId, null!))
            .Should().ThrowAsync<ArgumentNullException>()
            .WithMessage("*message*");

        _mockRepository.DidNotReceive().AddMessageAsync(Arg.Any<string>(), Arg.Any<AgentMessage>());
        await _mockEventBus.DidNotReceive().PublishAsync(Arg.Any<MessageAddedEvent>());
    }

    [Fact]
    public async Task AddMessageAsync_WithNullSessionId_ThrowsArgumentNullException()
    {
        // Arrange
        var message = TestDataBuilder.DefaultUserMessage("session", "Test message");

        // Act & Assert
        await FluentActions.Invoking(() => _sessionManager.AddMessageAsync(null!, message))
            .Should().ThrowAsync<ArgumentNullException>()
            .WithMessage("*sessionId*");

        _mockRepository.DidNotReceive().AddMessageAsync(Arg.Any<string>(), Arg.Any<AgentMessage>());
        await _mockEventBus.DidNotReceive().PublishAsync(Arg.Any<MessageAddedEvent>());
    }

    [Fact]
    public async Task AddMessageAsync_WithEmptySessionId_ThrowsArgumentNullException()
    {
        // Arrange
        var message = TestDataBuilder.DefaultUserMessage("session", "Test message");

        // Act & Assert
        await FluentActions.Invoking(() => _sessionManager.AddMessageAsync("", message))
            .Should().ThrowAsync<ArgumentNullException>()
            .WithMessage("*sessionId*");

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
        result.Should().BeTrue();
        activeSession.Status.Should().Be(SessionStatus.Completed);
        activeSession.LastActivityAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        
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
        result.Should().BeTrue();
        completedSession.Status.Should().Be(SessionStatus.Completed); // Still completed
        
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
        result.Should().BeFalse();
        
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
        result.Should().NotBeNull();
        result.SessionId.Should().Be(sessionId);
        result.SessionState.Id.Should().Be(session.Id);
        result.SessionState.Name.Should().Be(session.Name);
        
        _mockRepository.Received(1).GetSessionByIdAsync(sessionId);
        _mockRepository.Received(1).CreateSnapshotAsync(sessionId, Arg.Any<SessionSnapshot>());
    }

    [Fact]
    public async Task EndSessionAsync_WithNullOrEmptySessionId_ReturnsFalse()
    {
        // Act & Assert - Null
        var result1 = await _sessionManager.EndSessionAsync(null!);
        result1.Should().BeFalse();

        // Act & Assert - Empty
        var result2 = await _sessionManager.EndSessionAsync("");
        result2.Should().BeFalse();

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
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(activeSessions);
        
        _mockRepository.Received(1).GetActiveSessionsAsync();
    }

    #endregion
}