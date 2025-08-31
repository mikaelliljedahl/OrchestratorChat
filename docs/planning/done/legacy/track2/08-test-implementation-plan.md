# Track 2: Test Implementation Plan

## Executive Summary
This document outlines the comprehensive test implementation strategy for Track 2 (Agent Adapters & Saturn), focusing on critical components like tool execution, OAuth flows, and streaming. The plan leverages testing patterns and helpers from the SaturnFork project to ensure consistency and reduce implementation effort.

## Test Coverage Requirements

### Priority 1: Critical Components (Must Have)
1. **Tool Execution System** - The core of agent capabilities
2. **Command Approval Service** - Security-critical component
3. **OAuth Flow** - Authentication is essential
4. **Streaming/SSE** - Real-time response handling
5. **Agent Lifecycle** - Process management and health

### Priority 2: Core Functionality (Should Have)
1. **Provider Implementations** - OpenRouter and Anthropic clients
2. **Agent Factory** - Creation and registry management
3. **Tool Handlers** - Individual tool implementations
4. **Message Processing** - Format conversion and routing

### Priority 3: Supporting Features (Nice to Have)
1. **Token Store** - Encryption and persistence
2. **Health Monitoring** - Timer-based checks
3. **Multi-Agent Tools** - Coordination features

## Test Projects Structure

### 1. OrchestratorChat.Agents.Tests
```
tests/OrchestratorChat.Agents.Tests/
├── OrchestratorChat.Agents.Tests.csproj
├── TestHelpers/
│   ├── FileTestHelper.cs         # Ported from SaturnFork
│   ├── MockProcessHelper.cs      # For ClaudeAgent testing
│   └── TestConstants.cs          # Shared test constants
├── Claude/
│   ├── ClaudeAgentTests.cs
│   ├── ClaudeProcessManagementTests.cs
│   └── ClaudeStreamingTests.cs
├── Saturn/
│   ├── SaturnAgentTests.cs
│   └── SaturnCoreTests.cs
├── Factory/
│   ├── AgentFactoryTests.cs
│   └── RegistryTests.cs
├── Tools/
│   ├── ToolExecutorTests.cs
│   ├── CommandApprovalServiceTests.cs
│   └── Handlers/
│       ├── FileReadHandlerTests.cs
│       ├── FileWriteHandlerTests.cs
│       ├── BashCommandHandlerTests.cs
│       └── WebSearchHandlerTests.cs
└── Monitoring/
    └── AgentHealthMonitorTests.cs
```

### 2. OrchestratorChat.Saturn.Tests
```
tests/OrchestratorChat.Saturn.Tests/
├── OrchestratorChat.Saturn.Tests.csproj
├── TestHelpers/               # Reuse from SaturnFork
│   ├── FileTestHelper.cs
│   ├── MockHttpMessageHandler.cs
│   └── StreamTestHelper.cs
├── Providers/
│   ├── Anthropic/
│   │   ├── AnthropicAuthServiceTests.cs
│   │   ├── AnthropicClientTests.cs
│   │   ├── PKCEGeneratorTests.cs
│   │   └── TokenStoreTests.cs
│   ├── OpenRouter/
│   │   ├── OpenRouterClientTests.cs
│   │   ├── ChatCompletionsServiceTests.cs
│   │   └── ModelsServiceTests.cs
│   └── Streaming/
│       └── SseParserTests.cs
├── Tools/
│   ├── ApplyDiffToolTests.cs
│   ├── DeleteFileToolTests.cs
│   ├── GlobToolTests.cs
│   ├── GrepToolTests.cs
│   ├── ListFilesToolTests.cs
│   └── SearchAndReplaceToolTests.cs
└── MultiAgent/
    ├── CreateAgentToolTests.cs
    ├── HandOffToAgentToolTests.cs
    └── WaitForAgentToolTests.cs
```

## Reusable Components from SaturnFork

### 1. Test Helpers to Port

#### FileTestHelper.cs
```csharp
// Direct port from SaturnFork with namespace updates
public class FileTestHelper : IDisposable
{
    private readonly string _testDirectory;
    
    public FileTestHelper(string testName = null)
    {
        var dirName = testName ?? $"OrchestratorTest_{Guid.NewGuid():N}";
        _testDirectory = Path.Combine(Path.GetTempPath(), dirName);
        Directory.CreateDirectory(_testDirectory);
    }
    
    public string CreateFile(string relativePath, string content)
    public string CreateDirectory(string relativePath)
    public string ReadFile(string relativePath)
    public bool FileExists(string relativePath)
    public void Dispose() // Cleanup temp files
}
```

