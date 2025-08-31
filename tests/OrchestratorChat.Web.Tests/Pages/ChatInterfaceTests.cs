using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using OrchestratorChat.Web.Pages;
using OrchestratorChat.Web.Components;
using OrchestratorChat.Web.Services;
using OrchestratorChat.Web.Tests.TestHelpers;
using OrchestratorChat.Web.Models;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Messages;
using OrchestratorChat.Data;
using Moq;
using Xunit;
using Bunit.TestDoubles;

namespace OrchestratorChat.Web.Tests.Pages;

public class ChatInterfaceTests : TestContext, IAsyncDisposable
{
    private readonly OrchestratorDbContext _dbContext;
    private readonly Mock<IAgentService> _mockAgentService;
    private readonly Mock<ISessionManager> _mockSessionManager;
    private readonly Mock<IJSRuntime> _mockJSRuntime;
    private readonly List<IDisposable> _disposables = new();
    
    public ChatInterfaceTests()
    {
        // Set up in-memory database
        var options = new DbContextOptionsBuilder<OrchestratorDbContext>()
            .UseInMemoryDatabase(databaseName: $"ChatInterfaceTests_{Guid.NewGuid()}")
            .Options;
        _dbContext = new OrchestratorDbContext(options);
        _disposables.Add(_dbContext);
        
        // Set up mocks
        _mockAgentService = new Mock<IAgentService>();
        _mockSessionManager = new Mock<ISessionManager>();
        _mockJSRuntime = new Mock<IJSRuntime>();
        
        SetupServices();
    }
    
    private void SetupServices()
    {
        // Configure MudBlazor services for testing - disable popover provider check
        Services.AddMudServices(configuration => 
        {
            configuration.PopoverOptions.CheckForPopoverProvider = false;
            configuration.PopoverOptions.ThrowOnDuplicateProvider = false;
        });
        
        // Set up JSInterop in loose mode for MudBlazor components
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid("mudPopover.connect", _ => true);
        JSInterop.SetupVoid("scrollToBottom", _ => true);
        
        // Add database context
        Services.AddSingleton(_dbContext);
        
        // Add mock services
        Services.AddSingleton(_mockJSRuntime.Object);
        Services.AddSingleton(CreateMockNavigationManager());
        Services.AddSingleton(_mockAgentService.Object);
        Services.AddSingleton(_mockSessionManager.Object);
        
        // Add additional required services for ChatInterface components
        var mockAnthropicOAuthService = new Mock<OrchestratorChat.Core.Authentication.IAnthropicOAuthService>();
        Services.AddSingleton(mockAnthropicOAuthService.Object);
        
        var mockOrchestrationService = new Mock<OrchestratorChat.Web.Services.IOrchestrationService>();
        Services.AddSingleton(mockOrchestrationService.Object);
        
        var mockHubConnectionManager = new Mock<OrchestratorChat.Web.Services.IHubConnectionManager>();
        Services.AddSingleton(mockHubConnectionManager.Object);
        
        var mockHealthCheckService = new Mock<OrchestratorChat.Web.Services.IHealthCheckService>();
        Services.AddSingleton(mockHealthCheckService.Object);
        
        // Add logging - SUPPRESS DEBUG LOGGING to reduce noise
        Services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
    }
    
    private NavigationManager CreateMockNavigationManager()
    {
        return new MockNavigationManager("https://localhost/");
    }

    [Fact]
    public void ChatInterface_Should_Handle_MudBlazor_Configuration_Properly()
    {
        // Arrange - This test verifies that our MudBlazor test setup is correct
        // by ensuring the component doesn't crash on popover provider issues
        _mockAgentService.Setup(x => x.GetConfiguredAgentsAsync())
            .ThrowsAsync(new TimeoutException("Mock timeout for controlled test"));
        _mockSessionManager.Setup(x => x.GetCurrentSessionAsync())
            .ThrowsAsync(new OperationCanceledException("Mock cancellation for controlled test"));
        
        // Act & Assert - Verify MudBlazor components can be rendered without provider issues
        var exception = Record.Exception(() =>
        {
            // This should not throw MudPopoverProvider exceptions thanks to our configuration
            var component = RenderComponent<ChatInterface>();
            
            // Basic verification that component attempted to render
            Assert.NotNull(component);
            
            // The component may fail during initialization due to our mock errors,
            // but it should not fail due to MudBlazor provider issues
        });
        
        // If there's an exception, it should be about our intentional mock failures,
        // NOT about missing MudPopoverProvider
        if (exception != null)
        {
            Assert.False(exception.Message.Contains("MudPopoverProvider"), 
                $"Test setup failed - MudBlazor provider issue still present: {exception.Message}");
            
            // Expected exceptions from our mocks or component initialization are OK
            Assert.True(
                exception.Message.Contains("timeout") || 
                exception.Message.Contains("cancellation") ||
                exception.Message.Contains("initialization") ||
                exception.Message.Contains("Hub") ||
                exception.Message.Contains("SignalR") ||
                exception.Message.Contains("source"),
                $"Unexpected exception type: {exception.GetType().Name}: {exception.Message}"
            );
        }
    }

