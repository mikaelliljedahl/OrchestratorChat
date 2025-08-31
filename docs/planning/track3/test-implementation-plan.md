# Track 3: Web UI Test Implementation Plan

## Overview
This document outlines the test implementation strategy for Track 3 Web UI components using bUnit for Blazor component testing. Focus is on critical components that ensure core functionality renders correctly.

## Test Framework Setup

### Required NuGet Packages
```xml
<PackageReference Include="bunit" Version="1.24.10" />
<PackageReference Include="xunit" Version="2.5.3" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
<PackageReference Include="NSubstitute" Version="5.1.0" />
```

### Test Project Structure
```
tests/OrchestratorChat.Web.Tests/
├── OrchestratorChat.Web.Tests.csproj
├── TestHelpers/
│   ├── TestAuthenticationStateProvider.cs
│   ├── MockServiceFactory.cs
│   └── TestDataFactory.cs
├── Components/
│   ├── MessageBubbleTests.cs
│   ├── AgentCardTests.cs
│   ├── SessionIndicatorTests.cs
│   ├── MessageInputTests.cs
│   └── OrchestrationTimelineTests.cs
├── Pages/
│   ├── DashboardTests.cs
│   ├── ChatInterfaceTests.cs
│   └── OrchestratorTests.cs
└── Services/
    ├── AgentServiceTests.cs
    ├── SessionServiceTests.cs
    └── OrchestrationServiceTests.cs
```

## Priority 1: Critical Components (MUST TEST)

### 1. MessageBubble Component
**File**: `Components/MessageBubbleTests.cs`
**Critical Tests**:
- ✅ Renders message content correctly
- ✅ Shows correct sender information
- ✅ Displays timestamp
- ✅ Handles null/empty content gracefully
- ✅ Renders attachments when present

**Test Example**:
```csharp
[Fact]
public void MessageBubble_Should_Render_Content()
{
    // Arrange
    using var ctx = new TestContext();
    var message = new ChatMessage 
    { 
        Content = "Test message",
        Sender = "TestAgent",
        Timestamp = DateTime.Now
    };
    
    // Act
    var component = ctx.RenderComponent<MessageBubble>(
        parameters => parameters.Add(p => p.Message, message));
    
    // Assert
    Assert.Contains("Test message", component.Markup);
    Assert.Contains("TestAgent", component.Markup);
}
```

### 2. ChatInterface Page
**File**: `Pages/ChatInterfaceTests.cs`
**Critical Tests**:
- ✅ Page renders without errors
- ✅ Message list displays
- ✅ Input area is present
- ✅ Send button exists and is clickable
- ✅ SignalR connection initiated on load

**Test Example**:
```csharp
[Fact]
public void ChatInterface_Should_Render_MessageArea()
{
    // Arrange
    using var ctx = new TestContext();
    ctx.Services.AddSingleton(CreateMockSessionService());
    ctx.Services.AddSingleton(CreateMockAgentService());
    
    // Act
    var component = ctx.RenderComponent<ChatInterface>();
    
    // Assert
    Assert.NotNull(component.Find(".message-area"));
    Assert.NotNull(component.Find(".message-input"));
}
```

### 3. Dashboard Page
**File**: `Pages/DashboardTests.cs`
**Critical Tests**:
- ✅ Renders agent cards
- ✅ Shows session statistics
- ✅ Create session button present
- ✅ Navigation menu renders

### 4. SessionIndicator Component
**File**: `Components/SessionIndicatorTests.cs`
**Critical Tests**:
- ✅ Shows active session status
- ✅ Displays session name
- ✅ Shows participant count
- ✅ Handles null session gracefully

## Priority 2: Important Components (SHOULD TEST)

### 5. AgentCard Component
**File**: `Components/AgentCardTests.cs`
**Tests**:
- ✅ Displays agent name and status
- ✅ Shows capabilities
- ✅ Status indicator color correct

### 6. OrchestrationTimeline Component
**File**: `Components/OrchestrationTimelineTests.cs`
**Tests**:
- ✅ Renders timeline structure
- ✅ Shows step information
- ✅ Progress indicators work

### 7. MessageInput Component
**File**: `Components/MessageInputTests.cs`
**Tests**:
- ✅ Input field renders
- ✅ Send button enabled/disabled correctly
- ✅ Attachment button present
- ✅ OnSendMessage callback triggered

## Priority 3: Service Layer Tests

### 8. SessionService Tests
**File**: `Services/SessionServiceTests.cs`
**Tests**:
- ✅ GetCurrentSessionAsync returns data
- ✅ CreateSessionAsync creates session
- ✅ Repository methods called correctly

### 9. AgentService Tests
**File**: `Services/AgentServiceTests.cs`
**Tests**:
- ✅ GetAgentsAsync returns list
- ✅ SendMessageAsync processes message
- ✅ Error handling works

## Test Helpers Implementation

### MockServiceFactory.cs
```csharp
public static class MockServiceFactory
{
    public static ISessionService CreateMockSessionService()
    {
        var mock = Substitute.For<ISessionService>();
        mock.GetCurrentSessionAsync().Returns(Task.FromResult(
            new Session { Id = "test-1", Name = "Test Session" }));
        return mock;
    }
    
    public static IAgentService CreateMockAgentService()
    {
        var mock = Substitute.For<IAgentService>();
        mock.GetAgentsAsync().Returns(Task.FromResult(
            new List<AgentInfo> 
            { 
                new() { Id = "agent-1", Name = "Claude" } 
            }));
        return mock;
    }
}
```

