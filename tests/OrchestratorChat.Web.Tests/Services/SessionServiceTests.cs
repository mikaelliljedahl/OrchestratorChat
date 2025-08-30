using Microsoft.EntityFrameworkCore;
using NSubstitute;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Data;
using OrchestratorChat.Data.Models;
using OrchestratorChat.Web.Services;
using OrchestratorChat.Web.Tests.TestHelpers;
using Xunit;

namespace OrchestratorChat.Web.Tests.Services;

public class SessionServiceTests : IDisposable
{
    private readonly OrchestratorDbContext _context;
    private readonly SessionService _service;

    public SessionServiceTests()
    {
        var options = new DbContextOptionsBuilder<OrchestratorDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new OrchestratorDbContext(options);
        _service = new SessionService(_context);
    }

    [Fact]
    public async Task GetCurrentSessionAsync_Should_Return_Data_When_Session_Exists()
    {
        // Arrange
        var sessionEntity = new SessionEntity
        {
            Id = "test-session-1",
            Name = "Test Session",
            Type = SessionType.SingleAgent,
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            WorkingDirectory = "/test/directory"
        };

        _context.Sessions.Add(sessionEntity);
        await _context.SaveChangesAsync();
        await _service.SetCurrentSessionAsync("test-session-1");

        // Act
        var result = await _service.GetCurrentSessionAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-session-1", result.Id);
        Assert.Equal("Test Session", result.Name);
        Assert.Equal(SessionStatus.Active, result.Status);
    }

