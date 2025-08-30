# Track 4: SignalR Integration Tests - Implementation Instructions

## Overview
This document provides comprehensive instructions for implementing integration tests for the SignalR & Orchestration layer (Track 4). The tests will validate real-time communication between Web UI and backend services through SignalR hubs.

## Test Project Structure

### 1. Create Test Project
**Location**: `tests/OrchestratorChat.SignalR.IntegrationTests/`

```
tests/OrchestratorChat.SignalR.IntegrationTests/
├── OrchestratorChat.SignalR.IntegrationTests.csproj
├── Fixtures/
│   ├── SignalRTestFixture.cs
│   └── TestWebApplicationFactory.cs
├── Hubs/
│   ├── OrchestratorHubTests.cs
│   └── AgentHubTests.cs
├── Services/
│   ├── MessageRouterTests.cs
│   └── ConnectionManagerTests.cs
├── Integration/
│   ├── EndToEndMessageFlowTests.cs
│   ├── EventBusIntegrationTests.cs
│   └── WebUIIntegrationTests.cs
├── Helpers/
│   ├── SignalRTestClient.cs
│   ├── MockAgentFactory.cs
│   └── TestDataBuilder.cs
└── appsettings.test.json
```

## Implementation Requirements

### 1. Project File Configuration
**File**: `OrchestratorChat.SignalR.IntegrationTests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <!-- Testing Framework -->
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.9.0" />
    <PackageReference Include="xunit" Version="2.6.6" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.6" />
    
    <!-- SignalR Testing -->
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="8.0.0" />
    
    <!-- Mocking and Assertions -->
    <PackageReference Include="Moq" Version="4.20.70" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    
    <!-- Project References -->
    <ProjectReference Include="..\..\src\OrchestratorChat.SignalR\OrchestratorChat.SignalR.csproj" />
    <ProjectReference Include="..\..\src\OrchestratorChat.Core\OrchestratorChat.Core.csproj" />
    <ProjectReference Include="..\..\src\OrchestratorChat.Web\OrchestratorChat.Web.csproj" />
  </ItemGroup>
</Project>
```

### 2. Test Fixtures

#### SignalRTestFixture.cs
```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OrchestratorChat.SignalR.IntegrationTests.Fixtures;

public class SignalRTestFixture : WebApplicationFactory<Program>
{
    public HubConnection CreateHubConnection(string hubPath)
    {
        var client = WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Add test-specific services
                services.AddSignalR();
            });
        }).CreateDefaultClient();

        var connection = new HubConnectionBuilder()
            .WithUrl($"http://localhost{hubPath}", options =>
            {
                options.HttpMessageHandlerFactory = _ => Server.CreateHandler();
            })
            .Build();

        return connection;
    }
    
    public async Task<(HubConnection orchestrator, HubConnection agent)> CreateConnectedHubsAsync()
    {
        var orchestratorHub = CreateHubConnection("/hubs/orchestrator");
        var agentHub = CreateHubConnection("/hubs/agent");
        
        await orchestratorHub.StartAsync();
        await agentHub.StartAsync();
        
        return (orchestratorHub, agentHub);
    }
}
```

### 3. Hub Integration Tests

#### OrchestratorHubTests.cs
```csharp
namespace OrchestratorChat.SignalR.IntegrationTests.Hubs;

public class OrchestratorHubTests : IClassFixture<SignalRTestFixture>
{
    private readonly SignalRTestFixture _fixture;

    public OrchestratorHubTests(SignalRTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateSession_ShouldCreateAndJoinSession()
    {
        // Arrange
        var connection = _fixture.CreateHubConnection("/hubs/orchestrator");
        await connection.StartAsync();
        
        var sessionCreated = new TaskCompletionSource<Session>();
        connection.On<Session>("SessionCreated", session =>
        {
            sessionCreated.SetResult(session);
        });

        // Act
        var response = await connection.InvokeAsync<SessionCreatedResponse>(
            "CreateSession",
            new CreateSessionRequest
            {
                Name = "Test Session",
                Type = SessionType.MultiAgent,
                AgentIds = new List<string> { "agent-1" }
            });

        // Assert
        response.Success.Should().BeTrue();
        response.SessionId.Should().NotBeNullOrEmpty();
        
        var session = await sessionCreated.Task.WaitAsync(TimeSpan.FromSeconds(5));
        session.Should().NotBeNull();
        session.Name.Should().Be("Test Session");
    }

    [Fact]
    public async Task SendOrchestrationMessage_ShouldCreatePlanAndExecute()
    {
        // Test orchestration message flow
    }

    [Fact]
    public async Task JoinSession_ShouldAddClientToSessionGroup()
    {
        // Test session joining
    }
}
```

