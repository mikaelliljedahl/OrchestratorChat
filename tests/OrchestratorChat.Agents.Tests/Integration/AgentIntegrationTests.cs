using OrchestratorChat.Agents.Tests.TestHelpers;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Messages;
using OrchestratorChat.Core.Tools;

namespace OrchestratorChat.Agents.Tests.Integration;

/// <summary>
/// Integration tests for OrchestratorChat Agents Track 2 components.
/// These tests verify that agent-related components work together correctly.
/// </summary>
public class AgentIntegrationTests : IDisposable
{
    private readonly FileTestHelper _fileHelper;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public AgentIntegrationTests()
    {
        _fileHelper = new FileTestHelper("AgentIntegration");
        _cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));
    }

    [Fact]
    public void Integration_TestEnvironment_IsSetupCorrectly()
    {
        // Arrange & Act & Assert: Verify test environment is working
        Assert.NotNull(_fileHelper);
        Assert.True(Directory.Exists(_fileHelper.TestDirectory));
        Assert.False(_cancellationTokenSource.Token.IsCancellationRequested);
    }

    [Fact]
    public void Integration_CoreModels_CanBeCreated()
    {
        // Arrange & Act: Create Core model instances
        var agentConfig = new AgentConfiguration
        {
            Name = "TestAgent",
            Type = AgentType.Claude,
            WorkingDirectory = _fileHelper.TestDirectory,
            Model = "claude-3-sonnet",
            Temperature = 0.7,
            MaxTokens = 2048,
            SystemPrompt = "You are a helpful assistant for integration testing."
        };

        var message = new Message
        {
            Id = Guid.NewGuid().ToString(),
            Content = "Test message for integration",
            Type = MessageType.UserMessage,
            Timestamp = DateTime.UtcNow,
            SessionId = "test-session-123"
        };

        var toolRequest = new ToolRequest(
            "test_tool",
            new Dictionary<string, object>
            {
                { "parameter1", "value1" },
                { "parameter2", 42 },
                { "parameter3", true }
            }
        );

        // Assert: Verify models can be instantiated and configured
        Assert.NotNull(agentConfig);
        Assert.Equal("TestAgent", agentConfig.Name);
        Assert.Equal(AgentType.Claude, agentConfig.Type);
        Assert.Equal(_fileHelper.TestDirectory, agentConfig.WorkingDirectory);
        Assert.Equal("claude-3-sonnet", agentConfig.Model);
        Assert.Equal(0.7, agentConfig.Temperature);

        Assert.NotNull(message);
        Assert.Equal(MessageType.UserMessage, message.Type);
        Assert.Contains("integration", message.Content);
        Assert.Equal("test-session-123", message.SessionId);

        Assert.NotNull(toolRequest);
        Assert.Equal("test_tool", toolRequest.ToolName);
        Assert.Equal(3, toolRequest.Parameters.Count);
        Assert.Equal("value1", toolRequest.Parameters["parameter1"]);
        Assert.Equal(42, toolRequest.Parameters["parameter2"]);
        Assert.Equal(true, toolRequest.Parameters["parameter3"]);
    }

    [Fact]
    public void Integration_AgentEnums_AreCorrectlyDefined()
    {
        // Arrange & Act & Assert: Verify enums work correctly
        var agentTypes = Enum.GetValues<AgentType>().ToArray();
        Assert.Contains(AgentType.Claude, agentTypes);
        Assert.Contains(AgentType.Saturn, agentTypes);

        var agentStatuses = Enum.GetValues<AgentStatus>().ToArray();
        Assert.Contains(AgentStatus.Ready, agentStatuses);
        Assert.Contains(AgentStatus.Busy, agentStatuses);
        Assert.Contains(AgentStatus.Error, agentStatuses);

        var messageTypes = Enum.GetValues<MessageType>().ToArray();
        Assert.Contains(MessageType.UserMessage, messageTypes);
        Assert.Contains(MessageType.AgentResponse, messageTypes);
        Assert.Contains(MessageType.ToolRequest, messageTypes);

        // Test enum to string conversions
        Assert.Equal("Claude", AgentType.Claude.ToString());
        Assert.Equal("Saturn", AgentType.Saturn.ToString());
        Assert.Equal("Ready", AgentStatus.Ready.ToString());
        Assert.Equal("UserMessage", MessageType.UserMessage.ToString());
    }

    [Fact]
    public async Task Integration_FileOperations_WorkInTestEnvironment()
    {
        // Arrange: Create test files and directories
        var testFile = Path.Combine(_fileHelper.TestDirectory, "agent_test.txt");
        var testSubDir = Path.Combine(_fileHelper.TestDirectory, "subdir");
        var testContent = "Agent integration test content\nWith multiple lines\nFor testing purposes.";

        // Act: Perform file operations
        Directory.CreateDirectory(testSubDir);
        await File.WriteAllTextAsync(testFile, testContent);
        var readContent = await File.ReadAllTextAsync(testFile);

        // Assert: Verify file operations worked
        Assert.True(Directory.Exists(testSubDir));
        Assert.True(File.Exists(testFile));
        Assert.Equal(testContent, readContent);
        Assert.Contains("integration test", readContent);

        // Test file metadata
        var fileInfo = new FileInfo(testFile);
        Assert.True(fileInfo.Exists);
        Assert.True(fileInfo.Length > 0);

        // Cleanup and verify
        File.Delete(testFile);
        Directory.Delete(testSubDir);
        Assert.False(File.Exists(testFile));
        Assert.False(Directory.Exists(testSubDir));
    }

    [Fact]
    public void Integration_ToolResult_CanHandleDifferentScenarios()
    {
        // Arrange & Act: Create different tool results
        var successResult = new ToolResult { Success = true, Content = "Operation completed successfully", Error = null };
        var failureResult = new ToolResult { Success = false, Content = null, Error = "Operation failed due to invalid input" };
        var partialResult = new ToolResult { Success = true, Content = "Partial success", Error = "Warning: Some items skipped" };

        // Assert: Verify tool results work correctly
        Assert.True(successResult.Success);
        Assert.Equal("Operation completed successfully", successResult.Content);
        Assert.Null(successResult.Error);

        Assert.False(failureResult.Success);
        Assert.Null(failureResult.Content);
        Assert.Equal("Operation failed due to invalid input", failureResult.Error);

        Assert.True(partialResult.Success);
        Assert.Equal("Partial success", partialResult.Content);
        Assert.Equal("Warning: Some items skipped", partialResult.Error);
    }

    [Fact]
    public void Integration_AgentConfiguration_CanHandleComplexParameters()
    {
        // Arrange: Create complex agent configuration
        var complexConfig = new AgentConfiguration
        {
            Name = "ComplexAgent",
            Type = AgentType.Saturn,
            WorkingDirectory = _fileHelper.TestDirectory,
            Model = "claude-3-opus",
            Temperature = 0.3,
            MaxTokens = 8192,
            SystemPrompt = "You are an advanced agent with complex capabilities.",
            Parameters = new Dictionary<string, object>
            {
                // Nested configuration
                { "provider_settings", new Dictionary<string, object>
                    {
                        { "base_url", "https://api.example.com" },
                        { "timeout", 30 },
                        { "retry_count", 3 }
                    }
                },
                // Tool configuration
                { "enabled_tools", new List<string> { "file_read", "file_write", "bash", "web_search" } },
                // Feature flags
                { "enable_streaming", true },
                { "enable_function_calling", true },
                { "enable_vision", false },
                // Numeric parameters
                { "max_concurrent_requests", 5 },
                { "cache_ttl_seconds", 3600 }
            }
        };

        // Act & Assert: Verify complex parameters
        Assert.Equal(5, complexConfig.Parameters.Count);
        Assert.True(complexConfig.Parameters.ContainsKey("provider_settings"));
        Assert.True(complexConfig.Parameters.ContainsKey("enabled_tools"));
        
        // Verify nested dictionary
        var providerSettings = (Dictionary<string, object>)complexConfig.Parameters["provider_settings"];
        Assert.Equal(3, providerSettings.Count);
        Assert.Equal("https://api.example.com", providerSettings["base_url"]);
        Assert.Equal(30, providerSettings["timeout"]);

        // Verify list parameter
        var enabledTools = (List<string>)complexConfig.Parameters["enabled_tools"];
        Assert.Equal(4, enabledTools.Count);
        Assert.Contains("file_read", enabledTools);
        Assert.Contains("bash", enabledTools);

        // Verify boolean and numeric parameters
        Assert.True((bool)complexConfig.Parameters["enable_streaming"]);
        Assert.False((bool)complexConfig.Parameters["enable_vision"]);
        Assert.Equal(5, complexConfig.Parameters["max_concurrent_requests"]);
    }

    [Fact]
    public void Integration_MessageMetadata_HandlesComplexData()
    {
        // Arrange: Create message with complex metadata
        var message = new Message
        {
            Id = Guid.NewGuid().ToString(),
            Content = "Complex message with rich metadata",
            Type = MessageType.AgentResponse,
            Timestamp = DateTime.UtcNow,
            SessionId = "complex-session",
            AgentId = "agent-123",
            Metadata = new Dictionary<string, object>
            {
                // Execution metadata
                { "execution_time_ms", 1250 },
                { "token_usage", new Dictionary<string, object>
                    {
                        { "prompt_tokens", 150 },
                        { "completion_tokens", 75 },
                        { "total_tokens", 225 }
                    }
                },
                // Tool calls made
                { "tools_called", new List<Dictionary<string, object>>
                    {
                        new() {
                            { "name", "file_read" },
                            { "duration_ms", 45 },
                            { "success", true }
                        },
                        new() {
                            { "name", "web_search" },
                            { "duration_ms", 1200 },
                            { "success", true },
                            { "results_count", 5 }
                        }
                    }
                },
                // Message classification
                { "intent", "information_retrieval" },
                { "confidence", 0.92 },
                { "contains_code", false },
                { "language", "en" }
            }
        };

        // Act & Assert: Verify complex metadata handling
        Assert.Equal(7, message.Metadata.Count);
        Assert.Equal(1250, message.Metadata["execution_time_ms"]);
        
        // Verify nested token usage
        var tokenUsage = (Dictionary<string, object>)message.Metadata["token_usage"];
        Assert.Equal(225, tokenUsage["total_tokens"]);
        
        // Verify tools called array
        var toolsCalled = (List<Dictionary<string, object>>)message.Metadata["tools_called"];
        Assert.Equal(2, toolsCalled.Count);
        Assert.Equal("file_read", toolsCalled[0]["name"]);
        Assert.Equal(true, toolsCalled[0]["success"]);
        Assert.Equal("web_search", toolsCalled[1]["name"]);
        Assert.Equal(5, toolsCalled[1]["results_count"]);
        
        // Verify other metadata
        Assert.Equal("information_retrieval", message.Metadata["intent"]);
        Assert.Equal(0.92, message.Metadata["confidence"]);
        Assert.Equal(false, message.Metadata["contains_code"]);
    }

    [Fact]
    public void Integration_GuidGeneration_IsUnique()
    {
        // Arrange: Generate multiple GUIDs
        var guids = new HashSet<string>();
        var iterations = 1000;

        // Act: Generate GUIDs and check uniqueness
        for (int i = 0; i < iterations; i++)
        {
            var guid = Guid.NewGuid().ToString();
            Assert.True(guids.Add(guid), $"Duplicate GUID generated: {guid}");
        }

        // Assert: Verify all GUIDs are unique
        Assert.Equal(iterations, guids.Count);

        // Verify GUID format
        var sampleGuid = guids.First();
        Assert.Equal(36, sampleGuid.Length); // Standard GUID string length
        Assert.Equal(4, sampleGuid.Count(c => c == '-')); // Standard GUID format has 4 hyphens
    }

    [Fact]
    public void Integration_DateTimeHandling_IsConsistent()
    {
        // Arrange: Create timestamps
        var utcNow = DateTime.UtcNow;
        var localNow = DateTime.Now;
        var specificTime = new DateTime(2024, 8, 30, 15, 30, 45, DateTimeKind.Utc);

        // Act: Create messages with different timestamps
        var messages = new[]
        {
            new Message { Id = "1", Content = "UTC message", Timestamp = utcNow },
            new Message { Id = "2", Content = "Local message", Timestamp = localNow },
            new Message { Id = "3", Content = "Specific message", Timestamp = specificTime }
        };

        // Assert: Verify timestamp handling
        Assert.Equal(utcNow, messages[0].Timestamp);
        Assert.Equal(localNow, messages[1].Timestamp);
        Assert.Equal(specificTime, messages[2].Timestamp);

        // Verify chronological ordering
        var sortedMessages = messages.OrderBy(m => m.Timestamp).ToArray();
        Assert.Equal(3, sortedMessages.Length);

        // Verify timestamp differences
        var timeDiff = utcNow - specificTime;
        Assert.True(timeDiff.TotalSeconds > 0, "UTC now should be after specific time");
    }

    [Fact]
    public async Task Integration_ConcurrentOperations_AreThreadSafe()
    {
        // Arrange: Setup concurrent operations
        var tasks = new List<Task<Message>>();
        var taskCount = 10;
        var messages = new Dictionary<string, Message>();
        var lockObject = new object();

        // Act: Create messages concurrently
        for (int i = 0; i < taskCount; i++)
        {
            var taskId = i;
            var task = Task.Run(() =>
            {
                var message = new Message
                {
                    Id = $"concurrent-{taskId}",
                    Content = $"Concurrent message {taskId}",
                    Type = MessageType.UserMessage,
                    Timestamp = DateTime.UtcNow,
                    SessionId = $"session-{taskId % 3}", // 3 different sessions
                    Metadata = new Dictionary<string, object>
                    {
                        { "task_id", taskId },
                        { "thread_id", Thread.CurrentThread.ManagedThreadId }
                    }
                };

                // Simulate some work
                Thread.Sleep(Random.Shared.Next(10, 50));

                lock (lockObject)
                {
                    messages[message.Id] = message;
                }

                return message;
            });
            tasks.Add(task);
        }

        var results = await Task.WhenAll(tasks);

        // Assert: Verify concurrent operations
        Assert.Equal(taskCount, results.Length);
        Assert.Equal(taskCount, messages.Count);
        
        // Verify all messages were created correctly
        for (int i = 0; i < taskCount; i++)
        {
            var expectedId = $"concurrent-{i}";
            Assert.True(messages.ContainsKey(expectedId));
            
            var message = messages[expectedId];
            Assert.Equal($"Concurrent message {i}", message.Content);
            Assert.Equal(i, message.Metadata["task_id"]);
        }

        // Verify session distribution
        var sessionGroups = messages.Values.GroupBy(m => m.SessionId).ToArray();
        Assert.Equal(3, sessionGroups.Length);
        Assert.All(sessionGroups, group => Assert.True(group.Count() > 0));
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _fileHelper?.Dispose();
    }
}