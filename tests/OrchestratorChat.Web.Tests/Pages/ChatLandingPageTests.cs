using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using NSubstitute;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Authentication;
using OrchestratorChat.Core.Messages;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Web.Models;
using OrchestratorChat.Web.Pages;
using OrchestratorChat.Web.Services;
using OrchestratorChat.Web.Tests.TestHelpers;
using Xunit;

namespace OrchestratorChat.Web.Tests.Pages;

public class ChatLandingPageTests : TestContext
{
    private readonly ISessionManager _mockSessionManager;
    private readonly IAgentService _mockAgentService;
    private readonly NavigationManager _mockNavigationManager;
    private readonly ILogger<Chat> _mockLogger;

    public ChatLandingPageTests()
    {
        Services.AddMudServices(configuration => { });
        
        // Create mocks
        _mockSessionManager = Substitute.For<ISessionManager>();
        _mockAgentService = Substitute.For<IAgentService>();
        _mockNavigationManager = MockServiceFactory.CreateMockNavigationManager();
        _mockLogger = Substitute.For<ILogger<Chat>>();
        
        // Register services
        Services.AddSingleton(_mockSessionManager);
        Services.AddSingleton(_mockAgentService);
        Services.AddSingleton(_mockNavigationManager);
        Services.AddSingleton(_mockLogger);
        
        // Add mock for CreateAgentDialog dependency
        var mockAnthropicOAuthService = Substitute.For<IAnthropicOAuthService>();
        Services.AddSingleton(mockAnthropicOAuthService);
    }

    [Fact]
    public void ChatLandingPage_Should_Render_PageTitle()
    {
        // Arrange
        SetupMocks(withSessions: false, withAgents: false);
        
        // Act
        var component = RenderComponent<Chat>();
        
        // Assert
        Assert.Contains("Chat", component.Markup);
    }

    [Fact]
    public void ChatLandingPage_Should_Show_Recent_Sessions_When_Available()
    {
        // Arrange
        var testSessions = new List<Session>
        {
            new Session 
            { 
                Id = "session1", 
                Name = "Test Session 1", 
                Status = SessionStatus.Active,
                LastActivityAt = DateTime.UtcNow.AddMinutes(-5),
                Messages = new List<AgentMessage> { new AgentMessage() }
            },
            new Session 
            { 
                Id = "session2", 
                Name = "Test Session 2", 
                Status = SessionStatus.Paused,
                LastActivityAt = DateTime.UtcNow.AddHours(-2),
                Messages = new List<AgentMessage> { new AgentMessage(), new AgentMessage() }
            }
        };
        SetupMocks(withSessions: true, sessions: testSessions, withAgents: false);
        
        // Act
        var component = RenderComponent<Chat>();
        
        // Assert
        Assert.Contains("Test Session 1", component.Markup);
        Assert.Contains("Test Session 2", component.Markup);
        Assert.Contains("Recent Sessions", component.Markup);
        Assert.Contains("minutes ago", component.Markup); // Relative time
    }

    [Fact]
    public void ChatLandingPage_Should_Show_No_Sessions_Message_When_Empty()
    {
        // Arrange
        SetupMocks(withSessions: false, withAgents: false);
        
        // Act
        var component = RenderComponent<Chat>();
        
        // Assert
        Assert.Contains("No recent sessions", component.Markup);
        Assert.Contains("Start a new chat with an agent below", component.Markup);
    }

    [Fact]
    public void ChatLandingPage_Should_Show_Available_Agents()
    {
        // Arrange
        var testAgents = new List<AgentInfo>
        {
            new AgentInfo 
            { 
                Id = "agent1", 
                Name = "Claude Agent", 
                Type = AgentType.Claude,
                Status = AgentStatus.Ready
            },
            new AgentInfo 
            { 
                Id = "agent2", 
                Name = "Saturn Agent", 
                Type = AgentType.Saturn,
                Status = AgentStatus.Initializing
            }
        };
        SetupMocks(withSessions: false, withAgents: true, agents: testAgents);
        
        // Act
        var component = RenderComponent<Chat>();
        
        // Assert
        Assert.Contains("Claude Agent", component.Markup);
        Assert.Contains("Saturn Agent", component.Markup);
        Assert.Contains("Available Agents", component.Markup);
    }

    [Fact]
    public void ChatLandingPage_Should_Show_No_Agents_Message_When_Empty()
    {
        // Arrange
        SetupMocks(withSessions: false, withAgents: true, agents: new List<AgentInfo>());
        
        // Act
        var component = RenderComponent<Chat>();
        
        // Assert
        Assert.Contains("No agents configured", component.Markup);
        Assert.Contains("Create your first agent to start chatting", component.Markup);
    }

    [Fact]
    public void ChatLandingPage_Should_Navigate_To_Session_When_Clicked()
    {
        // Arrange
        var testSessions = new List<Session>
        {
            new Session 
            { 
                Id = "session1", 
                Name = "Test Session", 
                Status = SessionStatus.Active,
                LastActivityAt = DateTime.UtcNow
            }
        };
        SetupMocks(withSessions: true, sessions: testSessions, withAgents: false);
        
        // Act
        var component = RenderComponent<Chat>();
        var sessionCard = component.Find(".cursor-pointer");
        sessionCard.Click();
        
        // Assert
        _mockNavigationManager.Received(1).NavigateTo("/session/session1");
    }

