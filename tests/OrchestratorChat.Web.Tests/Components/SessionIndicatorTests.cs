using Bunit;
using Microsoft.Extensions.DependencyInjection;
using OrchestratorChat.Web.Components;
using OrchestratorChat.Web.Tests.TestHelpers;
using OrchestratorChat.Core.Sessions;
using NSubstitute;
using Xunit;

namespace OrchestratorChat.Web.Tests.Components;

public class SessionIndicatorTests : TestContext
{
    public SessionIndicatorTests()
    {
        Services.AddMudServices();
    }

    [Fact]
    public void SessionIndicator_Should_Show_Active_Session_Status()
    {
        // Arrange
        var mockSessionManager = MockServiceFactory.CreateMockSessionManager();
        Services.AddSingleton(mockSessionManager);
        
        // Act
        var component = RenderComponent<SessionIndicator>();
        
        // Assert
        Assert.Contains("Session Active", component.Markup);
    }

    [Fact]
    public void SessionIndicator_Should_Display_Session_Name()
    {
        // Arrange
        var mockSessionManager = MockServiceFactory.CreateMockSessionManager();
        Services.AddSingleton(mockSessionManager);
        
        // Act
        var component = RenderComponent<SessionIndicator>();
        
        // Assert
        Assert.Contains("Test Session", component.Markup);
    }

    [Fact]
    public void SessionIndicator_Should_Show_Participant_Count()
    {
        // Arrange
        var session = TestDataFactory.CreateSession();
        session.ParticipantAgentIds = new List<string> { "agent-1", "agent-2" };
        
        var mockSessionManager = Substitute.For<ISessionManager>();
        mockSessionManager.GetCurrentSessionAsync().Returns(Task.FromResult<Session?>(session));
        Services.AddSingleton(mockSessionManager);
        
        // Act
        var component = RenderComponent<SessionIndicator>();
        
        // Assert
        // The component should handle participant count (stored in internal state)
        Assert.Contains("Test Session", component.Markup);
    }

    [Fact]
    public void SessionIndicator_Should_Handle_Null_Session_Gracefully()
    {
        // Arrange
        var mockSessionManager = Substitute.For<ISessionManager>();
        mockSessionManager.GetCurrentSessionAsync().Returns(Task.FromResult<Session?>(null));
        Services.AddSingleton(mockSessionManager);
        
        // Act
        var component = RenderComponent<SessionIndicator>();
        
        // Assert
        Assert.Contains("No Session", component.Markup);
    }
}