using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Messages;
using OrchestratorChat.Agents.Claude;
using OrchestratorChat.Agents.Tests.TestHelpers;

namespace OrchestratorChat.Agents.Tests.Claude;

/// <summary>
/// Main ClaudeAgent lifecycle tests covering initialization, messaging, and status management.
/// Tests the ClaudeAgent's process lifecycle and communication patterns.
/// </summary>
public class ClaudeAgentTests : IDisposable
{
    private readonly ILogger<ClaudeAgent> _logger;
    private readonly IOptions<ClaudeConfiguration> _configuration;
    private readonly MockProcessHelper _processHelper;

    public ClaudeAgentTests()
    {
        _logger = Substitute.For<ILogger<ClaudeAgent>>();
        _processHelper = new MockProcessHelper();
        
        var config = new ClaudeConfiguration
        {
            ExecutablePath = TestConstants.TestClaudeExecutable,
            DefaultModel = TestConstants.ValidClaudeModel,
            TimeoutSeconds = 30,
            EnableMcp = true
        };
        _configuration = Substitute.For<IOptions<ClaudeConfiguration>>();
        _configuration.Value.Returns(config);
    }

    [Fact]
    public async Task InitializeAsync_ValidConfiguration_StartsSuccessfully()
    {
        // Arrange
        var agent = new ClaudeAgent(_logger, _configuration);
        var agentConfig = new AgentConfiguration
        {
            Id = TestConstants.DefaultAgentId,
            Name = TestConstants.DefaultAgentName,
            Type = AgentType.Claude,
            WorkingDirectory = Directory.GetCurrentDirectory()
        };

        // Set up successful process start
        _processHelper.SetupProcess(
            TestConstants.TestClaudeExecutable,
            exitCode: 0,
            standardOutput: "Claude initialized successfully"
        );

        // Act
        var result = await agent.InitializeAsync(agentConfig);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(AgentStatus.Ready, agent.Status);
        Assert.Equal(TestConstants.DefaultAgentName, agent.Name);
        Assert.Equal(AgentType.Claude, agent.Type);
        Assert.NotNull(result.Capabilities);
    }

    [Fact]
    public async Task InitializeAsync_InvalidExecutable_ReturnsError()
    {
        // Arrange
        var config = new ClaudeConfiguration
        {
            ExecutablePath = "invalid-executable-path",
            DefaultModel = TestConstants.ValidClaudeModel,
            TimeoutSeconds = 30
        };
        var options = Substitute.For<IOptions<ClaudeConfiguration>>();
        options.Value.Returns(config);

        var agent = new ClaudeAgent(_logger, options);
        var agentConfig = new AgentConfiguration
        {
            Id = TestConstants.DefaultAgentId,
            Name = TestConstants.DefaultAgentName,
            Type = AgentType.Claude
        };

        // Set up process start failure
        _processHelper.SimulateStartFailure("invalid-executable-path", "Executable not found");

        // Act
        var result = await agent.InitializeAsync(agentConfig);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Executable not found", result.ErrorMessage);
        Assert.Equal(AgentStatus.Error, agent.Status);
    }