#### AgentHubTests.cs
```csharp
namespace OrchestratorChat.SignalR.IntegrationTests.Hubs;

public class AgentHubTests : IClassFixture<SignalRTestFixture>
{
    [Fact]
    public async Task SendAgentMessage_ShouldStreamResponses()
    {
        // Arrange
        var connection = _fixture.CreateHubConnection("/hubs/agent");
        await connection.StartAsync();
        
        var responses = new List<AgentResponseDto>();
        connection.On<AgentResponseDto>("ReceiveAgentResponse", response =>
        {
            responses.Add(response);
        });

        // Act
        await connection.InvokeAsync("SendAgentMessage", new AgentMessageRequest
        {
            AgentId = "test-agent",
            SessionId = "test-session",
            Content = "Test message"
        });

        // Assert
        await Task.Delay(1000); // Wait for streaming
        responses.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExecuteTool_ShouldReturnToolResult()
    {
        // Test tool execution
    }

    [Fact]
    public async Task SubscribeToAgent_ShouldReceiveStatusUpdates()
    {
        // Test agent subscription
    }
}
```

### 4. Service Integration Tests

#### MessageRouterTests.cs
```csharp
namespace OrchestratorChat.SignalR.IntegrationTests.Services;

public class MessageRouterTests
{
    [Fact]
    public async Task RouteAgentMessage_ShouldDeliverToSessionClients()
    {
        // Test message routing to session groups
    }

    [Fact]
    public async Task RouteOrchestrationUpdate_ShouldDeliverToAllClients()
    {
        // Test orchestration update routing
    }
}
```

#### ConnectionManagerTests.cs
```csharp
namespace OrchestratorChat.SignalR.IntegrationTests.Services;

public class ConnectionManagerTests
{
    [Fact]
    public async Task AddUserToSession_ShouldTrackUserSession()
    {
        // Test user-session tracking
    }

    [Fact]
    public async Task RemoveUserFromSession_ShouldCleanupProperly()
    {
        // Test cleanup on disconnect
    }
}
```

### 5. End-to-End Integration Tests

#### EndToEndMessageFlowTests.cs
```csharp
namespace OrchestratorChat.SignalR.IntegrationTests.Integration;

public class EndToEndMessageFlowTests : IClassFixture<SignalRTestFixture>
{
    [Fact]
    public async Task CompleteMessageFlow_FromWebUIToAgent_ShouldWork()
    {
        // Arrange
        var (orchestratorHub, agentHub) = await _fixture.CreateConnectedHubsAsync();
        
        // 1. Create session via orchestrator
        var sessionResponse = await orchestratorHub.InvokeAsync<SessionCreatedResponse>(
            "CreateSession",
            new CreateSessionRequest { Name = "E2E Test" });
        
        // 2. Subscribe to agent responses
        var responseReceived = new TaskCompletionSource<AgentResponseDto>();
        agentHub.On<AgentResponseDto>("ReceiveAgentResponse", response =>
        {
            responseReceived.SetResult(response);
        });
        
        // 3. Send message to agent
        await agentHub.InvokeAsync("SendAgentMessage", new AgentMessageRequest
        {
            SessionId = sessionResponse.SessionId,
            AgentId = "test-agent",
            Content = "Test message"
        });
        
        // 4. Verify response received
        var response = await responseReceived.Task.WaitAsync(TimeSpan.FromSeconds(10));
        response.Should().NotBeNull();
        response.SessionId.Should().Be(sessionResponse.SessionId);
    }

    [Fact]
    public async Task MultipleClients_InSameSession_ShouldReceiveUpdates()
    {
        // Test broadcasting to multiple clients in same session
    }

    [Fact]
    public async Task OrchestrationPlan_Execution_ShouldReportProgress()
    {
        // Test orchestration progress reporting
    }
}
```

