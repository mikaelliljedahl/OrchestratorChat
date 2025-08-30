using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Messages;
using OrchestratorChat.Core.Tools;
using OrchestratorChat.Agents.Saturn;
using OrchestratorChat.Agents.Tests.TestHelpers;
using AgentsSaturnCore = OrchestratorChat.Agents.Saturn.ISaturnCore;
using AgentsSaturnConfig = OrchestratorChat.Agents.Saturn.SaturnConfiguration;
using SaturnProviderType = OrchestratorChat.Saturn.Models.ProviderType;

namespace OrchestratorChat.Agents.Tests.Saturn;

/// <summary>
/// SaturnAgent lifecycle tests covering initialization with different providers,
/// message processing, tool execution, and streaming responses.
/// </summary>
public class SaturnAgentTests : IDisposable
{
    private readonly ILogger<SaturnAgent> _logger;
    private readonly AgentsSaturnCore _mockSaturnCore;
    private readonly IOptions<AgentsSaturnConfig> _configuration;
    private readonly MockProcessHelper _processHelper;

    public SaturnAgentTests()
    {
        _logger = Substitute.For<ILogger<SaturnAgent>>();
        _mockSaturnCore = Substitute.For<AgentsSaturnCore>();
        _processHelper = new MockProcessHelper();
        
        var config = new AgentsSaturnConfig
        {
            DefaultProvider = "OpenRouter",
            MaxSubAgents = TestConstants.MaxSubAgents,
            SupportedModels = new[] { TestConstants.ValidOpenRouterModel, TestConstants.ValidClaudeModel },
            EnableToolExecution = true,
            HealthCheckIntervalMs = TestConstants.HealthCheckIntervalMs
        };
        _configuration = Substitute.For<IOptions<AgentsSaturnConfig>>();
        _configuration.Value.Returns(config);
    }

    [Fact]
    public async Task InitializeAsync_ValidProvider_InitializesSuccessfully()
    {
        // Arrange
        var mockProvider = Substitute.For<OrchestratorChat.Saturn.Providers.ILLMProvider>();
        mockProvider.IsAvailable.Returns(true);
        mockProvider.ProviderType.Returns(SaturnProviderType.OpenRouter);
        
        _mockSaturnCore.CreateProviderAsync("OpenRouter", Arg.Any<Dictionary<string, object>>())
            .Returns(mockProvider);

        var agent = new SaturnAgent(_logger, _mockSaturnCore, _configuration);
        var agentConfig = new AgentConfiguration
        {
            Id = TestConstants.DefaultAgentId,
            Name = TestConstants.DefaultAgentName,
            Type = AgentType.Saturn,
            WorkingDirectory = Directory.GetCurrentDirectory(),
            Parameters = new Dictionary<string, object>
            {
                { "provider", "OpenRouter" },
                { "model", TestConstants.ValidOpenRouterModel },
                { "api_key", TestConstants.TestApiKey }
            }
        };

        // Act
        var result = await agent.InitializeAsync(agentConfig);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(AgentStatus.Ready, agent.Status);
        Assert.Equal(TestConstants.DefaultAgentName, agent.Name);
        Assert.Equal(AgentType.Saturn, agent.Type);
        Assert.NotNull(result.Capabilities);

        // Verify SaturnCore was called
        await _mockSaturnCore.Received(1).CreateProviderAsync("OpenRouter", Arg.Any<Dictionary<string, object>>());
    }