    [Fact]
    public async Task SendMessageAsync_ProcessNotStarted_StartsAutomatically()
    {
        // Arrange
        var agent = new ClaudeAgent(_logger, _configuration);
        var agentConfig = new AgentConfiguration
        {
            Id = TestConstants.DefaultAgentId,
            Name = TestConstants.DefaultAgentName,
            Type = AgentType.Claude
        };

        // Set up successful process start and response
        _processHelper.SetupProcess(
            TestConstants.TestClaudeExecutable,
            exitCode: 0,
            standardOutput: TestConstants.ValidClaudeResponse
        );

        await agent.InitializeAsync(agentConfig);

        var message = new AgentMessage
        {
            Id = Guid.NewGuid().ToString(),
            Content = "Hello Claude",
            Role = MessageRole.User,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var response = await agent.SendMessageAsync(message);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(ResponseType.Success, response.Type);
        Assert.NotNull(response.Content);
        Assert.True(_processHelper.VerifyProcessStarted(TestConstants.TestClaudeExecutable));
    }

    [Fact]
    public async Task SendMessageAsync_ValidMessage_ReturnsResponse()
    {
        // Arrange
        var agent = new ClaudeAgent(_logger, _configuration);
        var agentConfig = new AgentConfiguration
        {
            Id = TestConstants.DefaultAgentId,
            Name = TestConstants.DefaultAgentName,
            Type = AgentType.Claude
        };

        // Set up process with streaming response
        var responseChunks = new[]
        {
            "Hello from",
            " Claude!",
            " How can I",
            " help you today?"
        };

        _processHelper.SetupStreamingProcess(
            TestConstants.TestClaudeExecutable,
            responseChunks,
            chunkDelayMs: 10
        );

        await agent.InitializeAsync(agentConfig);

        var message = new AgentMessage
        {
            Id = Guid.NewGuid().ToString(),
            Content = "Hello Claude",
            Role = MessageRole.User,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var response = await agent.SendMessageAsync(message);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(ResponseType.Success, response.Type);
        Assert.Contains("Hello from Claude!", response.Content);
        Assert.Equal(MessageRole.Assistant, response.Role);
        Assert.NotNull(response.TokenUsage);
    }

    [Fact]
    public async Task ShutdownAsync_RunningProcess_TerminatesGracefully()
    {
        // Arrange
        var agent = new ClaudeAgent(_logger, _configuration);
        var agentConfig = new AgentConfiguration
        {
            Id = TestConstants.DefaultAgentId,
            Name = TestConstants.DefaultAgentName,
            Type = AgentType.Claude
        };

        _processHelper.SetupProcess(
            TestConstants.TestClaudeExecutable,
            exitCode: 0,
            executionTimeMs: 5000 // Long-running process
        );

        await agent.InitializeAsync(agentConfig);

        // Verify process is running
        Assert.Equal(AgentStatus.Ready, agent.Status);

        // Act
        await agent.ShutdownAsync();

        // Assert
        Assert.Equal(AgentStatus.Stopped, agent.Status);
        
        // Verify process was started (and should be terminated)
        Assert.True(_processHelper.VerifyProcessStarted(TestConstants.TestClaudeExecutable));
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsCurrentStatus()
    {
        // Arrange
        var agent = new ClaudeAgent(_logger, _configuration);
        
        // Test uninitialized status
        var initialStatus = await agent.GetStatusAsync();
        Assert.Equal(AgentStatus.Uninitialized, initialStatus.Status);

        // Initialize and test ready status
        var agentConfig = new AgentConfiguration
        {
            Id = TestConstants.DefaultAgentId,
            Name = TestConstants.DefaultAgentName,
            Type = AgentType.Claude
        };

        _processHelper.SetupProcess(
            TestConstants.TestClaudeExecutable,
            exitCode: 0,
            standardOutput: "Claude ready"
        );

        await agent.InitializeAsync(agentConfig);

        // Act
        var readyStatus = await agent.GetStatusAsync();

        // Assert
        Assert.Equal(AgentStatus.Ready, readyStatus.Status);
        Assert.NotNull(readyStatus.LastActivity);
        Assert.True(readyStatus.IsHealthy);
        Assert.NotNull(readyStatus.Capabilities);
        Assert.Equal(TestConstants.DefaultAgentId, readyStatus.AgentId);
    }

    [Fact]
    public async Task StatusChanged_Event_RaisedOnStatusChange()
    {
        // Arrange
        var agent = new ClaudeAgent(_logger, _configuration);
        var statusChanges = new List<AgentStatus>();
        
        agent.StatusChanged += (sender, args) => statusChanges.Add(args.NewStatus);

        var agentConfig = new AgentConfiguration
        {
            Id = TestConstants.DefaultAgentId,
            Name = TestConstants.DefaultAgentName,
            Type = AgentType.Claude
        };

        _processHelper.SetupProcess(
            TestConstants.TestClaudeExecutable,
            exitCode: 0,
            standardOutput: "Claude initialized"
        );

        // Act
        await agent.InitializeAsync(agentConfig);

        // Assert
        Assert.Contains(AgentStatus.Initializing, statusChanges);
        Assert.Contains(AgentStatus.Ready, statusChanges);
        Assert.True(statusChanges.Count >= 2);
    }

    [Fact]
    public async Task OutputReceived_Event_RaisedOnProcessOutput()
    {
        // Arrange
        var agent = new ClaudeAgent(_logger, _configuration);
        var outputReceived = new List<string>();
        
        agent.OutputReceived += (sender, args) => outputReceived.Add(args.Output);

        var agentConfig = new AgentConfiguration
        {
            Id = TestConstants.DefaultAgentId,
            Name = TestConstants.DefaultAgentName,
            Type = AgentType.Claude
        };

        var responseChunks = new[] { "Output ", "chunk ", "1", "Output ", "chunk ", "2" };
        _processHelper.SetupStreamingProcess(
            TestConstants.TestClaudeExecutable,
            responseChunks,
            chunkDelayMs: 5
        );

        await agent.InitializeAsync(agentConfig);

        var message = new AgentMessage
        {
            Id = Guid.NewGuid().ToString(),
            Content = "Test message",
            Role = MessageRole.User,
            Timestamp = DateTime.UtcNow
        };

        // Act
        await agent.SendMessageAsync(message);

        // Wait a bit for streaming to complete
        await Task.Delay(100);

        // Assert
        Assert.True(outputReceived.Count > 0);
        Assert.Contains(outputReceived, output => output.Contains("Output"));
    }

    public void Dispose()
    {
        _processHelper?.Dispose();
    }
}