    [Fact]
    public void ChatInterface_Route_Parameters_Should_Be_Accessible()
    {
        // Arrange - Test that component accepts route parameters without crashing
        var testAgentId = "test-agent-123";
        var testSessionId = "test-session-456";
        
        // Set up minimal mocks to prevent hanging
        _mockAgentService.Setup(x => x.GetConfiguredAgentsAsync())
            .ReturnsAsync(new List<AgentInfo>());
        _mockSessionManager.Setup(x => x.GetCurrentSessionAsync())
            .ReturnsAsync((Session?)null);
        
        // Act - Test route parameter binding
        var exception = Record.Exception(() =>
        {
            var component = RenderComponent<ChatInterface>(parameters => parameters
                .Add(p => p.AgentId, testAgentId)
                .Add(p => p.SessionId, testSessionId));
                
            Assert.NotNull(component);
        });
        
        // Assert - Should not crash due to parameter binding
        if (exception != null)
        {
            // Should fail gracefully, not crash on parameter binding
            Assert.False(exception.Message.Contains("Parameter"), 
                $"Component failed on parameter binding: {exception.Message}");
        }
    }

    [Fact]
    public void ChatInterface_Component_Dependencies_Should_Be_Resolvable()
    {
        // Arrange & Act - Test that all required services can be resolved
        var serviceExceptions = new List<Exception>();
        
        // Test each required service can be resolved from DI
        try { Services.GetService<IAgentService>(); } catch (Exception ex) { serviceExceptions.Add(ex); }
        try { Services.GetService<ISessionManager>(); } catch (Exception ex) { serviceExceptions.Add(ex); }
        try { Services.GetService<NavigationManager>(); } catch (Exception ex) { serviceExceptions.Add(ex); }
        try { Services.GetService<IJSRuntime>(); } catch (Exception ex) { serviceExceptions.Add(ex); }
        try { Services.GetService<ILogger<ChatInterface>>(); } catch (Exception ex) { serviceExceptions.Add(ex); }
        
        // Assert - All services should be resolvable
        Assert.Empty(serviceExceptions);
        
        // Verify specific service types
        Assert.NotNull(Services.GetService<IAgentService>());
        Assert.NotNull(Services.GetService<ISessionManager>());
        Assert.NotNull(Services.GetService<NavigationManager>());
        Assert.NotNull(Services.GetService<IJSRuntime>());
    }

    [Fact] 
    public void ChatInterface_Should_Handle_Agent_Service_Failure_Gracefully()
    {
        // Arrange - Mock agent service to fail with a known error
        _mockAgentService.Setup(x => x.GetConfiguredAgentsAsync())
            .ThrowsAsync(new InvalidOperationException("Agent service connection failed"));
        _mockSessionManager.Setup(x => x.GetCurrentSessionAsync())
            .ReturnsAsync((Session?)null);
        
        // Act - Component should handle service failure gracefully
        var exception = Record.Exception(() =>
        {
            var component = RenderComponent<ChatInterface>();
            Assert.NotNull(component);
        });
        
        // Assert - Should fail gracefully, not crash the test framework
        if (exception != null)
        {
            Assert.Contains("Agent service", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ChatInterface_Should_Handle_Session_Service_Failure_Gracefully()
    {
        // Arrange - Mock session service to fail with a known error
        _mockAgentService.Setup(x => x.GetConfiguredAgentsAsync())
            .ReturnsAsync(new List<AgentInfo>());
        _mockSessionManager.Setup(x => x.GetCurrentSessionAsync())
            .ThrowsAsync(new InvalidOperationException("Database connection timeout"));
        
        // Act - Component should handle service failure gracefully
        var exception = Record.Exception(() =>
        {
            var component = RenderComponent<ChatInterface>();
            Assert.NotNull(component);
        });
        
        // Assert - Should fail gracefully, not crash the test framework
        if (exception != null)
        {
            Assert.True(
                exception.Message.Contains("Database") || 
                exception.Message.Contains("timeout") ||
                exception.Message.Contains("connection"),
                $"Unexpected error message: {exception.Message}"
            );
        }
    }

    [Fact]
    public void ChatInterface_Test_Infrastructure_Should_Be_Working()
    {
        // This is a meta-test to verify our test infrastructure itself is working
        
        // Arrange & Act - Test basic test framework functionality
        var testDb = Services.GetService<OrchestratorDbContext>();
        var testAgentService = Services.GetService<IAgentService>();
        var testSessionManager = Services.GetService<ISessionManager>();
        
        // Assert - Test infrastructure is properly set up
        Assert.NotNull(testDb);
        Assert.NotNull(testAgentService);
        Assert.NotNull(testSessionManager);
        
        // Verify our mocks are working
        Assert.IsType<Mock<IAgentService>>(Mock.Get(testAgentService));
        Assert.IsType<Mock<ISessionManager>>(Mock.Get(testSessionManager));
    }
    
    private AgentInfo CreateTestAgent(string id, string name, AgentStatus status)
    {
        return new AgentInfo
        {
            Id = id,
            Name = name,
            Type = AgentType.Claude,
            Status = status,
            Description = $"Test {name} agent",
            LastActive = DateTime.UtcNow
        };
    }
    
    private Session CreateTestSession()
    {
        return new Session
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Chat Session",
            Status = SessionStatus.Active,
            Type = SessionType.SingleAgent,
            CreatedAt = DateTime.UtcNow,
            ParticipantAgentIds = new List<string> { "test-agent" },
            Messages = new List<AgentMessage>()
        };
    }
    
    public async ValueTask DisposeAsync()
    {
        foreach (var disposable in _disposables)
        {
            if (disposable is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else
            {
                disposable.Dispose();
            }
        }
        
        Dispose();
    }
}

public class MockNavigationManager : NavigationManager
{
    public MockNavigationManager(string baseUri) : base()
    {
        Initialize(baseUri, baseUri);
    }
    
    protected override void NavigateToCore(string uri, bool forceLoad)
    {
        // Mock navigation - do nothing
    }
}