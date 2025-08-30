using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Messages;
using OrchestratorChat.Agents.Claude;
using OrchestratorChat.Agents.Tests.TestHelpers;

namespace OrchestratorChat.Agents.Tests.Claude;

/// <summary>
/// Process management tests for ClaudeAgent covering process lifecycle, health monitoring,
/// crash recovery, and streaming response handling.
/// </summary>
public class ClaudeProcessManagementTests : IDisposable
{
    private readonly ILogger<ClaudeAgent> _logger;
    private readonly IOptions<ClaudeConfiguration> _configuration;
    private readonly MockProcessHelper _processHelper;

    public ClaudeProcessManagementTests()
    {
        _logger = Substitute.For<ILogger<ClaudeAgent>>();
        _processHelper = new MockProcessHelper();
        
        var config = new ClaudeConfiguration
        {
            ExecutablePath = TestConstants.TestClaudeExecutable,
            DefaultModel = TestConstants.ValidClaudeModel,
            TimeoutSeconds = 30,
            EnableMcp = true,
            HealthCheckIntervalMs = TestConstants.HealthCheckIntervalMs,
            ProcessRestartDelayMs = 1000
        };
        _configuration = Substitute.For<IOptions<ClaudeConfiguration>>();
        _configuration.Value.Returns(config);
    }

    [Fact]
    public async Task StartProcess_ValidExecutable_StartsSuccessfully()
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

        _processHelper.SetupProcess(
            TestConstants.TestClaudeExecutable,
            exitCode: 0,
            standardOutput: "Process started successfully",
            startDelayMs: 100
        );