#### EventBusIntegrationTests.cs
```csharp
namespace OrchestratorChat.SignalR.IntegrationTests.Integration;

public class EventBusIntegrationTests : IClassFixture<SignalRTestFixture>
{
    [Fact]
    public async Task AgentStatusChangedEvent_ShouldPropagateToSignalRClients()
    {
        // Arrange
        var connection = _fixture.CreateHubConnection("/hubs/agent");
        await connection.StartAsync();
        
        var statusUpdate = new TaskCompletionSource<AgentStatusDto>();
        connection.On<AgentStatusDto>("AgentStatusUpdate", status =>
        {
            statusUpdate.SetResult(status);
        });
        
        // Act - Trigger event through Core's IEventBus
        var eventBus = _fixture.Services.GetRequiredService<IEventBus>();
        await eventBus.PublishAsync(new AgentStatusChangedEvent
        {
            AgentId = "test-agent",
            NewStatus = AgentStatus.Ready
        });
        
        // Assert
        var status = await statusUpdate.Task.WaitAsync(TimeSpan.FromSeconds(5));
        status.Should().NotBeNull();
        status.Status.Should().Be(AgentStatus.Ready);
    }

    [Fact]
    public async Task MessageReceivedEvent_ShouldRouteToSession()
    {
        // Test message event routing
    }
}
```

#### WebUIIntegrationTests.cs
```csharp
namespace OrchestratorChat.SignalR.IntegrationTests.Integration;

public class WebUIIntegrationTests : IClassFixture<SignalRTestFixture>
{
    [Fact]
    public async Task WebUI_ChatComponent_ShouldConnectToSignalR()
    {
        // Test Web UI components can connect
    }

    [Fact]
    public async Task WebUI_SessionList_ShouldUpdateInRealTime()
    {
        // Test real-time session updates
    }

    [Fact]
    public async Task WebUI_OrchestrationTimeline_ShouldReceiveProgress()
    {
        // Test orchestration timeline updates
    }
}
```

### 6. Test Helpers

#### SignalRTestClient.cs
```csharp
namespace OrchestratorChat.SignalR.IntegrationTests.Helpers;

public class SignalRTestClient : IAsyncDisposable
{
    private readonly HubConnection _orchestratorHub;
    private readonly HubConnection _agentHub;
    
    public SignalRTestClient(string baseUrl)
    {
        _orchestratorHub = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}/hubs/orchestrator")
            .Build();
            
        _agentHub = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}/hubs/agent")
            .Build();
    }
    
    public async Task ConnectAsync()
    {
        await _orchestratorHub.StartAsync();
        await _agentHub.StartAsync();
    }
    
    public async ValueTask DisposeAsync()
    {
        await _orchestratorHub.DisposeAsync();
        await _agentHub.DisposeAsync();
    }
}
```

#### MockAgentFactory.cs
```csharp
namespace OrchestratorChat.SignalR.IntegrationTests.Helpers;

public class MockAgentFactory : IAgentFactory
{
    public Task<IAgent> CreateAgentAsync(AgentType type, AgentConfiguration configuration)
    {
        var mockAgent = new Mock<IAgent>();
        mockAgent.Setup(x => x.Id).Returns($"mock-{type}");
        mockAgent.Setup(x => x.Type).Returns(type);
        mockAgent.Setup(x => x.Status).Returns(AgentStatus.Ready);
        
        // Setup streaming response
        mockAgent.Setup(x => x.SendMessageAsync(It.IsAny<AgentMessage>(), It.IsAny<CancellationToken>()))
            .Returns(CreateMockResponseStream());
        
        return Task.FromResult(mockAgent.Object);
    }
    
    private async Task<IAsyncEnumerable<AgentResponse>> CreateMockResponseStream()
    {
        return AsyncEnumerable.Range(1, 3).Select(i => new AgentResponse
        {
            Content = $"Response {i}",
            Type = ResponseType.Text,
            IsComplete = i == 3
        });
    }
}
```

## Test Execution Strategy