#### MockHttpMessageHandler.cs
```csharp
// For testing HTTP clients without real network calls
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses;
    
    public void EnqueueResponse(HttpStatusCode status, string content)
    public void EnqueueStreamingResponse(IEnumerable<string> chunks)
    protected override Task<HttpResponseMessage> SendAsync(...)
}
```

### 2. Test Patterns to Adopt

#### Tool Testing Pattern (from SaturnFork)
```csharp
[Fact]
public async Task ExecuteTool_WithValidParameters_ReturnsSuccess()
{
    // Arrange
    using var fileHelper = new FileTestHelper("ToolTest");
    var tool = new ReadFileTool();
    var testFile = fileHelper.CreateFile("test.txt", "content");
    
    var parameters = new Dictionary<string, object>
    {
        { "file_path", testFile }
    };
    
    // Act
    var result = await tool.ExecuteAsync(parameters);
    
    // Assert
    Assert.True(result.Success);
    Assert.Equal("content", result.Output);
}
```

#### OAuth Flow Testing Pattern
```csharp
[Fact]
public async Task AuthenticateAsync_WithValidCode_ReturnsTokens()
{
    // Arrange
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.EnqueueResponse(HttpStatusCode.OK, JsonSerializer.Serialize(new
    {
        access_token = "test_token",
        refresh_token = "refresh_token",
        expires_in = 3600
    }));
    
    var authService = new AnthropicAuthService(mockHttp);
    
    // Act
    var result = await authService.ExchangeCodeForTokensAsync("test_code");
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal("test_token", result.AccessToken);
}
```

## Critical Test Scenarios

### 1. Tool Execution Tests (HIGHEST PRIORITY)

#### ToolExecutor Core Tests
```csharp
public class ToolExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_UnknownTool_ThrowsException()
    
    [Fact]
    public async Task ExecuteAsync_InvalidParameters_ReturnsValidationError()
    
    [Fact]
    public async Task ExecuteAsync_WithTimeout_CancelsExecution()
    
    [Fact]
    public async Task ExecuteAsync_SuccessfulExecution_ReturnsResult()
    
    [Theory]
    [InlineData("file_read", typeof(FileReadHandler))]
    [InlineData("file_write", typeof(FileWriteHandler))]
    public void RegisterHandler_MapsCorrectly(string toolName, Type handlerType)
}
```

#### Command Approval Service Tests
```csharp
public class CommandApprovalServiceTests
{
    [Fact]
    public async Task RequestApprovalAsync_InYoloMode_AlwaysApproves()
    {
        // Arrange
        var service = new CommandApprovalService();
        service.ConfigureSettings(new ApprovalSettings { EnableYoloMode = true });
        
        // Act
        var result = await service.RequestApprovalAsync("rm -rf /", context);
        
        // Assert
        Assert.True(result.Approved);
        Assert.Contains("YOLO", result.Reason);
    }
    
    [Fact]
    public async Task RequestApprovalAsync_DangerousCommand_RequiresApproval()
    
    [Fact]
    public async Task RequestApprovalAsync_SafeCommand_AutoApproves()
    
    [Fact]
    public async Task RequestApprovalAsync_WithCaching_ReturnsCachedResult()
}
```

### 2. File Operation Tool Tests

#### Pattern from SaturnFork's ApplyDiffToolTests
```csharp
public class ApplyDiffToolTests : IDisposable
{
    private readonly FileTestHelper _fileHelper;
    private readonly ApplyDiffTool _tool;
    
    [Fact]
    public async Task ApplyDiff_AddNewFile_CreatesFile()
    {
        // Arrange
        var diff = @"--- /dev/null
+++ b/newfile.txt
@@ -0,0 +1,3 @@
+Line 1
+Line 2
+Line 3";
        
        // Act
        var result = await _tool.ExecuteAsync(new { file_path = "newfile.txt", diff });
        
        // Assert
        Assert.True(result.Success);
        Assert.True(_fileHelper.FileExists("newfile.txt"));
        Assert.Equal("Line 1\nLine 2\nLine 3", _fileHelper.ReadFile("newfile.txt"));
    }
    
    [Fact]
    public async Task ApplyDiff_ModifyExistingFile_AppliesChanges()
    
    [Fact]
    public async Task ApplyDiff_DeleteFile_RemovesFile()
    
    [Fact]
    public async Task ApplyDiff_InvalidDiff_ReturnsError()
}
```

### 3. Streaming/SSE Tests