    [Fact]
    public async Task SendMessageAsync_WithOpenRouter_ProcessesCorrectly()
    {
        // Arrange
        var mockProvider = Substitute.For<OrchestratorChat.Saturn.Providers.ILLMProvider>();
        mockProvider.IsAvailable.Returns(true);
        mockProvider.ProviderType.Returns(SaturnProviderType.OpenRouter);
        
        var mockResponse = new AgentResponse
        {
            Id = Guid.NewGuid().ToString(),
            Content = "Response from OpenRouter",
            Role = MessageRole.Assistant,
            Type = ResponseType.Success,
            Timestamp = DateTime.UtcNow,
            TokenUsage = new TokenUsage { InputTokens = 10, OutputTokens = 15 }
        };

        _mockSaturnCore.CreateProviderAsync("OpenRouter", Arg.Any<Dictionary<string, object>>())
            .Returns(mockProvider);
        _mockSaturnCore.SendMessageAsync(Arg.Any<AgentMessage>(), Arg.Any<OrchestratorChat.Saturn.Providers.ILLMProvider>())
            .Returns(mockResponse);

        var agent = new SaturnAgent(_logger, _mockSaturnCore, _configuration);
        var agentConfig = new AgentConfiguration
        {
            Id = TestConstants.DefaultAgentId,
            Name = TestConstants.DefaultAgentName,
            Type = AgentType.Saturn,
            Parameters = new Dictionary<string, object>
            {
                { "provider", "OpenRouter" },
                { "model", TestConstants.ValidOpenRouterModel },
                { "api_key", TestConstants.TestApiKey }
            }
        };

        await agent.InitializeAsync(agentConfig);

        var message = new AgentMessage
        {
            Id = Guid.NewGuid().ToString(),
            Content = "Test message for OpenRouter",
            Role = MessageRole.User,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var response = await agent.SendMessageAsync(message);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(ResponseType.Success, response.Type);
        Assert.Contains("Response from OpenRouter", response.Content);
        Assert.Equal(MessageRole.Assistant, response.Role);
        Assert.NotNull(response.TokenUsage);
        Assert.Equal(10, response.TokenUsage.InputTokens);
        Assert.Equal(15, response.TokenUsage.OutputTokens);

        // Verify SaturnCore was called
        await _mockSaturnCore.Received(1).SendMessageAsync(Arg.Any<AgentMessage>(), mockProvider);
    }

    [Fact]
    public async Task SendMessageAsync_WithAnthropic_ProcessesCorrectly()
    {
        // Arrange
        var mockProvider = Substitute.For<OrchestratorChat.Saturn.Providers.ILLMProvider>();
        mockProvider.IsAvailable.Returns(true);
        mockProvider.ProviderType.Returns(SaturnProviderType.Anthropic);
        
        var mockResponse = new AgentResponse
        {
            Id = Guid.NewGuid().ToString(),
            Content = "Response from Anthropic Claude",
            Role = MessageRole.Assistant,
            Type = ResponseType.Success,
            Timestamp = DateTime.UtcNow,
            TokenUsage = new TokenUsage { InputTokens = 12, OutputTokens = 18 }
        };

        _mockSaturnCore.CreateProviderAsync("Anthropic", Arg.Any<Dictionary<string, object>>())
            .Returns(mockProvider);
        _mockSaturnCore.SendMessageAsync(Arg.Any<AgentMessage>(), Arg.Any<OrchestratorChat.Saturn.Providers.ILLMProvider>())
            .Returns(mockResponse);

        var agent = new SaturnAgent(_logger, _mockSaturnCore, _configuration);
        var agentConfig = new AgentConfiguration
        {
            Id = TestConstants.DefaultAgentId,
            Name = TestConstants.DefaultAgentName,
            Type = AgentType.Saturn,
            Parameters = new Dictionary<string, object>
            {
                { "provider", "Anthropic" },
                { "model", TestConstants.ValidClaudeModel },
                { "access_token", TestConstants.TestAccessToken }
            }
        };

        await agent.InitializeAsync(agentConfig);

        var message = new AgentMessage
        {
            Id = Guid.NewGuid().ToString(),
            Content = "Test message for Anthropic",
            Role = MessageRole.User,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var response = await agent.SendMessageAsync(message);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(ResponseType.Success, response.Type);
        Assert.Contains("Response from Anthropic Claude", response.Content);
        Assert.Equal(MessageRole.Assistant, response.Role);
        Assert.NotNull(response.TokenUsage);
        Assert.Equal(12, response.TokenUsage.InputTokens);
        Assert.Equal(18, response.TokenUsage.OutputTokens);

        // Verify SaturnCore was called
        await _mockSaturnCore.Received(1).SendMessageAsync(Arg.Any<AgentMessage>(), mockProvider);
    }

    [Fact]
    public async Task ExecuteToolAsync_CallsSaturnCore()
    {
        // Arrange
        var mockProvider = Substitute.For<OrchestratorChat.Saturn.Providers.ILLMProvider>();
        mockProvider.IsAvailable.Returns(true);
        
        var toolResult = new ToolExecutionResult
        {
            Success = true,
            Output = "Tool executed successfully",
            ToolName = "test_tool",
            ExecutionTime = TimeSpan.FromMilliseconds(100)
        };

        _mockSaturnCore.CreateProviderAsync("OpenRouter", Arg.Any<Dictionary<string, object>>())
            .Returns(mockProvider);
        _mockSaturnCore.ExecuteToolAsync(Arg.Any<string>(), Arg.Any<Dictionary<string, object>>())
            .Returns(Task.FromResult(new OrchestratorChat.Saturn.Models.ToolExecutionResult
            {
                Success = true,
                Output = "Tool executed successfully",
                ToolName = "test_tool",
                ExecutionTime = TimeSpan.FromMilliseconds(100)
            }));

        var agent = new SaturnAgent(_logger, _mockSaturnCore, _configuration);
        var agentConfig = new AgentConfiguration
        {
            Id = TestConstants.DefaultAgentId,
            Name = TestConstants.DefaultAgentName,
            Type = AgentType.Saturn,
            Parameters = new Dictionary<string, object>
            {
                { "provider", "OpenRouter" },
                { "model", TestConstants.ValidOpenRouterModel },
                { "api_key", TestConstants.TestApiKey }
            }
        };

        await agent.InitializeAsync(agentConfig);

        var toolCall = new ToolCall
        {
            Id = Guid.NewGuid().ToString(),
            Name = "test_tool",
            Parameters = TestConstants.StandardToolParams,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await agent.ExecuteToolAsync(toolCall);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("Tool executed successfully", result.Output);
        Assert.Equal("test_tool", result.ToolName);
        Assert.True(result.ExecutionTime.TotalMilliseconds > 0);

        // Verify Saturn core was called
        await _mockSaturnCore.Received(1).ExecuteToolAsync("test_tool", TestConstants.StandardToolParams);
    }

    [Fact]
    public async Task HandleStreamingResponse_EventsEmitted()
    {
        // Arrange
        var mockProvider = Substitute.For<OrchestratorChat.Saturn.Providers.ILLMProvider>();
        mockProvider.IsAvailable.Returns(true);
        
        var outputEvents = new List<string>();
        var statusChanges = new List<AgentStatus>();

        _mockSaturnCore.CreateProviderAsync("OpenRouter", Arg.Any<Dictionary<string, object>>())
            .Returns(mockProvider);

        var agent = new SaturnAgent(_logger, _mockSaturnCore, _configuration);
        
        agent.OutputReceived += (sender, args) => outputEvents.Add(args.Output);
        agent.StatusChanged += (sender, args) => statusChanges.Add(args.NewStatus);

        var agentConfig = new AgentConfiguration
        {
            Id = TestConstants.DefaultAgentId,
            Name = TestConstants.DefaultAgentName,
            Type = AgentType.Saturn,
            Parameters = new Dictionary<string, object>
            {
                { "provider", "OpenRouter" },
                { "model", TestConstants.ValidOpenRouterModel },
                { "api_key", TestConstants.TestApiKey }
            }
        };

        await agent.InitializeAsync(agentConfig);

        // Setup streaming response
        var streamingChunks = new[] { "Chunk 1", "Chunk 2", "Chunk 3" };
        var streamingResponse = new AgentResponse
        {
            Id = Guid.NewGuid().ToString(),
            Content = string.Join("", streamingChunks),
            Role = MessageRole.Assistant,
            Type = ResponseType.Success,
            Timestamp = DateTime.UtcNow
        };

        _mockSaturnCore.SendMessageAsync(Arg.Any<AgentMessage>(), Arg.Any<OrchestratorChat.Saturn.Providers.ILLMProvider>())
            .Returns(streamingResponse);

        var message = new AgentMessage
        {
            Id = Guid.NewGuid().ToString(),
            Content = "Test streaming message",
            Role = MessageRole.User,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var response = await agent.SendMessageAsync(message);

        // Small delay to allow events to be processed
        await Task.Delay(100);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(ResponseType.Success, response.Type);
        
        // Verify status changed events (should include Initializing and Ready at minimum)
        Assert.Contains(AgentStatus.Ready, statusChanges);
        
        // OutputReceived events would depend on the Saturn core implementation
        // For now, verify the response content contains the expected data
        Assert.Contains("Chunk", response.Content);
    }

    [Fact]
    public async Task ShutdownAsync_CleansUpResources()
    {
        // Arrange
        var mockProvider = Substitute.For<OrchestratorChat.Saturn.Providers.ILLMProvider>();
        mockProvider.IsAvailable.Returns(true);
        
        _mockSaturnCore.CreateProviderAsync("OpenRouter", Arg.Any<Dictionary<string, object>>())
            .Returns(mockProvider);

        var agent = new SaturnAgent(_logger, _mockSaturnCore, _configuration);
        var agentConfig = new AgentConfiguration
        {
            Id = TestConstants.DefaultAgentId,
            Name = TestConstants.DefaultAgentName,
            Type = AgentType.Saturn,
            Parameters = new Dictionary<string, object>
            {
                { "provider", "OpenRouter" },
                { "model", TestConstants.ValidOpenRouterModel },
                { "api_key", TestConstants.TestApiKey }
            }
        };

        await agent.InitializeAsync(agentConfig);

        // Verify agent is ready
        Assert.Equal(AgentStatus.Ready, agent.Status);

        // Act
        await agent.ShutdownAsync();

        // Assert
        Assert.Equal(AgentStatus.Stopped, agent.Status);
        
        // Verify cleanup was called on Saturn core
        _mockSaturnCore.Received(1).Dispose();
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsCorrectAgentStatus()
    {
        // Arrange
        var mockProvider = Substitute.For<OrchestratorChat.Saturn.Providers.ILLMProvider>();
        mockProvider.IsAvailable.Returns(true);
        
        _mockSaturnCore.CreateProviderAsync("OpenRouter", Arg.Any<Dictionary<string, object>>())
            .Returns(mockProvider);

        var agent = new SaturnAgent(_logger, _mockSaturnCore, _configuration);
        
        // Test uninitialized status
        var uninitializedStatus = await agent.GetStatusAsync();
        Assert.Equal(AgentStatus.Uninitialized, uninitializedStatus.Status);

        // Initialize agent
        var agentConfig = new AgentConfiguration
        {
            Id = TestConstants.DefaultAgentId,
            Name = TestConstants.DefaultAgentName,
            Type = AgentType.Saturn,
            Parameters = new Dictionary<string, object>
            {
                { "provider", "OpenRouter" },
                { "model", TestConstants.ValidOpenRouterModel },
                { "api_key", TestConstants.TestApiKey }
            }
        };

        await agent.InitializeAsync(agentConfig);

        // Act
        var readyStatus = await agent.GetStatusAsync();

        // Assert
        Assert.Equal(AgentStatus.Ready, readyStatus.Status);
        Assert.NotNull(readyStatus.LastActivity);
        Assert.True(readyStatus.IsHealthy);
        Assert.NotNull(readyStatus.Capabilities);
        Assert.Equal(TestConstants.DefaultAgentId, readyStatus.AgentId);
        Assert.True(readyStatus.Capabilities.SupportsToolExecution);
        Assert.True(readyStatus.Capabilities.SupportsStreaming);
    }

    [Fact]
    public async Task InitializeAsync_InvalidProvider_ReturnsError()
    {
        // Arrange
        _mockSaturnCore.CreateProviderAsync("InvalidProvider", Arg.Any<Dictionary<string, object>>())
            .Throws(new InvalidOperationException("Provider not supported"));

        var agent = new SaturnAgent(_logger, _mockSaturnCore, _configuration);
        var agentConfig = new AgentConfiguration
        {
            Id = TestConstants.DefaultAgentId,
            Name = TestConstants.DefaultAgentName,
            Type = AgentType.Saturn,
            Parameters = new Dictionary<string, object>
            {
                { "provider", "InvalidProvider" },
                { "model", "invalid-model" }
            }
        };

        // Act
        var result = await agent.InitializeAsync(agentConfig);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Provider not supported", result.ErrorMessage);
        Assert.Equal(AgentStatus.Error, agent.Status);
    }

    public void Dispose()
    {
        _processHelper?.Dispose();
        _mockSaturnCore?.Dispose();
    }
}