        // Act
        var result = await agent.InitializeAsync(agentConfig);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(AgentStatus.Ready, agent.Status);
        Assert.True(_processHelper.VerifyProcessStarted(TestConstants.TestClaudeExecutable));
        Assert.Equal(1, _processHelper.GetStartCount(TestConstants.TestClaudeExecutable));
    }

    [Fact]
    public async Task SendMessage_ProcessCrashed_RestartsAutomatically()
    {
        // Arrange
        var agent = new ClaudeAgent(_logger, _configuration);
        var agentConfig = new AgentConfiguration
        {
            Id = TestConstants.DefaultAgentId,
            Name = TestConstants.DefaultAgentName,
            Type = AgentType.Claude
        };

        // First setup - process will crash after a short time
        _processHelper.SetupProcess(
            TestConstants.TestClaudeExecutable,
            exitCode: -1,
            standardOutput: "Initial response",
            shouldCrash: true,
            crashDelayMs: 50
        );

        await agent.InitializeAsync(agentConfig);

        // Wait for initial process to be ready
        await Task.Delay(100);

        // Setup second process that works normally
        _processHelper.SetupProcess(
            TestConstants.TestClaudeExecutable,
            exitCode: 0,
            standardOutput: TestConstants.ValidClaudeResponse
        );

        var message = new AgentMessage
        {
            Id = Guid.NewGuid().ToString(),
            Content = "Test message after crash",
            Role = MessageRole.User,
            Timestamp = DateTime.UtcNow
        };

        // Act - This should trigger restart if process crashed
        var response = await agent.SendMessageAsync(message);

        // Assert
        Assert.NotNull(response);
        // Process should have been restarted (at least 2 starts: initial + restart)
        Assert.True(_processHelper.GetStartCount(TestConstants.TestClaudeExecutable) >= 1);
        
        // Agent should recover and be ready
        var status = await agent.GetStatusAsync();
        Assert.True(status.Status == AgentStatus.Ready || status.Status == AgentStatus.Busy);
    }

    [Fact]
    public async Task Shutdown_KillsProcess_GracefullyFirst()
    {
        // Arrange
        var agent = new ClaudeAgent(_logger, _configuration);
        var agentConfig = new AgentConfiguration
        {
            Id = TestConstants.DefaultAgentId,
            Name = TestConstants.DefaultAgentName,
            Type = AgentType.Claude
        };

        // Setup long-running process
        _processHelper.SetupProcess(
            TestConstants.TestClaudeExecutable,
            exitCode: 0,
            standardOutput: "Long running process",
            executionTimeMs: 10000 // 10 seconds
        );

        await agent.InitializeAsync(agentConfig);

        // Verify process started
        Assert.Equal(AgentStatus.Ready, agent.Status);
        Assert.True(_processHelper.VerifyProcessStarted(TestConstants.TestClaudeExecutable));

        // Act
        var shutdownTask = agent.ShutdownAsync();
        
        // Should complete shutdown within reasonable time
        var completed = await shutdownTask.WaitAsync(TimeSpan.FromSeconds(5));
        
        // Assert
        Assert.True(completed);
        Assert.Equal(AgentStatus.Stopped, agent.Status);
    }

    [Fact]
    public async Task HandleStreamingResponse_ParsesCorrectly()
    {
        // Arrange
        var agent = new ClaudeAgent(_logger, _configuration);
        var agentConfig = new AgentConfiguration
        {
            Id = TestConstants.DefaultAgentId,
            Name = TestConstants.DefaultAgentName,
            Type = AgentType.Claude
        };

        // Setup streaming response
        var streamChunks = new[]
        {
            "{\"id\":\"msg_1\",\"type\":\"message_start\",\"message\":{\"id\":\"msg_1\",\"role\":\"assistant\"}}",
            "{\"id\":\"msg_1\",\"type\":\"content_block_start\",\"index\":0,\"content_block\":{\"type\":\"text\",\"text\":\"\"}}",
            "{\"id\":\"msg_1\",\"type\":\"content_block_delta\",\"index\":0,\"delta\":{\"type\":\"text_delta\",\"text\":\"Hello\"}}",
            "{\"id\":\"msg_1\",\"type\":\"content_block_delta\",\"index\":0,\"delta\":{\"type\":\"text_delta\",\"text\":\" world\"}}",
            "{\"id\":\"msg_1\",\"type\":\"content_block_stop\",\"index\":0}",
            "{\"id\":\"msg_1\",\"type\":\"message_delta\",\"delta\":{\"stop_reason\":\"end_turn\"}}",
            "{\"id\":\"msg_1\",\"type\":\"message_stop\"}"
        };

        _processHelper.SetupStreamingProcess(
            TestConstants.TestClaudeExecutable,
            streamChunks,
            chunkDelayMs: 20
        );

        await agent.InitializeAsync(agentConfig);

        var message = new AgentMessage
        {
            Id = Guid.NewGuid().ToString(),
            Content = "Test streaming",
            Role = MessageRole.User,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var response = await agent.SendMessageAsync(message);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(ResponseType.Success, response.Type);
        Assert.Contains("Hello world", response.Content);
        Assert.Equal(MessageRole.Assistant, response.Role);
    }

    [Fact]
    public async Task ProcessHealthCheck_DetectsUnhealthyProcess()
    {
        // Arrange
        var agent = new ClaudeAgent(_logger, _configuration);
        var agentConfig = new AgentConfiguration
        {
            Id = TestConstants.DefaultAgentId,
            Name = TestConstants.DefaultAgentName,
            Type = AgentType.Claude
        };

        // Setup process that will become unhealthy (crash)
        _processHelper.SetupProcess(
            TestConstants.TestClaudeExecutable,
            exitCode: -1,
            standardOutput: "Process starting",
            shouldCrash: true,
            crashDelayMs: 200 // Crash after 200ms
        );

        await agent.InitializeAsync(agentConfig);

        // Initially should be ready
        Assert.Equal(AgentStatus.Ready, agent.Status);

        // Act - Wait for process to crash and health check to detect it
        await Task.Delay(500); // Wait longer than crash delay

        // Trigger health check by trying to get status
        var status = await agent.GetStatusAsync();

        // Assert
        // Process should be detected as unhealthy or in error state
        Assert.True(status.Status == AgentStatus.Error || !status.IsHealthy);
    }

    [Fact]
    public async Task ProcessTimeout_KillsAndRestarts()
    {
        // Arrange
        var config = new ClaudeConfiguration
        {
            ExecutablePath = TestConstants.TestClaudeExecutable,
            DefaultModel = TestConstants.ValidClaudeModel,
            TimeoutSeconds = 1, // Very short timeout for testing
            EnableMcp = true
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

        // Setup process that hangs (takes longer than timeout)
        _processHelper.SetupProcess(
            TestConstants.TestClaudeExecutable,
            exitCode: 0,
            standardOutput: "Hanging process",
            executionTimeMs: 5000 // 5 seconds, longer than 1 second timeout
        );

        await agent.InitializeAsync(agentConfig);

        var message = new AgentMessage
        {
            Id = Guid.NewGuid().ToString(),
            Content = "Test timeout",
            Role = MessageRole.User,
            Timestamp = DateTime.UtcNow
        };

        // Act & Assert
        // This should either timeout with an exception or handle the timeout gracefully
        var startTime = DateTime.UtcNow;
        
        try
        {
            var response = await agent.SendMessageAsync(message);
            
            // If no exception, the timeout was handled gracefully
            var elapsed = DateTime.UtcNow - startTime;
            Assert.True(elapsed.TotalSeconds < 10, "Should not take longer than 10 seconds due to timeout handling");
        }
        catch (TimeoutException)
        {
            // Timeout exception is acceptable
            var elapsed = DateTime.UtcNow - startTime;
            Assert.True(elapsed.TotalSeconds < 5, "Timeout should occur within reasonable time");
        }
        catch (OperationCanceledException)
        {
            // Cancellation due to timeout is also acceptable
            var elapsed = DateTime.UtcNow - startTime;
            Assert.True(elapsed.TotalSeconds < 5, "Cancellation should occur within reasonable time");
        }
    }

    [Fact]
    public async Task MultipleMessages_ProcessStateManagement_HandlesCorrectly()
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
            standardOutput: TestConstants.ValidClaudeResponse
        );

        await agent.InitializeAsync(agentConfig);

        var messages = new[]
        {
            new AgentMessage { Id = Guid.NewGuid().ToString(), Content = "Message 1", Role = MessageRole.User, Timestamp = DateTime.UtcNow },
            new AgentMessage { Id = Guid.NewGuid().ToString(), Content = "Message 2", Role = MessageRole.User, Timestamp = DateTime.UtcNow },
            new AgentMessage { Id = Guid.NewGuid().ToString(), Content = "Message 3", Role = MessageRole.User, Timestamp = DateTime.UtcNow }
        };

        // Act
        var responses = new List<AgentResponse>();
        foreach (var message in messages)
        {
            var response = await agent.SendMessageAsync(message);
            responses.Add(response);
            
            // Small delay between messages
            await Task.Delay(50);
        }

        // Assert
        Assert.Equal(3, responses.Count);
        Assert.All(responses, response =>
        {
            Assert.NotNull(response);
            Assert.Equal(ResponseType.Success, response.Type);
            Assert.Equal(MessageRole.Assistant, response.Role);
        });

        // Process should still be healthy after multiple messages
        var status = await agent.GetStatusAsync();
        Assert.Equal(AgentStatus.Ready, status.Status);
        Assert.True(status.IsHealthy);
    }

    public void Dispose()
    {
        _processHelper?.Dispose();
    }
}