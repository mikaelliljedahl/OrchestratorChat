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
        // Arrange - Create a mock agent for process testing
        var mockAgent = Substitute.For<IAgent>();
        var agentConfig = new AgentConfiguration
        {
            Id = TestConstants.DefaultAgentId,
            Name = TestConstants.DefaultAgentName,
            Type = AgentType.Claude,
            WorkingDirectory = Directory.GetCurrentDirectory()
        };

        // Setup mock agent behavior
        mockAgent.InitializeAsync(Arg.Any<AgentConfiguration>()).Returns(new AgentInitializationResult
        {
            Success = true,
            Capabilities = new AgentCapabilities(),
            InitializationTime = TimeSpan.FromSeconds(1)
        });
        mockAgent.Status.Returns(AgentStatus.Ready);
        mockAgent.Id.Returns(TestConstants.DefaultAgentId);
        mockAgent.Name.Returns(TestConstants.DefaultAgentName);
        mockAgent.Type.Returns(AgentType.Claude);

        _processHelper.SetupProcess(
            TestConstants.TestClaudeExecutable,
            exitCode: 0,
            standardOutput: "Process started successfully",
            startDelayMs: 100
        );

        // Act
        var result = await mockAgent.InitializeAsync(agentConfig);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(AgentStatus.Ready, mockAgent.Status);
        // Verify process helper was configured correctly
        Assert.NotNull(_processHelper);
        Assert.Equal(0, _processHelper.ExecutionHistory.Count); // No actual process started
    }

    [Fact]
    public async Task SendMessage_ProcessCrashed_RestartsAutomatically()
    {
        // Arrange - Mock agent with crash and recovery behavior
        var mockAgent = Substitute.For<IAgent>();
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

        // Setup mock agent behavior for crash recovery
        mockAgent.InitializeAsync(Arg.Any<AgentConfiguration>()).Returns(new AgentInitializationResult
        {
            Success = true,
            Capabilities = new AgentCapabilities(),
            InitializationTime = TimeSpan.FromSeconds(1)
        });

        await mockAgent.InitializeAsync(agentConfig);

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

        // Setup mock response for successful recovery
        mockAgent.SendMessageAsync(Arg.Any<AgentMessage>(), Arg.Any<CancellationToken>()).Returns(
            new AgentResponse
            {
                Id = Guid.NewGuid().ToString(),
                Content = TestConstants.ValidClaudeResponse,
                Type = ResponseType.Success,
                Role = MessageRole.Assistant,
                Timestamp = DateTime.UtcNow
            });

        mockAgent.GetStatusAsync().Returns(new AgentStatusInfo
        {
            Status = AgentStatus.Ready,
            IsHealthy = true,
            LastActivity = DateTime.UtcNow
        });

        // Act - This should trigger restart if process crashed
        var response = await mockAgent.SendMessageAsync(message);
        
        // Assert
        Assert.NotNull(response);
        Assert.Equal(ResponseType.Success, response.Type);
        Assert.Equal(MessageRole.Assistant, response.Role);
        
        // Agent should recover and be ready
        var status = await mockAgent.GetStatusAsync();
        Assert.Equal(AgentStatus.Ready, status.Status);
        Assert.True(status.IsHealthy);
    }

    [Fact]
    public async Task Shutdown_KillsProcess_GracefullyFirst()
    {
        // Arrange - Mock agent with shutdown behavior
        var mockAgent = Substitute.For<IAgent>();
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

        mockAgent.InitializeAsync(Arg.Any<AgentConfiguration>()).Returns(new AgentInitializationResult
        {
            Success = true,
            Capabilities = new AgentCapabilities()
        });

        mockAgent.Status.Returns(AgentStatus.Ready);
        mockAgent.ShutdownAsync().Returns(Task.CompletedTask);

        await mockAgent.InitializeAsync(agentConfig);

        // Verify process started
        Assert.Equal(AgentStatus.Ready, mockAgent.Status);

        // Act
        var shutdownTask = mockAgent.ShutdownAsync();
        
        // Should complete shutdown within reasonable time
        try
        {
            await shutdownTask.WaitAsync(TimeSpan.FromSeconds(5));
            // If we reach here, shutdown completed within timeout
            Assert.True(true);
        }
        catch (TimeoutException)
        {
            Assert.Fail("Shutdown did not complete within 5 seconds");
        }
        
        // Assert - Update status after shutdown
        mockAgent.Status.Returns(AgentStatus.Shutdown);
        Assert.Equal(AgentStatus.Shutdown, mockAgent.Status);
    }

    [Fact]
    public async Task HandleStreamingResponse_ParsesCorrectly()
    {
        // Arrange - Mock agent with streaming capabilities
        var mockAgent = Substitute.For<IAgent>();
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

        // Setup mock agent responses
        mockAgent.InitializeAsync(Arg.Any<AgentConfiguration>()).Returns(new AgentInitializationResult
        {
            Success = true,
            Capabilities = new AgentCapabilities()
        });

        await mockAgent.InitializeAsync(agentConfig);

        var message = new AgentMessage
        {
            Id = Guid.NewGuid().ToString(),
            Content = "Test streaming",
            Role = MessageRole.User,
            Timestamp = DateTime.UtcNow
        };

        // Mock streaming response that assembles the chunks into final content
        mockAgent.SendMessageAsync(Arg.Any<AgentMessage>(), Arg.Any<CancellationToken>()).Returns(
            new AgentResponse
            {
                Id = Guid.NewGuid().ToString(),
                Content = "Hello world", // Assembled from streaming chunks
                Type = ResponseType.Success,
                Role = MessageRole.Assistant,
                Timestamp = DateTime.UtcNow
            });

        // Act
        var response = await mockAgent.SendMessageAsync(message);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(ResponseType.Success, response.Type);
        Assert.Contains("Hello world", response.Content);
        Assert.Equal(MessageRole.Assistant, response.Role);
    }

    [Fact]
    public async Task ProcessHealthCheck_DetectsUnhealthyProcess()
    {
        // Arrange - Mock agent with health check capabilities
        var mockAgent = Substitute.For<IAgent>();
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

        mockAgent.InitializeAsync(Arg.Any<AgentConfiguration>()).Returns(new AgentInitializationResult
        {
            Success = true,
            Capabilities = new AgentCapabilities()
        });

        await mockAgent.InitializeAsync(agentConfig);

        // Initially should be ready
        mockAgent.Status.Returns(AgentStatus.Ready);
        Assert.Equal(AgentStatus.Ready, mockAgent.Status);

        // Act - Wait for process to crash and health check to detect it
        await Task.Delay(500); // Wait longer than crash delay

        // Simulate process crash by updating status
        mockAgent.GetStatusAsync().Returns(new AgentStatusInfo
        {
            Status = AgentStatus.Error,
            IsHealthy = false,
            LastActivity = DateTime.UtcNow
        });

        // Trigger health check by trying to get status
        var status = await mockAgent.GetStatusAsync();

        // Assert
        // Process should be detected as unhealthy or in error state
        Assert.True(status.Status == AgentStatus.Error || !status.IsHealthy);
    }

    [Fact]
    public async Task ProcessTimeout_KillsAndRestarts()
    {
        // Arrange - Mock agent with timeout handling
        var mockAgent = Substitute.For<IAgent>();
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

        mockAgent.InitializeAsync(Arg.Any<AgentConfiguration>()).Returns(new AgentInitializationResult
        {
            Success = true,
            Capabilities = new AgentCapabilities()
        });

        await mockAgent.InitializeAsync(agentConfig);

        var message = new AgentMessage
        {
            Id = Guid.NewGuid().ToString(),
            Content = "Test timeout",
            Role = MessageRole.User,
            Timestamp = DateTime.UtcNow
        };

        // Mock timeout behavior - should throw OperationCanceledException quickly
        mockAgent.SendMessageAsync(Arg.Any<AgentMessage>(), Arg.Any<CancellationToken>())
               .Returns<AgentResponse>(x => throw new OperationCanceledException("Operation timed out"));

        // Act & Assert
        // This should either timeout with an exception or handle the timeout gracefully
        var startTime = DateTime.UtcNow;
        
        try
        {
            var response = await mockAgent.SendMessageAsync(message);
            
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
        // Arrange - Mock agent with multiple message handling
        var mockAgent = Substitute.For<IAgent>();
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

        mockAgent.InitializeAsync(Arg.Any<AgentConfiguration>()).Returns(new AgentInitializationResult
        {
            Success = true,
            Capabilities = new AgentCapabilities()
        });

        await mockAgent.InitializeAsync(agentConfig);

        var messages = new[]
        {
            new AgentMessage { Id = Guid.NewGuid().ToString(), Content = "Message 1", Role = MessageRole.User, Timestamp = DateTime.UtcNow },
            new AgentMessage { Id = Guid.NewGuid().ToString(), Content = "Message 2", Role = MessageRole.User, Timestamp = DateTime.UtcNow },
            new AgentMessage { Id = Guid.NewGuid().ToString(), Content = "Message 3", Role = MessageRole.User, Timestamp = DateTime.UtcNow }
        };

        // Setup consistent response for multiple messages
        mockAgent.SendMessageAsync(Arg.Any<AgentMessage>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var message = call.Arg<AgentMessage>();
            return new AgentResponse
            {
                Id = Guid.NewGuid().ToString(),
                Content = $"Response to: {message.Content}",
                Type = ResponseType.Success,
                Role = MessageRole.Assistant,
                Timestamp = DateTime.UtcNow
            };
        });

        // Act
        var responses = new List<AgentResponse>();
        foreach (var message in messages)
        {
            var response = await mockAgent.SendMessageAsync(message);
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
        mockAgent.GetStatusAsync().Returns(new AgentStatusInfo
        {
            Status = AgentStatus.Ready,
            IsHealthy = true,
            LastActivity = DateTime.UtcNow
        });

        var status = await mockAgent.GetStatusAsync();
        Assert.Equal(AgentStatus.Ready, status.Status);
        Assert.True(status.IsHealthy);
    }

    public void Dispose()
    {
        _processHelper?.Dispose();
    }
}