#### SSE Parser Tests
```csharp
public class SseParserTests
{
    [Fact]
    public async Task ParseStreamAsync_ValidSSE_ParsesCorrectly()
    {
        // Arrange
        var sseData = @"data: {""text"": ""Hello""}

data: {""text"": "" World""}

data: [DONE]
";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(sseData));
        var parser = new SseParser();
        
        // Act
        var events = await parser.ParseStreamAsync(stream).ToListAsync();
        
        // Assert
        Assert.Equal(2, events.Count);
        Assert.Equal("Hello", events[0].Data["text"]);
        Assert.Equal(" World", events[1].Data["text"]);
    }
    
    [Fact]
    public async Task ParseStreamAsync_WithComments_IgnoresComments()
    
    [Fact]
    public async Task ParseStreamAsync_Cancellation_StopsProcessing()
}
```

### 4. Agent Process Management Tests

#### ClaudeAgent Process Tests
```csharp
public class ClaudeProcessManagementTests
{
    private readonly MockProcessHelper _processHelper;
    
    [Fact]
    public async Task StartProcess_ValidExecutable_StartsSuccessfully()
    {
        // Arrange
        _processHelper.SetupProcess("claude", exitCode: 0);
        var agent = new ClaudeAgent(logger, config, _processHelper);
        
        // Act
        var result = await agent.InitializeAsync(config);
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal(AgentStatus.Ready, agent.Status);
    }
    
    [Fact]
    public async Task SendMessage_ProcessCrashed_RestartsAutomatically()
    
    [Fact]
    public async Task Shutdown_KillsProcess_GracefullyFirst()
}
```

## Mock Implementations

### 1. MockSaturnCore
```csharp
public class MockSaturnCore : ISaturnCore
{
    public Queue<ISaturnAgent> AgentsToReturn { get; } = new();
    public List<ToolInfo> ToolsToReturn { get; set; } = new();
    
    public Task<ISaturnAgent> CreateAgentAsync(...)
    {
        return Task.FromResult(AgentsToReturn.Dequeue());
    }
}
```

### 2. MockProcessHelper
```csharp
public class MockProcessHelper : IProcessHelper
{
    private readonly Dictionary<string, ProcessBehavior> _processBehaviors;
    
    public void SetupProcess(string executable, int exitCode = 0)
    public void SimulateOutput(string output)
    public void SimulateCrash()
}
```

## Testing Standards (from CLAUDE.md)

### Assertion Style
```csharp
// ✅ DO: Use standard xUnit assertions
Assert.NotNull(result);
Assert.True(result.Success);
Assert.Equal("expected", result.Value);
Assert.Throws<InvalidOperationException>(() => ...);

// ❌ DON'T: Use FluentAssertions (despite SaturnFork using it)
// result.Should().NotBeNull(); // Don't use
// result.Success.Should().BeTrue(); // Don't use
```

### Mocking Framework
- Use **NSubstitute** or **Moq** for mocking
- Avoid over-mocking - prefer real implementations where practical
- Use test doubles for external dependencies (HTTP, File System, Processes)

## Test Data Management

### 1. Test Constants
```csharp
public static class TestConstants
{
    public const string ValidClaudeModel = "claude-sonnet-4-20250514";
    public const string ValidOpenRouterModel = "anthropic/claude-3.5-sonnet";
    public const string TestApiKey = "test_api_key_12345";
    public const string TestOAuthCode = "test_oauth_code";
    
    public static readonly Dictionary<string, object> StandardToolParams = new()
    {
        { "timeout", 30 },
        { "max_retries", 3 }
    };
}
```

### 2. Test File Content
```csharp
public static class TestFileContent
{
    public const string SimpleTextFile = "Line 1\nLine 2\nLine 3";
    
    public const string UnifiedDiff = @"--- a/file.txt
+++ b/file.txt
@@ -1,3 +1,3 @@
 Line 1
-Line 2
+Modified Line 2
 Line 3";
    
    public const string ValidJsonResponse = @"{
        ""id"": ""msg_123"",
        ""type"": ""message"",
        ""content"": [{""type"": ""text"", ""text"": ""Hello""}]
    }";
}
```

## Integration Test Scenarios

### 1. End-to-End Tool Execution
```csharp
[IntegrationTest]
public async Task ToolExecution_FileOperations_CompleteWorkflow()
{
    // 1. Create a file
    // 2. Read the file
    // 3. Apply a diff
    // 4. Search with grep
    // 5. Delete the file
    // Verify each step and final state
}
```

### 2. OAuth Flow Integration
```csharp
[IntegrationTest]
public async Task OAuthFlow_CompleteAuthentication_StoresTokens()
{
    // 1. Generate PKCE challenge
    // 2. Build auth URL
    // 3. Exchange code for tokens
    // 4. Store tokens securely
    // 5. Refresh tokens
    // Verify token persistence and encryption
}
```

