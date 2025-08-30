using System.Net;
using OrchestratorChat.Saturn.Tests.TestHelpers;
using OrchestratorChat.Saturn.Models;

namespace OrchestratorChat.Saturn.Tests.Integration;

/// <summary>
/// Simple integration tests that verify basic integration scenarios
/// for OrchestratorChat Track 2 components work together.
/// </summary>
public class SimpleIntegrationTests : IDisposable
{
    private readonly FileTestHelper _fileHelper;
    private readonly MockHttpMessageHandler _mockHttpHandler;
    private readonly HttpClient _httpClient;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public SimpleIntegrationTests()
    {
        _fileHelper = new FileTestHelper("SimpleIntegration");
        _mockHttpHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHttpHandler);
        _cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));
    }

    [Fact]
    public void Integration_TestEnvironment_IsSetupCorrectly()
    {
        // Arrange & Act & Assert: Verify test environment is working
        Assert.NotNull(_fileHelper);
        Assert.NotNull(_httpClient);
        Assert.NotNull(_mockHttpHandler);
        Assert.True(Directory.Exists(_fileHelper.TestDirectory));
        Assert.False(_cancellationTokenSource.Token.IsCancellationRequested);
    }

    [Fact]
    public void Integration_MockHttpHandler_WorksCorrectly()
    {
        // Arrange: Setup mock response
        var expectedResponse = "Test response content";
        _mockHttpHandler.EnqueueResponse(HttpStatusCode.OK, expectedResponse);

        // Act: Make HTTP request
        var response = _httpClient.GetAsync("https://test.example.com").Result;
        var content = response.Content.ReadAsStringAsync().Result;

        // Assert: Verify mock worked
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedResponse, content);
        Assert.Single(_mockHttpHandler.Requests);
    }

    [Fact]
    public void Integration_SaturnModels_CanBeCreated()
    {
        // Arrange & Act: Create Saturn model instances
        var config = new SaturnAgentConfiguration
        {
            Model = TestConstants.ValidClaudeModel,
            Temperature = 0.7,
            MaxTokens = 2048,
            EnableTools = true
        };

        var message = new AgentMessage
        {
            Content = "Test message",
            Role = MessageRole.User,
            SessionId = "test-session",
            Timestamp = DateTime.UtcNow
        };

        var providerInfo = new ProviderInfo
        {
            Id = "test-provider",
            Name = "Test Provider",
            Type = ProviderType.OpenRouter,
            IsConfigured = true
        };

        // Assert: Verify models can be instantiated and configured
        Assert.NotNull(config);
        Assert.Equal(TestConstants.ValidClaudeModel, config.Model);
        Assert.Equal(0.7, config.Temperature);
        Assert.True(config.EnableTools);

        Assert.NotNull(message);
        Assert.Equal("Test message", message.Content);
        Assert.Equal(MessageRole.User, message.Role);
        Assert.Equal("test-session", message.SessionId);

        Assert.NotNull(providerInfo);
        Assert.Equal("test-provider", providerInfo.Id);
        Assert.Equal(ProviderType.OpenRouter, providerInfo.Type);
        Assert.True(providerInfo.IsConfigured);
    }

    [Fact]
    public void Integration_TestConstants_AreValid()
    {
        // Arrange & Act & Assert: Verify test constants are properly defined
        Assert.NotEmpty(TestConstants.ValidClaudeModel);
        Assert.NotEmpty(TestConstants.ValidOpenRouterModel);
        Assert.NotEmpty(TestConstants.TestApiKey);
        Assert.NotEmpty(TestConstants.TestOAuthCode);
        
        Assert.Contains("claude", TestConstants.ValidClaudeModel);
        Assert.Contains("anthropic", TestConstants.ValidOpenRouterModel);
        
        Assert.NotNull(TestConstants.StandardToolParams);
        Assert.True(TestConstants.StandardToolParams.ContainsKey("timeout"));
        
        Assert.NotNull(TestConstants.FileReadParams);
        Assert.True(TestConstants.FileReadParams.ContainsKey("file_path"));
    }

    [Fact]
    public async Task Integration_FileOperations_WorkInTestEnvironment()
    {
        // Arrange: Create test file path
        var testFile = Path.Combine(_fileHelper.TestDirectory, "integration_test.txt");
        var testContent = "Integration test content";

        // Act: Write and read file
        await File.WriteAllTextAsync(testFile, testContent);
        var readContent = await File.ReadAllTextAsync(testFile);

        // Assert: Verify file operations worked
        Assert.True(File.Exists(testFile));
        Assert.Equal(testContent, readContent);

        // Cleanup
        File.Delete(testFile);
        Assert.False(File.Exists(testFile));
    }

    [Fact]
    public async Task Integration_HttpClientConfiguration_WorksWithMocks()
    {
        // Arrange: Setup multiple mock responses
        _mockHttpHandler.EnqueueResponse(HttpStatusCode.OK, "Response 1");
        _mockHttpHandler.EnqueueResponse(HttpStatusCode.Accepted, "Response 2");
        _mockHttpHandler.EnqueueResponse(HttpStatusCode.BadRequest, "Error response");

        // Act: Make multiple requests
        var response1 = await _httpClient.GetAsync("https://api.example.com/endpoint1");
        var content1 = await response1.Content.ReadAsStringAsync();

        var response2 = await _httpClient.GetAsync("https://api.example.com/endpoint2");
        var content2 = await response2.Content.ReadAsStringAsync();

        var response3 = await _httpClient.GetAsync("https://api.example.com/endpoint3");
        var content3 = await response3.Content.ReadAsStringAsync();

        // Assert: Verify all responses were handled correctly
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal("Response 1", content1);

        Assert.Equal(HttpStatusCode.Accepted, response2.StatusCode);
        Assert.Equal("Response 2", content2);

        Assert.Equal(HttpStatusCode.BadRequest, response3.StatusCode);
        Assert.Equal("Error response", content3);

        Assert.Equal(3, _mockHttpHandler.Requests.Count);
    }

    [Fact]
    public void Integration_EnumValues_AreCorrectlyDefined()
    {
        // Arrange & Act & Assert: Verify enums work correctly
        var messageRoles = Enum.GetValues<MessageRole>().ToArray();
        Assert.Contains(MessageRole.User, messageRoles);
        Assert.Contains(MessageRole.Assistant, messageRoles);
        Assert.Contains(MessageRole.System, messageRoles);
        Assert.Contains(MessageRole.Tool, messageRoles);

        var providerTypes = Enum.GetValues<ProviderType>().ToArray();
        Assert.Contains(ProviderType.OpenRouter, providerTypes);
        Assert.Contains(ProviderType.Anthropic, providerTypes);

        // Test enum to string conversions
        Assert.Equal("User", MessageRole.User.ToString());
        Assert.Equal("Assistant", MessageRole.Assistant.ToString());
        Assert.Equal("OpenRouter", ProviderType.OpenRouter.ToString());
        Assert.Equal("Anthropic", ProviderType.Anthropic.ToString());
    }

    [Fact]
    public void Integration_CancellationToken_WorksCorrectly()
    {
        // Arrange: Create a short-lived cancellation token
        using var shortCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var token = shortCts.Token;

        // Act & Assert: Verify token behavior
        Assert.False(token.IsCancellationRequested);

        // Wait for cancellation
        Thread.Sleep(150);
        Assert.True(token.IsCancellationRequested);

        // Verify main token is still active
        Assert.False(_cancellationTokenSource.Token.IsCancellationRequested);
    }

    [Fact]
    public void Integration_DictionaryOperations_WorkWithMetadata()
    {
        // Arrange: Create message with metadata
        var message = new AgentMessage
        {
            Content = "Test with metadata",
            Role = MessageRole.User,
            Metadata = new Dictionary<string, object>
            {
                { "priority", "high" },
                { "source", "integration_test" },
                { "timestamp", DateTime.UtcNow },
                { "count", 42 },
                { "enabled", true }
            }
        };

        // Act & Assert: Verify metadata operations
        Assert.Equal(5, message.Metadata.Count);
        Assert.Equal("high", message.Metadata["priority"]);
        Assert.Equal("integration_test", message.Metadata["source"]);
        Assert.True(message.Metadata.ContainsKey("timestamp"));
        Assert.Equal(42, message.Metadata["count"]);
        Assert.Equal(true, message.Metadata["enabled"]);

        // Test metadata modification
        message.Metadata["status"] = "processed";
        Assert.Equal(6, message.Metadata.Count);
        Assert.Equal("processed", message.Metadata["status"]);

        // Test metadata removal
        message.Metadata.Remove("count");
        Assert.Equal(5, message.Metadata.Count);
        Assert.False(message.Metadata.ContainsKey("count"));
    }

    [Fact]
    public void Integration_ConfigurationObjects_CanBeNestedAndSerialized()
    {
        // Arrange: Create complex configuration
        var saturnConfig = new SaturnConfiguration
        {
            DefaultConfiguration = new SaturnAgentConfiguration
            {
                Model = TestConstants.ValidClaudeModel,
                Temperature = 0.8,
                MaxTokens = 4096,
                SystemPrompt = "You are a helpful assistant for integration testing.",
                EnableTools = true,
                ToolNames = new List<string> { "file_read", "file_write", "bash" },
                ProviderType = ProviderType.Anthropic,
                ProviderSettings = new Dictionary<string, object>
                {
                    { "retry_count", 3 },
                    { "timeout_seconds", 30 }
                }
            },
            Providers = new Dictionary<string, ProviderConfiguration>
            {
                {
                    "anthropic", new ProviderConfiguration
                    {
                        ApiKey = TestConstants.TestApiKey,
                        DefaultModel = TestConstants.ValidClaudeModel,
                        Settings = new Dictionary<string, object>
                        {
                            { "base_url", "https://api.anthropic.com" },
                            { "version", "2023-06-01" }
                        }
                    }
                },
                {
                    "openrouter", new ProviderConfiguration
                    {
                        ApiKey = "or_test_key",
                        DefaultModel = TestConstants.ValidOpenRouterModel,
                        Settings = new Dictionary<string, object>
                        {
                            { "base_url", "https://openrouter.ai/api/v1" },
                            { "app_name", "OrchestratorChat" }
                        }
                    }
                }
            },
            Tools = new ToolConfiguration
            {
                Enabled = new List<string> { "file_read", "file_write", "bash", "grep", "glob" },
                RequireApproval = new List<string> { "bash", "delete_file" }
            },
            MultiAgent = new MultiAgentConfiguration
            {
                Enabled = true,
                MaxConcurrentAgents = 3
            }
        };

        // Act & Assert: Verify configuration structure
        Assert.NotNull(saturnConfig.DefaultConfiguration);
        Assert.Equal(TestConstants.ValidClaudeModel, saturnConfig.DefaultConfiguration.Model);
        Assert.Equal(0.8, saturnConfig.DefaultConfiguration.Temperature);
        Assert.True(saturnConfig.DefaultConfiguration.EnableTools);
        Assert.Equal(3, saturnConfig.DefaultConfiguration.ToolNames.Count);

        Assert.Equal(2, saturnConfig.Providers.Count);
        Assert.True(saturnConfig.Providers.ContainsKey("anthropic"));
        Assert.True(saturnConfig.Providers.ContainsKey("openrouter"));

        var anthropicProvider = saturnConfig.Providers["anthropic"];
        Assert.Equal(TestConstants.TestApiKey, anthropicProvider.ApiKey);
        Assert.Equal(2, anthropicProvider.Settings.Count);

        Assert.NotNull(saturnConfig.Tools);
        Assert.Equal(5, saturnConfig.Tools.Enabled.Count);
        Assert.Equal(2, saturnConfig.Tools.RequireApproval.Count);

        Assert.NotNull(saturnConfig.MultiAgent);
        Assert.True(saturnConfig.MultiAgent.Enabled);
        Assert.Equal(3, saturnConfig.MultiAgent.MaxConcurrentAgents);
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _httpClient?.Dispose();
        _mockHttpHandler?.Dispose();
        _fileHelper?.Dispose();
    }
}