    [Fact]
    public async Task GetCurrentSessionAsync_Should_Return_Null_When_No_Current_Session()
    {
        // Act
        var result = await _service.GetCurrentSessionAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateSessionAsync_Should_Create_Session_Successfully()
    {
        // Arrange
        var sessionName = "New Test Session";
        var description = "Test Description";

        // Act
        var result = await _service.CreateSessionAsync(sessionName, description);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(sessionName, result.Name);
        Assert.Equal(SessionType.SingleAgent, result.Type);
        Assert.Equal(SessionStatus.Active, result.Status);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);

        // Verify it was added to database
        var entityInDb = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == result.Id);
        Assert.NotNull(entityInDb);
        Assert.Equal(sessionName, entityInDb.Name);
    }

    [Fact]
    public async Task CreateSessionAsync_Should_Set_As_Current_Session()
    {
        // Arrange
        var sessionName = "Current Session Test";

        // Act
        var createdSession = await _service.CreateSessionAsync(sessionName);
        var currentSession = await _service.GetCurrentSessionAsync();

        // Assert
        Assert.NotNull(currentSession);
        Assert.Equal(createdSession.Id, currentSession.Id);
    }

    [Fact]
    public async Task GetSessionAsync_Should_Return_Session_When_Exists()
    {
        // Arrange
        var sessionEntity = new SessionEntity
        {
            Id = "lookup-test-session",
            Name = "Lookup Test",
            Type = SessionType.MultiAgent,
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            WorkingDirectory = "/test/lookup"
        };

        _context.Sessions.Add(sessionEntity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSessionAsync("lookup-test-session");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("lookup-test-session", result.Id);
        Assert.Equal("Lookup Test", result.Name);
        Assert.Equal(SessionType.MultiAgent, result.Type);
    }

    [Fact]
    public async Task GetSessionAsync_Should_Return_Null_When_Not_Found()
    {
        // Act
        var result = await _service.GetSessionAsync("nonexistent-session");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetRecentSessionsAsync_Should_Return_Sessions_In_Correct_Order()
    {
        // Arrange
        var baseTime = DateTime.UtcNow.AddHours(-3);
        
        var session1 = new SessionEntity
        {
            Id = "session-1",
            Name = "Oldest Session",
            Type = SessionType.SingleAgent,
            Status = SessionStatus.Active,
            CreatedAt = baseTime,
            LastActivityAt = baseTime,
            WorkingDirectory = "/test"
        };

        var session2 = new SessionEntity
        {
            Id = "session-2",
            Name = "Newer Session",
            Type = SessionType.SingleAgent,
            Status = SessionStatus.Active,
            CreatedAt = baseTime.AddHours(1),
            LastActivityAt = baseTime.AddHours(1),
            WorkingDirectory = "/test"
        };

        var session3 = new SessionEntity
        {
            Id = "session-3",
            Name = "Newest Session",
            Type = SessionType.SingleAgent,
            Status = SessionStatus.Active,
            CreatedAt = baseTime.AddHours(2),
            LastActivityAt = baseTime.AddHours(2),
            WorkingDirectory = "/test"
        };

        _context.Sessions.AddRange(session1, session2, session3);
        await _context.SaveChangesAsync();

        // Act
        var results = await _service.GetRecentSessionsAsync(10);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("session-3", results[0].Id); // Newest first
        Assert.Equal("session-2", results[1].Id);
        Assert.Equal("session-1", results[2].Id); // Oldest last
    }

    [Fact]
    public async Task UpdateSessionAsync_Should_Update_Session_Properties()
    {
        // Arrange
        var sessionEntity = new SessionEntity
        {
            Id = "update-test-session",
            Name = "Original Name",
            Type = SessionType.SingleAgent,
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            LastActivityAt = DateTime.UtcNow.AddHours(-1),
            WorkingDirectory = "/original/path",
            ProjectId = "original-project"
        };

        _context.Sessions.Add(sessionEntity);
        await _context.SaveChangesAsync();

        var updatedSession = new Session
        {
            Id = "update-test-session",
            Name = "Updated Name",
            Status = SessionStatus.Completed,
            WorkingDirectory = "/updated/path",
            ProjectId = "updated-project"
        };

        // Act
        await _service.UpdateSessionAsync(updatedSession);

        // Assert
        var entityInDb = await _context.Sessions.FindAsync("update-test-session");
        Assert.NotNull(entityInDb);
        Assert.Equal("Updated Name", entityInDb.Name);
        Assert.Equal(SessionStatus.Completed, entityInDb.Status);
        Assert.Equal("/updated/path", entityInDb.WorkingDirectory);
        Assert.Equal("updated-project", entityInDb.ProjectId);
        Assert.True(entityInDb.LastActivityAt > DateTime.UtcNow.AddMinutes(-1)); // Updated recently
    }

    [Fact]
    public async Task DeleteSessionAsync_Should_Remove_Session_From_Database()
    {
        // Arrange
        var sessionEntity = new SessionEntity
        {
            Id = "delete-test-session",
            Name = "Delete Test",
            Type = SessionType.SingleAgent,
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            WorkingDirectory = "/test"
        };

        _context.Sessions.Add(sessionEntity);
        await _context.SaveChangesAsync();

        // Act
        await _service.DeleteSessionAsync("delete-test-session");

        // Assert
        var entityInDb = await _context.Sessions.FindAsync("delete-test-session");
        Assert.Null(entityInDb);
    }

    [Fact]
    public async Task DeleteSessionAsync_Should_Clear_Current_Session_If_Deleted()
    {
        // Arrange
        var sessionEntity = new SessionEntity
        {
            Id = "current-delete-test",
            Name = "Current Delete Test",
            Type = SessionType.SingleAgent,
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            WorkingDirectory = "/test"
        };

        _context.Sessions.Add(sessionEntity);
        await _context.SaveChangesAsync();
        await _service.SetCurrentSessionAsync("current-delete-test");

        // Act
        await _service.DeleteSessionAsync("current-delete-test");

        // Assert
        var currentSession = await _service.GetCurrentSessionAsync();
        Assert.Null(currentSession);
    }

    [Fact]
    public async Task SetCurrentSessionAsync_Should_Return_True_When_Session_Exists()
    {
        // Arrange
        var sessionEntity = new SessionEntity
        {
            Id = "set-current-test",
            Name = "Set Current Test",
            Type = SessionType.SingleAgent,
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            WorkingDirectory = "/test"
        };

        _context.Sessions.Add(sessionEntity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.SetCurrentSessionAsync("set-current-test");

        // Assert
        Assert.True(result);
        var currentSession = await _service.GetCurrentSessionAsync();
        Assert.NotNull(currentSession);
        Assert.Equal("set-current-test", currentSession.Id);
    }

    [Fact]
    public async Task SetCurrentSessionAsync_Should_Return_False_When_Session_Not_Found()
    {
        // Act
        var result = await _service.SetCurrentSessionAsync("nonexistent-session");

        // Assert
        Assert.False(result);
        var currentSession = await _service.GetCurrentSessionAsync();
        Assert.Null(currentSession);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}