using NSubstitute;
using OrchestratorChat.Saturn.Core;
using OrchestratorChat.Saturn.Models;
using OrchestratorChat.Saturn.Tests.TestHelpers;
using OrchestratorChat.Saturn.Tools.MultiAgent;
using System.Text.Json;

namespace OrchestratorChat.Saturn.Tests.MultiAgent;

/// <summary>
/// Tests for HandOffToAgentTool implementation
/// </summary>
public class HandOffToAgentToolTests : IDisposable
{
    private readonly IAgentManager _mockAgentManager;
    private readonly ISaturnAgent _mockAgent;
    private readonly HandOffToAgentTool _tool;
    private readonly string _testAgentId;

    public HandOffToAgentToolTests()
    {
        _mockAgentManager = Substitute.For<IAgentManager>();
        _mockAgent = Substitute.For<ISaturnAgent>();
        _testAgentId = "test-agent-123";
        
        _tool = new HandOffToAgentTool(_mockAgentManager);
        
        // Setup basic mock behavior
        _mockAgent.Id.Returns(_testAgentId);
        _mockAgent.Name.Returns("TestAgent");
        _mockAgent.Status.Returns(AgentStatus.Idle);
        _mockAgentManager.GetAgentAsync(_testAgentId).Returns(_mockAgent);
    }

