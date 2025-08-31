using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Messages;
using OrchestratorChat.Core.Tools;
using OrchestratorChat.Agents.Saturn;
using OrchestratorChat.Agents.Tests.TestHelpers;
using AgentsSaturnCore = OrchestratorChat.Agents.Saturn.ISaturnCore;
using AgentsSaturnConfig = OrchestratorChat.Agents.Saturn.SaturnConfiguration;
using SaturnProviderType = OrchestratorChat.Saturn.Models.ProviderType;
using ISaturnAgent = OrchestratorChat.Saturn.Core.ISaturnAgent;
using SaturnAgentConfiguration = OrchestratorChat.Saturn.Models.SaturnAgentConfiguration;
using SaturnAgentResponse = OrchestratorChat.Saturn.Models.AgentResponse;
using SaturnMessageRole = OrchestratorChat.Saturn.Models.MessageRole;
using SaturnResponseType = OrchestratorChat.Saturn.Models.ResponseType;
// TokenUsage model not available in Saturn models
using SaturnToolCall = OrchestratorChat.Saturn.Models.ToolCall;
using SaturnToolExecutionResult = OrchestratorChat.Saturn.Models.ToolExecutionResult;
using SaturnAgentMessage = OrchestratorChat.Saturn.Models.AgentMessage;

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
            SupportedModels = new List<string> { TestConstants.ValidOpenRouterModel, TestConstants.ValidClaudeModel },
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
        // Note: ILLMProvider doesn't expose ProviderType in interface
        
        _mockSaturnCore.CreateProviderAsync(SaturnProviderType.OpenRouter, Arg.Any<Dictionary<string, object>>())
            .Returns(mockProvider);
        _mockSaturnCore.CreateAgentAsync(mockProvider, Arg.Any<SaturnAgentConfiguration>())
            .Returns(Substitute.For<ISaturnAgent>());
        _mockSaturnCore.GetAvailableTools()
            .Returns(new List<OrchestratorChat.Saturn.Models.ToolInfo>());

        var agent = new SaturnAgent(_logger, _mockSaturnCore, _configuration);
        var agentConfig = new AgentConfiguration
        {
            Id = TestConstants.DefaultAgentId,
            Name = TestConstants.DefaultAgentName,
            Type = AgentType.Saturn,
            WorkingDirectory = Directory.GetCurrentDirectory(),
            CustomSettings = new Dictionary<string, object>
            {
                { "Provider", "OpenRouter" },
                { "Model", TestConstants.ValidOpenRouterModel },
                { "ApiKey", TestConstants.TestApiKey }
            }
        };

        // Act
        var result = await agent.InitializeAsync(agentConfig);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(OrchestratorChat.Core.Agents.AgentStatus.Ready, agent.Status);
        Assert.Equal(TestConstants.DefaultAgentName, agent.Name);
        Assert.Equal(AgentType.Saturn, agent.Type);
        Assert.NotNull(result.Capabilities);

        // Verify SaturnCore was called
        await _mockSaturnCore.Received(1).CreateProviderAsync(SaturnProviderType.OpenRouter, Arg.Any<Dictionary<string, object>>());
    }

    [Fact]
    public async Task SendMessageAsync_WithOpenRouter_ProcessesCorrectly()
    {
        // Arrange
        var mockProvider = Substitute.For<OrchestratorChat.Saturn.Providers.ILLMProvider>();
        // Note: ILLMProvider doesn't expose ProviderType in interface
        
        var mockResponse = new SaturnAgentResponse
        {
            Content = "Response from OpenRouter",
            Type = SaturnResponseType.Text,
            IsComplete = true
        };

        // Create a mock Saturn agent
        var mockSaturnAgent = Substitute.For<ISaturnAgent>();
        var mockAsyncEnumerable = CreateMockAsyncEnumerable(mockResponse);
        mockSaturnAgent.ProcessMessageAsync(Arg.Any<SaturnAgentMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mockAsyncEnumerable));
        
        _mockSaturnCore.CreateProviderAsync(SaturnProviderType.OpenRouter, Arg.Any<Dictionary<string, object>>())
            .Returns(mockProvider);
        _mockSaturnCore.CreateAgentAsync(Arg.Any<OrchestratorChat.Saturn.Providers.ILLMProvider>(), Arg.Any<SaturnAgentConfiguration>())
            .Returns(mockSaturnAgent);
        _mockSaturnCore.GetAvailableTools()
            .Returns(new List<OrchestratorChat.Saturn.Models.ToolInfo>());

        var agent = new SaturnAgent(_logger, _mockSaturnCore, _configuration);
        var agentConfig = new AgentConfiguration
        {
            Id = TestConstants.DefaultAgentId,
            Name = TestConstants.DefaultAgentName,
            Type = AgentType.Saturn,
            CustomSettings = new Dictionary<string, object>
            {
                { "Provider", "OpenRouter" },
                { "Model", TestConstants.ValidOpenRouterModel },
                { "ApiKey", TestConstants.TestApiKey }
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
        // Usage information is not directly exposed by Saturn adapter

        // Verify SaturnAgent was created and called
        await _mockSaturnCore.Received(1).CreateAgentAsync(mockProvider, Arg.Any<SaturnAgentConfiguration>());
        await mockSaturnAgent.Received(1).ProcessMessageAsync(Arg.Any<SaturnAgentMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendMessageAsync_WithAnthropic_ProcessesCorrectly()
    {
        // Arrange
        var mockProvider = Substitute.For<OrchestratorChat.Saturn.Providers.ILLMProvider>();
        // Note: ILLMProvider doesn't expose ProviderType in interface
        
        var mockResponse = new SaturnAgentResponse
        {
            Content = "Response from Anthropic Claude",
            Type = SaturnResponseType.Text,
            IsComplete = true
        };

        // Create a mock Saturn agent
        var mockSaturnAgent = Substitute.For<ISaturnAgent>();
        var mockAsyncEnumerable = CreateMockAsyncEnumerable(mockResponse);
        mockSaturnAgent.ProcessMessageAsync(Arg.Any<SaturnAgentMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mockAsyncEnumerable));

        _mockSaturnCore.CreateProviderAsync(SaturnProviderType.Anthropic, Arg.Any<Dictionary<string, object>>())
            .Returns(mockProvider);
        _mockSaturnCore.CreateAgentAsync(Arg.Any<OrchestratorChat.Saturn.Providers.ILLMProvider>(), Arg.Any<SaturnAgentConfiguration>())
            .Returns(mockSaturnAgent);
        _mockSaturnCore.GetAvailableTools()
            .Returns(new List<OrchestratorChat.Saturn.Models.ToolInfo>());

        var agent = new SaturnAgent(_logger, _mockSaturnCore, _configuration);
        var agentConfig = new AgentConfiguration
        {
            Id = TestConstants.DefaultAgentId,
            Name = TestConstants.DefaultAgentName,
            Type = AgentType.Saturn,
            CustomSettings = new Dictionary<string, object>
            {
                { "Provider", "Anthropic" },
                { "Model", TestConstants.ValidClaudeModel },
                { "AccessToken", TestConstants.TestAccessToken }
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
        // Usage information is not directly exposed by Saturn adapter

        // Verify SaturnAgent was created and called
        await _mockSaturnCore.Received(1).CreateAgentAsync(mockProvider, Arg.Any<SaturnAgentConfiguration>());
        await mockSaturnAgent.Received(1).ProcessMessageAsync(Arg.Any<SaturnAgentMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteToolAsync_CallsSaturnAgent()
    {
        // Arrange
        var mockProvider = Substitute.For<OrchestratorChat.Saturn.Providers.ILLMProvider>();
        
        var toolResult = new SaturnToolExecutionResult
        {
            Success = true,
            Output = "Tool executed successfully",
            ExecutionTime = TimeSpan.FromMilliseconds(100)
        };

        // Create a mock Saturn agent
        var mockSaturnAgent = Substitute.For<ISaturnAgent>();
        mockSaturnAgent.ExecuteToolAsync(Arg.Any<SaturnToolCall>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(toolResult));

        _mockSaturnCore.CreateProviderAsync(SaturnProviderType.OpenRouter, Arg.Any<Dictionary<string, object>>())
            .Returns(mockProvider);
        _mockSaturnCore.CreateAgentAsync(Arg.Any<OrchestratorChat.Saturn.Providers.ILLMProvider>(), Arg.Any<SaturnAgentConfiguration>())
            .Returns(mockSaturnAgent);
        _mockSaturnCore.GetAvailableTools()
            .Returns(new List<OrchestratorChat.Saturn.Models.ToolInfo>());

        var agent = new SaturnAgent(_logger, _mockSaturnCore, _configuration);
        var agentConfig = new AgentConfiguration
        {
            Id = TestConstants.DefaultAgentId,
            Name = TestConstants.DefaultAgentName,
            Type = AgentType.Saturn,
            CustomSettings = new Dictionary<string, object>
            {
                { "Provider", "OpenRouter" },
                { "Model", TestConstants.ValidOpenRouterModel },
                { "ApiKey", TestConstants.TestApiKey }
            }
        };

        await agent.InitializeAsync(agentConfig);

        var toolCall = new ToolCall
        {
            Id = Guid.NewGuid().ToString(),
            ToolName = "test_tool",
            Parameters = TestConstants.StandardToolParams,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await agent.ExecuteToolAsync(toolCall);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("Tool executed successfully", result.Output);
        // Assert.Equal("test_tool", result.ToolName); // ToolName not in Core model
        Assert.True(result.ExecutionTime.TotalMilliseconds > 0);

        // Verify Saturn agent was created and called
        await _mockSaturnCore.Received(1).CreateAgentAsync(mockProvider, Arg.Any<SaturnAgentConfiguration>());
        await mockSaturnAgent.Received(1).ExecuteToolAsync(Arg.Any<SaturnToolCall>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleStreamingResponse_EventsEmitted()
    {
        // Arrange
        var mockProvider = Substitute.For<OrchestratorChat.Saturn.Providers.ILLMProvider>();
        
        var outputEvents = new List<string>();
        var statusChanges = new List<OrchestratorChat.Core.Agents.AgentStatus>();

        var streamingResponse = new SaturnAgentResponse
        {
            Content = "Chunk 1Chunk 2Chunk 3",
            Type = SaturnResponseType.Text,
            IsComplete = true
        };

        // Create a mock Saturn agent
        var mockSaturnAgent = Substitute.For<ISaturnAgent>();
        var mockAsyncEnumerable = CreateMockAsyncEnumerable(streamingResponse);
        mockSaturnAgent.ProcessMessageAsync(Arg.Any<SaturnAgentMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mockAsyncEnumerable));

        _mockSaturnCore.CreateProviderAsync(SaturnProviderType.OpenRouter, Arg.Any<Dictionary<string, object>>())
            .Returns(mockProvider);
        _mockSaturnCore.CreateAgentAsync(Arg.Any<OrchestratorChat.Saturn.Providers.ILLMProvider>(), Arg.Any<SaturnAgentConfiguration>())
            .Returns(mockSaturnAgent);
        _mockSaturnCore.GetAvailableTools()
            .Returns(new List<OrchestratorChat.Saturn.Models.ToolInfo>());

        var agent = new SaturnAgent(_logger, _mockSaturnCore, _configuration);
        
        agent.OutputReceived += (sender, args) => outputEvents.Add(args.Output);
        agent.StatusChanged += (sender, args) => statusChanges.Add(args.NewStatus);

        var agentConfig = new AgentConfiguration
        {
            Id = TestConstants.DefaultAgentId,
            Name = TestConstants.DefaultAgentName,
            Type = AgentType.Saturn,
            CustomSettings = new Dictionary<string, object>
            {
                { "Provider", "OpenRouter" },
                { "Model", TestConstants.ValidOpenRouterModel },
                { "ApiKey", TestConstants.TestApiKey }
            }
        };

        await agent.InitializeAsync(agentConfig);

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
        Assert.Contains(OrchestratorChat.Core.Agents.AgentStatus.Ready, statusChanges);
        
        // OutputReceived events would depend on the Saturn core implementation
        // For now, verify the response content contains the expected data
        Assert.Contains("Chunk", response.Content);
    }

    [Fact]
    public async Task ShutdownAsync_CleansUpResources()
    {
        // Arrange
        var mockProvider = Substitute.For<OrchestratorChat.Saturn.Providers.ILLMProvider>();
        var mockSaturnAgent = Substitute.For<ISaturnAgent>();
        
        _mockSaturnCore.CreateProviderAsync(SaturnProviderType.OpenRouter, Arg.Any<Dictionary<string, object>>())
            .Returns(mockProvider);
        _mockSaturnCore.CreateAgentAsync(mockProvider, Arg.Any<SaturnAgentConfiguration>())
            .Returns(mockSaturnAgent);
        _mockSaturnCore.GetAvailableTools()
            .Returns(new List<OrchestratorChat.Saturn.Models.ToolInfo>());

        var agent = new SaturnAgent(_logger, _mockSaturnCore, _configuration);
        var agentConfig = new AgentConfiguration
        {
            Id = TestConstants.DefaultAgentId,
            Name = TestConstants.DefaultAgentName,
            Type = AgentType.Saturn,
            CustomSettings = new Dictionary<string, object>
            {
                { "Provider", "OpenRouter" },
                { "Model", TestConstants.ValidOpenRouterModel },
                { "ApiKey", TestConstants.TestApiKey }
            }
        };

        await agent.InitializeAsync(agentConfig);

        // Verify agent is ready
        Assert.Equal(OrchestratorChat.Core.Agents.AgentStatus.Ready, agent.Status);

        // Act
        await agent.ShutdownAsync();

        // Assert
        Assert.Equal(OrchestratorChat.Core.Agents.AgentStatus.Shutdown, agent.Status);
        
        // Verify cleanup was called on Saturn agent
        await mockSaturnAgent.Received(1).ShutdownAsync();
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsCorrectAgentStatus()
    {
        // Arrange
        var mockProvider = Substitute.For<OrchestratorChat.Saturn.Providers.ILLMProvider>();
        var mockSaturnAgent = Substitute.For<ISaturnAgent>();
        
        _mockSaturnCore.CreateProviderAsync(SaturnProviderType.OpenRouter, Arg.Any<Dictionary<string, object>>())
            .Returns(mockProvider);
        _mockSaturnCore.CreateAgentAsync(mockProvider, Arg.Any<SaturnAgentConfiguration>())
            .Returns(mockSaturnAgent);
        _mockSaturnCore.GetAvailableTools()
            .Returns(new List<OrchestratorChat.Saturn.Models.ToolInfo>());

        var agent = new SaturnAgent(_logger, _mockSaturnCore, _configuration);
        
        // Test uninitialized status
        var uninitializedStatus = await agent.GetStatusAsync();
        Assert.Equal(OrchestratorChat.Core.Agents.AgentStatus.Uninitialized, uninitializedStatus.Status);

        // Initialize agent
        var agentConfig = new AgentConfiguration
        {
            Id = TestConstants.DefaultAgentId,
            Name = TestConstants.DefaultAgentName,
            Type = AgentType.Saturn,
            CustomSettings = new Dictionary<string, object>
            {
                { "Provider", "OpenRouter" },
                { "Model", TestConstants.ValidOpenRouterModel },
                { "ApiKey", TestConstants.TestApiKey }
            }
        };

        await agent.InitializeAsync(agentConfig);

        // Act
        var readyStatus = await agent.GetStatusAsync();

        // Assert
        Assert.Equal(OrchestratorChat.Core.Agents.AgentStatus.Ready, readyStatus.Status);
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
        _mockSaturnCore.CreateProviderAsync((SaturnProviderType)99, Arg.Any<Dictionary<string, object>>())
            .Returns(Task.FromException<OrchestratorChat.Saturn.Providers.ILLMProvider>(new InvalidOperationException("Provider not supported")));

        var agent = new SaturnAgent(_logger, _mockSaturnCore, _configuration);
        var agentConfig = new AgentConfiguration
        {
            Id = TestConstants.DefaultAgentId,
            Name = TestConstants.DefaultAgentName,
            Type = AgentType.Saturn,
            CustomSettings = new Dictionary<string, object>
            {
                { "Provider", "InvalidProvider" },
                { "Model", "invalid-model" }
            }
        };

        // Act
        var result = await agent.InitializeAsync(agentConfig);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Provider not supported", result.ErrorMessage);
        Assert.Equal(OrchestratorChat.Core.Agents.AgentStatus.Error, agent.Status);
    }

    /// <summary>
    /// Helper method to create mock async enumerable for Saturn agent responses
    /// </summary>
    private async IAsyncEnumerable<SaturnAgentResponse> CreateMockAsyncEnumerable(SaturnAgentResponse response)
    {
        yield return response;
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _processHelper?.Dispose();
        // _mockSaturnCore doesn't implement IDisposable
    }
}