### 3. Multi-Agent Coordination
```csharp
[IntegrationTest]
public async Task MultiAgent_CreateAndHandoff_WorksEndToEnd()
{
    // 1. Create primary agent
    // 2. Create sub-agent
    // 3. Hand off task
    // 4. Wait for completion
    // 5. Get results
    // Verify coordination and results
}
```

## Test Execution Strategy

### Phase 1: Unit Tests (Days 1-2)
1. Port test helpers from SaturnFork
2. Implement tool executor tests
3. Implement command approval tests
4. Basic agent tests

### Phase 2: Component Tests (Days 3-4)
1. Individual tool tests
2. Provider client tests
3. SSE streaming tests
4. Process management tests

### Phase 3: Integration Tests (Day 5)
1. End-to-end scenarios
2. OAuth flow testing
3. Multi-agent coordination
4. Performance benchmarks

## Coverage Goals

### Minimum Acceptable Coverage
- **Critical Components**: 90% coverage
  - Tool Executor
  - Command Approval
  - OAuth Flow
- **Core Functionality**: 80% coverage
  - Providers
  - Agents
  - Tools
- **Overall**: 75% coverage

### Ideal Coverage
- **Critical Components**: 95%+
- **Core Functionality**: 90%+
- **Overall**: 85%+

## Performance Benchmarks

### Tool Execution Benchmarks
```csharp
[Benchmark]
public async Task ToolExecutor_SingleExecution()
{
    // Target: < 10ms for simple tools
}

[Benchmark]
public async Task ToolExecutor_ParallelExecution()
{
    // Target: < 50ms for 10 parallel executions
}
```

### Streaming Performance
```csharp
[Benchmark]
public async Task SseParser_LargeStream()
{
    // Target: Process 1MB of SSE data in < 100ms
}
```

## Continuous Integration

### Test Pipeline
```yaml
name: Track 2 Tests
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - uses: actions/setup-dotnet@v1
      - run: dotnet restore
      - run: dotnet build
      - run: dotnet test tests/OrchestratorChat.Agents.Tests
      - run: dotnet test tests/OrchestratorChat.Saturn.Tests
      - run: dotnet test --collect:"XPlat Code Coverage"
```

## Risk Mitigation

### High-Risk Areas Requiring Extra Testing
1. **Process Management**: ClaudeAgent process lifecycle
2. **Security**: Command approval bypass scenarios
3. **Concurrency**: Multi-agent coordination deadlocks
4. **Resource Management**: File handle and memory leaks
5. **Error Recovery**: Network failures and retries

### Mitigation Strategies
- Use deterministic mocks for external dependencies
- Implement timeout guards in all async tests
- Use `IDisposable` pattern consistently
- Test error paths explicitly
- Use memory and handle leak detection

## Success Criteria

### Definition of "Tests Complete"
- [ ] All test projects created and building
- [ ] Test helpers ported from SaturnFork
- [ ] 100% of critical component tests written
- [ ] 80% of core functionality tests written
- [ ] All tests passing consistently
- [ ] Coverage goals met
- [ ] Integration tests running
- [ ] CI pipeline configured

## Appendix: SaturnFork Test Assets

### Files to Port Directly
1. `Saturn.Tests/TestHelpers/FileTestHelper.cs`
2. `Saturn.Tests/TestHelpers/TestConstants.cs`
3. `Saturn.Tests/Mocks/MockHttpMessageHandler.cs`

### Test Patterns to Adapt
1. Tool testing pattern from `ApplyDiffToolTests.cs`
2. Provider testing from `AnthropicClientTests.cs`
3. OAuth flow from `AnthropicAuthServiceTests.cs`
4. Streaming tests from provider tests

### Key Differences from SaturnFork
1. No FluentAssertions - use standard xUnit
2. No Terminal.Gui tests needed
3. Focus on web/SignalR integration points
4. Add YOLO mode testing for approval service

---

## Implementation Checklist

### Immediate Actions
- [ ] Create test project files (.csproj)
- [ ] Port FileTestHelper from SaturnFork
- [ ] Write first ToolExecutor test
- [ ] Write first CommandApproval test

### Week 1 Goals
- [ ] Complete all Priority 1 test implementations
- [ ] Achieve 80% coverage on critical components
- [ ] Set up CI pipeline
- [ ] Document any testing gaps

This plan ensures comprehensive testing of Track 2 components while maximizing reuse from the proven SaturnFork test suite.