### TestDataFactory.cs
```csharp
public static class TestDataFactory
{
    public static ChatMessage CreateMessage(string content = "Test")
    {
        return new ChatMessage
        {
            Id = Guid.NewGuid().ToString(),
            Content = content,
            Sender = "TestAgent",
            Timestamp = DateTime.UtcNow,
            MessageType = MessageType.Text
        };
    }
    
    public static Session CreateSession()
    {
        return new Session
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Session",
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            ParticipantAgentIds = new List<string> { "agent-1" }
        };
    }
}
```

## Implementation Steps

### Step 1: Create Test Project (30 min)
```bash
cd tests/OrchestratorChat.Web.Tests
dotnet new xunit
dotnet add package bunit
dotnet add package NSubstitute
dotnet add package MudBlazor
dotnet add reference ../../src/OrchestratorChat.Web/OrchestratorChat.Web.csproj
```

### Step 2: Implement Test Helpers (45 min)
- Create MockServiceFactory with all service mocks
- Create TestDataFactory for test data
- Create TestAuthenticationStateProvider for auth scenarios

### Step 3: Write Component Tests (2-3 hours)
Priority order:
1. MessageBubble (30 min)
2. SessionIndicator (20 min)
3. AgentCard (20 min)
4. MessageInput (30 min)
5. OrchestrationTimeline (30 min)

### Step 4: Write Page Tests (2-3 hours)
Priority order:
1. ChatInterface (1 hour)
2. Dashboard (45 min)
3. Orchestrator (45 min)

### Step 5: Write Service Tests (1-2 hours)
1. SessionService (30 min)
2. AgentService (30 min)
3. OrchestrationService (30 min)

## Success Criteria

### Minimum Coverage (MUST HAVE)
- ✅ All Priority 1 components have rendering tests
- ✅ All pages load without errors
- ✅ Core user interactions tested (send message, create session)
- ✅ 50% code coverage for components

### Good Coverage (NICE TO HAVE)
- ✅ Priority 2 components tested
- ✅ Service layer has unit tests
- ✅ Error scenarios covered
- ✅ 70% code coverage

## Test Execution

### Run All Tests
```bash
dotnet test tests/OrchestratorChat.Web.Tests
```

### Run with Coverage
```bash
dotnet test tests/OrchestratorChat.Web.Tests --collect:"XPlat Code Coverage"
```

### Run Specific Category
```bash
dotnet test --filter Category=Critical
```

## Common bUnit Patterns

### Testing MudBlazor Components
```csharp
[Fact]
public void Should_Render_MudButton()
{
    using var ctx = new TestContext();
    ctx.Services.AddMudServices();
    
    var component = ctx.RenderComponent<MyComponent>();
    var button = component.Find(".mud-button");
    
    Assert.NotNull(button);
}
```

### Testing Event Callbacks
```csharp
[Fact]
public void Should_Trigger_Callback()
{
    using var ctx = new TestContext();
    var callbackTriggered = false;
    
    var component = ctx.RenderComponent<MessageInput>(
        parameters => parameters
            .Add(p => p.OnSendMessage, (string msg) => { 
                callbackTriggered = true; 
                return Task.CompletedTask; 
            }));
    
    component.Find("button").Click();
    
    Assert.True(callbackTriggered);
}
```

### Testing Async Operations
```csharp
[Fact]
public async Task Should_Load_Data()
{
    using var ctx = new TestContext();
    var mockService = Substitute.For<ISessionService>();
    mockService.GetCurrentSessionAsync()
        .Returns(Task.FromResult(new Session()));
    
    ctx.Services.AddSingleton(mockService);
    
    var component = ctx.RenderComponent<SessionIndicator>();
    
    // Wait for async operations
    await Task.Delay(100);
    
    Assert.Contains("Active", component.Markup);
}
```

## Potential Issues & Solutions

### Issue 1: SignalR Dependencies
**Problem**: Components depend on SignalR connections
**Solution**: Mock IHubConnectionManager and return fake connections

### Issue 2: Navigation Dependencies
**Problem**: NavigationManager required for page navigation
**Solution**: Use bUnit's built-in NavigationManager mock

### Issue 3: JavaScript Interop
**Problem**: Some components may use JSInterop
**Solution**: Use bUnit's JSInterop mock setup

### Issue 4: Authentication State
**Problem**: Some pages require authentication
**Solution**: Use TestAuthenticationStateProvider with fake user

## Definition of Done

- [ ] Test project created and references configured
- [ ] All Priority 1 components have tests
- [ ] All pages have basic rendering tests
- [ ] Tests run successfully in CI/CD pipeline
- [ ] No failing tests
- [ ] Coverage report generated
- [ ] Documentation updated with test running instructions

## Time Estimate

**Total Estimated Time**: 6-8 hours

- Project Setup: 30 minutes
- Test Helpers: 45 minutes
- Component Tests: 2-3 hours
- Page Tests: 2-3 hours
- Service Tests: 1-2 hours
- Documentation: 30 minutes

## Notes for Implementation

1. **Keep tests simple**: Focus on "does it render" rather than complex logic
2. **Use mocks liberally**: Don't test external dependencies
3. **Test user perspective**: Test what users see and do
4. **Avoid implementation details**: Don't test private methods or internal state
5. **Fast feedback**: Tests should run quickly (< 5 seconds total)

---

*This plan provides comprehensive test coverage for critical UI components while keeping scope manageable.*