### 1. Test Categories
- **Unit Tests**: Test individual components in isolation
- **Integration Tests**: Test component interactions
- **End-to-End Tests**: Test complete workflows

### 2. Test Data Setup
```csharp
public class TestDataBuilder
{
    public static CreateSessionRequest BuildSessionRequest(string name = "Test Session")
    {
        return new CreateSessionRequest
        {
            Name = name,
            Type = SessionType.MultiAgent,
            AgentIds = new List<string> { "agent-1", "agent-2" },
            WorkingDirectory = Path.GetTempPath()
        };
    }
    
    public static AgentMessage BuildAgentMessage(string sessionId)
    {
        return new AgentMessage
        {
            SessionId = sessionId,
            Content = "Test message",
            Role = MessageRole.User,
            AgentId = "test-agent"
        };
    }
}
```

### 3. Assertions Pattern
```csharp
// Use FluentAssertions for readable assertions
result.Should().NotBeNull();
result.Success.Should().BeTrue();
result.SessionId.Should().MatchRegex(@"^[a-f0-9-]{36}$");

// Async assertions with timeout
await action.Should().CompleteWithinAsync(TimeSpan.FromSeconds(5));

// Collection assertions
responses.Should().HaveCount(3);
responses.Should().ContainSingle(r => r.IsComplete);
```

## Configuration

### appsettings.test.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore.SignalR": "Debug",
      "OrchestratorChat": "Debug"
    }
  },
  "SignalR": {
    "EnableDetailedErrors": true,
    "KeepAliveInterval": 5,
    "ClientTimeoutInterval": 10
  },
  "Testing": {
    "UseInMemoryDatabase": true,
    "MockExternalServices": true
  }
}
```

## Test Scenarios to Cover

### 1. Connection Management
- [x] Client connection and disconnection
- [x] Automatic reconnection
- [x] Multiple concurrent connections
- [x] Connection cleanup on error

### 2. Session Management
- [x] Create new session
- [x] Join existing session
- [x] Leave session
- [x] Session isolation (messages don't leak)

### 3. Message Flow
- [x] Send message to agent
- [x] Receive streaming responses
- [x] Broadcast to session participants
- [x] Error propagation

### 4. Orchestration
- [x] Create orchestration plan
- [x] Execute plan with progress
- [x] Handle plan failures
- [x] Cancel running orchestration

### 5. Event Integration
- [x] Agent events propagate to SignalR
- [x] Orchestration events propagate to SignalR
- [x] Custom events handled correctly

### 6. Web UI Integration
- [x] Blazor components connect to hubs
- [x] Real-time updates in UI
- [x] Form submissions through SignalR
- [x] Error handling in UI

## Success Criteria

1. **All tests pass**: 100% pass rate
2. **Code coverage**: Minimum 80% coverage for SignalR code
3. **Performance**: Hub operations complete within 100ms
4. **Reliability**: Tests are not flaky, consistent results
5. **Integration**: Web UI can successfully communicate through SignalR

## Implementation Notes

1. **Use WebApplicationFactory** for integration testing with real HTTP pipeline
2. **Mock external dependencies** (agents, database) for predictable tests
3. **Use TestServer** for in-memory testing without network overhead
4. **Implement retry logic** for timing-sensitive tests
5. **Clean up resources** properly in test teardown
6. **Run tests in parallel** where possible for speed

## Common Pitfalls to Avoid

1. **Don't test SignalR framework itself** - focus on our implementation
2. **Avoid timing-dependent assertions** - use proper async waiting
3. **Don't share state between tests** - each test should be independent
4. **Mock time-dependent operations** for consistent results
5. **Handle connection disposal properly** to avoid test pollution

## Running the Tests

```bash
# Run all integration tests
dotnet test tests/OrchestratorChat.SignalR.IntegrationTests

# Run specific test category
dotnet test --filter Category=Integration

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run in verbose mode for debugging
dotnet test --logger "console;verbosity=detailed"
```

## Next Steps

1. Implement test project structure
2. Create fixture and helper classes
3. Implement hub tests
4. Implement service tests
5. Implement end-to-end tests
6. Add Web UI integration tests
7. Configure CI/CD to run tests
8. Generate coverage reports