    [Fact]
    public async Task ExecuteAsync_ValidHandoff_TransfersControl()
    {
        // Arrange
        var task = "Complete this analysis task";
        var responses = new List<AgentResponse>
        {
            new AgentResponse { Content = "Starting analysis...", IsComplete = false },
            new AgentResponse { Content = "Analysis complete!", IsComplete = true }
        };

        _mockAgent.ProcessMessageAsync(Arg.Any<AgentMessage>(), Arg.Any<CancellationToken>())
                  .Returns(CreateAsyncEnumerable(responses));

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["agent_id"] = _testAgentId,
                ["task"] = task,
                ["wait_for_completion"] = true,
                ["timeout_seconds"] = 30
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.True(result.Success);
        
        var outputJson = JsonDocument.Parse(result.Output);
        Assert.Equal(_testAgentId, outputJson.RootElement.GetProperty("agent_id").GetString());
        Assert.Equal("TestAgent", outputJson.RootElement.GetProperty("agent_name").GetString());
        Assert.Equal(task, outputJson.RootElement.GetProperty("task").GetString());
        Assert.True(outputJson.RootElement.GetProperty("handoff_completed").GetBoolean());
        Assert.True(outputJson.RootElement.GetProperty("waited_for_completion").GetBoolean());
        
        Assert.Equal(_testAgentId, result.Metadata["target_agent_id"]);
        Assert.Equal(task, result.Metadata["task"]);
        Assert.Equal(true, result.Metadata["waited_for_completion"]);
        Assert.Equal(true, result.Metadata["completion_status"]);

        // Verify the agent received the message
        await _mockAgent.Received(1).ProcessMessageAsync(
            Arg.Is<AgentMessage>(msg => 
                msg.Content == task && 
                msg.Role == MessageRole.User &&
                msg.Metadata.ContainsKey("handoff_source")
            ), 
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task ExecuteAsync_NonExistentAgent_ReturnsError()
    {
        // Arrange
        var nonExistentAgentId = "non-existent-agent";
        _mockAgentManager.GetAgentAsync(nonExistentAgentId).Returns(Task.FromResult<ISaturnAgent?>(null));

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["agent_id"] = nonExistentAgentId,
                ["task"] = "This should fail"
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.False(result.Success);
        Assert.Contains($"Agent with ID '{nonExistentAgentId}' not found", result.Error);
        
        // Verify no message was sent
        await _mockAgent.DidNotReceive().ProcessMessageAsync(Arg.Any<AgentMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_BusyAgent_QueuesTask()
    {
        // Arrange
        _mockAgent.Status.Returns(AgentStatus.Processing); // Agent is busy

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["agent_id"] = _testAgentId,
                ["task"] = "This should be queued or rejected"
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.False(result.Success);
        Assert.Contains($"Agent '{_testAgentId}' is currently busy", result.Error);
        Assert.Contains("Processing", result.Error);
        
        // Verify no message was sent to the busy agent
        await _mockAgent.DidNotReceive().ProcessMessageAsync(Arg.Any<AgentMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithContext_PassesContext()
    {
        // Arrange
        var task = "Task with context data";
        var contextJson = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["session_id"] = "test-session-456",
            ["previous_result"] = "analysis completed",
            ["files"] = new[] { "file1.cs", "file2.cs" }
        });
        var contextElement = JsonSerializer.Deserialize<JsonElement>(contextJson);

        var responses = new List<AgentResponse>
        {
            new AgentResponse { Content = "Task received with context", IsComplete = true }
        };

        _mockAgent.ProcessMessageAsync(Arg.Any<AgentMessage>(), Arg.Any<CancellationToken>())
                  .Returns(CreateAsyncEnumerable(responses));

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["agent_id"] = _testAgentId,
                ["task"] = task,
                ["context"] = contextElement,
                ["wait_for_completion"] = false
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.True(result.Success);
        
        var contextMetadata = (Dictionary<string, object>)result.Metadata["context"];
        Assert.True(contextMetadata.ContainsKey("session_id"));
        Assert.True(contextMetadata.ContainsKey("previous_result"));
        Assert.True(contextMetadata.ContainsKey("files"));
        
        // Verify the agent received the message with context
        await _mockAgent.Received(1).ProcessMessageAsync(
            Arg.Is<AgentMessage>(msg => 
                msg.Content == task && 
                msg.Metadata.Count >= 2 && // Should have context plus handoff metadata
                msg.Metadata.ContainsKey("handoff_source") &&
                msg.Metadata.ContainsKey("handoff_timestamp")
            ), 
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task ExecuteAsync_ErrorStatus_ReturnsError()
    {
        // Arrange
        _mockAgent.Status.Returns(AgentStatus.Error); // Agent is in error state

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["agent_id"] = _testAgentId,
                ["task"] = "This should fail due to agent error"
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.False(result.Success);
        Assert.Contains($"Agent '{_testAgentId}' is not available", result.Error);
        Assert.Contains("Error", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_ShutdownStatus_ReturnsError()
    {
        // Arrange
        _mockAgent.Status.Returns(AgentStatus.Shutdown); // Agent is shutdown

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["agent_id"] = _testAgentId,
                ["task"] = "This should fail due to shutdown"
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.False(result.Success);
        Assert.Contains($"Agent '{_testAgentId}' is not available", result.Error);
        Assert.Contains("Shutdown", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_FireAndForget_DoesNotWait()
    {
        // Arrange
        var task = "Background task";
        var responses = new List<AgentResponse>
        {
            new AgentResponse { Content = "Starting background task...", IsComplete = false },
            new AgentResponse { Content = "Background task completed", IsComplete = true }
        };

        _mockAgent.ProcessMessageAsync(Arg.Any<AgentMessage>(), Arg.Any<CancellationToken>())
                  .Returns(CreateAsyncEnumerable(responses));

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["agent_id"] = _testAgentId,
                ["task"] = task,
                ["wait_for_completion"] = false
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.True(result.Success);
        
        var outputJson = JsonDocument.Parse(result.Output);
        Assert.False(outputJson.RootElement.GetProperty("waited_for_completion").GetBoolean());
        Assert.True(outputJson.RootElement.GetProperty("handoff_completed").GetBoolean());
        Assert.Contains("not waiting for completion", outputJson.RootElement.GetProperty("output").GetString());
        
        Assert.Equal(false, result.Metadata["waited_for_completion"]);
        Assert.Equal(true, result.Metadata["completion_status"]);
    }

    private static async IAsyncEnumerable<AgentResponse> CreateAsyncEnumerable(IEnumerable<AgentResponse> responses)
    {
        await Task.Yield(); // This makes the method truly async to avoid CS1998
        foreach (var response in responses)
        {
            yield return response;
        }
    }

    public void Dispose()
    {
        // No resources to dispose in this test class
    }
}