    [Fact]
    public void ChatLandingPage_Should_Navigate_To_Agent_Chat_When_Clicked()
    {
        // Arrange
        var testAgents = new List<AgentInfo>
        {
            new AgentInfo 
            { 
                Id = "agent1", 
                Name = "Test Agent", 
                Type = AgentType.Claude,
                Status = AgentStatus.Ready
            }
        };
        SetupMocks(withSessions: false, withAgents: true, agents: testAgents);
        
        // Act
        var component = RenderComponent<Chat>();
        var agentCards = component.FindAll(".cursor-pointer");
        // Find the agent card (not session card)
        var agentCard = agentCards.FirstOrDefault(c => c.TextContent.Contains("Test Agent"));
        agentCard?.Click();
        
        // Assert
        _mockNavigationManager.Received(1).NavigateTo("/chat/agent1");
    }

    [Fact]
    public void ChatLandingPage_Should_Show_Quick_Actions()
    {
        // Arrange
        SetupMocks(withSessions: false, withAgents: false);
        
        // Act
        var component = RenderComponent<Chat>();
        
        // Assert
        Assert.Contains("Quick Actions", component.Markup);
        Assert.Contains("New Orchestrated Session", component.Markup);
        Assert.Contains("New Team Session", component.Markup);
        Assert.Contains("Quick Chat", component.Markup);
    }

    [Fact]
    public void ChatLandingPage_Should_Navigate_To_Orchestrator_For_New_Session()
    {
        // Arrange
        SetupMocks(withSessions: false, withAgents: false);
        
        // Act
        var component = RenderComponent<Chat>();
        var orchestratorButton = component.FindAll("button")
            .FirstOrDefault(b => b.TextContent.Contains("New Orchestrated Session"));
        orchestratorButton?.Click();
        
        // Assert
        _mockNavigationManager.Received(1).NavigateTo("/orchestrator");
    }

    [Fact]
    public void ChatLandingPage_Should_Navigate_To_Default_Agent_For_Quick_Chat()
    {
        // Arrange
        var defaultAgent = new AgentInfo 
        { 
            Id = "default-agent", 
            Name = "Default Agent", 
            Type = AgentType.Claude,
            Status = AgentStatus.Ready
        };
        var testAgents = new List<AgentInfo> { defaultAgent };
        
        _mockAgentService.GetConfiguredAgentsAsync().Returns(Task.FromResult(testAgents));
        _mockAgentService.GetDefaultAgentAsync().Returns(Task.FromResult(defaultAgent));
        _mockSessionManager.GetRecentSessionsAsync(Arg.Any<int>()).Returns(Task.FromResult(new List<Session>()));
        
        // Act
        var component = RenderComponent<Chat>();
        var quickChatButton = component.FindAll("button")
            .FirstOrDefault(b => b.TextContent.Contains("Quick Chat"));
        quickChatButton?.Click();
        
        // Assert
        _mockNavigationManager.Received(1).NavigateTo("/chat/default-agent");
    }

    [Fact]
    public void ChatLandingPage_Should_Show_View_All_Sessions_Link()
    {
        // Arrange
        var testSessions = new List<Session>
        {
            new Session 
            { 
                Id = "session1", 
                Name = "Test Session", 
                Status = SessionStatus.Active,
                LastActivityAt = DateTime.UtcNow
            }
        };
        SetupMocks(withSessions: true, sessions: testSessions, withAgents: false);
        
        // Act
        var component = RenderComponent<Chat>();
        
        // Assert
        Assert.Contains("View All Sessions", component.Markup);
    }

    [Fact]
    public void ChatLandingPage_Should_Navigate_To_Sessions_Page_When_ViewAll_Clicked()
    {
        // Arrange
        var testSessions = new List<Session>
        {
            new Session 
            { 
                Id = "session1", 
                Name = "Test Session", 
                Status = SessionStatus.Active,
                LastActivityAt = DateTime.UtcNow
            }
        };
        SetupMocks(withSessions: true, sessions: testSessions, withAgents: false);
        
        // Act
        var component = RenderComponent<Chat>();
        var viewAllButton = component.FindAll("button")
            .FirstOrDefault(b => b.TextContent.Contains("View All Sessions"));
        viewAllButton?.Click();
        
        // Assert
        _mockNavigationManager.Received(1).NavigateTo("/sessions");
    }

    private void SetupMocks(
        bool withSessions, 
        List<Session>? sessions = null, 
        bool withAgents = false, 
        List<AgentInfo>? agents = null)
    {
        if (withSessions)
        {
            _mockSessionManager.GetRecentSessionsAsync(Arg.Any<int>())
                .Returns(Task.FromResult(sessions ?? new List<Session>()));
        }
        else
        {
            _mockSessionManager.GetRecentSessionsAsync(Arg.Any<int>())
                .Returns(Task.FromResult(new List<Session>()));
        }

        if (withAgents)
        {
            _mockAgentService.GetConfiguredAgentsAsync()
                .Returns(Task.FromResult(agents ?? new List<AgentInfo>()));
        }
        else
        {
            _mockAgentService.GetConfiguredAgentsAsync()
                .Returns(Task.FromResult(new List<AgentInfo>()));
        }
        
        // Default agent is null unless specified
        _mockAgentService.GetDefaultAgentAsync()
            .Returns(Task.FromResult<AgentInfo?>(null));
    }
}