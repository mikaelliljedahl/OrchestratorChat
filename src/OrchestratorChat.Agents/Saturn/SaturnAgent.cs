using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrchestratorChat.Core.Agents;
using CoreAgentMessage = OrchestratorChat.Core.Messages.AgentMessage;
using CoreAgentResponse = OrchestratorChat.Core.Messages.AgentResponse;
using CoreToolCall = OrchestratorChat.Core.Tools.ToolCall;
using CoreToolExecutionResult = OrchestratorChat.Core.Tools.ToolExecutionResult;
using CoreMessageRole = OrchestratorChat.Core.Messages.MessageRole;
using CoreResponseType = OrchestratorChat.Core.Messages.ResponseType;
using CoreAgentStatus = OrchestratorChat.Core.Agents.AgentStatus;
using OrchestratorChat.Core.Tools;
using OrchestratorChat.Saturn.Core;
using SaturnAgentConfiguration = OrchestratorChat.Saturn.Models.SaturnAgentConfiguration;
using SaturnAgentMessage = OrchestratorChat.Saturn.Models.AgentMessage;
using SaturnAgentResponse = OrchestratorChat.Saturn.Models.AgentResponse;
using SaturnToolCall = OrchestratorChat.Saturn.Models.ToolCall;
using SaturnToolExecutionResult = OrchestratorChat.Saturn.Models.ToolExecutionResult;
using SaturnMessageRole = OrchestratorChat.Saturn.Models.MessageRole;
using SaturnResponseType = OrchestratorChat.Saturn.Models.ResponseType;
using SaturnAgentStatus = OrchestratorChat.Saturn.Models.AgentStatus;
using SaturnToolInfo = OrchestratorChat.Saturn.Models.ToolInfo;
using SaturnToolParameter = OrchestratorChat.Saturn.Models.ToolParameter;
using SaturnILLMProvider = OrchestratorChat.Saturn.Providers.ILLMProvider;
using SaturnProviderType = OrchestratorChat.Saturn.Models.ProviderType;
using CoreTokenUsage = OrchestratorChat.Core.Messages.TokenUsage;

namespace OrchestratorChat.Agents.Saturn;

public class SaturnAgent : IAgent
{
    private readonly ILogger<SaturnAgent> _logger;
    private readonly ISaturnCore _saturnCore;
    private readonly SaturnConfiguration _configuration;
    private CoreAgentStatus _status = CoreAgentStatus.Uninitialized;
    private SaturnILLMProvider? _llmProvider;
    private ISaturnAgent? _internalAgent;

    public string Id { get; private set; }
    public string Name { get; set; } = string.Empty;
    public AgentType Type => AgentType.Saturn;
    public CoreAgentStatus Status => _status;
    public AgentCapabilities Capabilities { get; private set; } = new();
    public string WorkingDirectory { get; set; } = string.Empty;

    public event EventHandler<AgentStatusChangedEventArgs>? StatusChanged;
    public event EventHandler<AgentOutputEventArgs>? OutputReceived;

    public SaturnAgent(
        ILogger<SaturnAgent> logger,
        ISaturnCore saturnCore,
        IOptions<SaturnConfiguration> configuration)
    {
        _logger = logger;
        _saturnCore = saturnCore;
        _configuration = configuration.Value;
        Id = Guid.NewGuid().ToString();
    }

    public async Task<AgentInitializationResult> InitializeAsync(
        AgentConfiguration configuration)
    {
        try
        {
            SetStatus(CoreAgentStatus.Initializing);

            // Initialize Saturn LLM provider
            _llmProvider = await CreateLLMProviderAsync(configuration);

            // Create Saturn agent instance
            var saturnConfig = new SaturnAgentConfiguration
            {
                Model = configuration.CustomSettings.GetValueOrDefault("Model", "claude-sonnet-4-20250514")?.ToString() ?? "claude-sonnet-4-20250514",
                Temperature = double.Parse(configuration.CustomSettings.GetValueOrDefault("Temperature", "0.7")?.ToString() ?? "0.7"),
                MaxTokens = int.Parse(configuration.CustomSettings.GetValueOrDefault("MaxTokens", "4096")?.ToString() ?? "4096"),
                SystemPrompt = configuration.CustomSettings.GetValueOrDefault("SystemPrompt", "")?.ToString() ?? "",
                EnableTools = bool.Parse(configuration.CustomSettings.GetValueOrDefault("EnableTools", "true")?.ToString() ?? "true"),
                RequireApproval = bool.Parse(configuration.CustomSettings.GetValueOrDefault("RequireApproval", "true")?.ToString() ?? "true")
            };
            
            _internalAgent = await _saturnCore.CreateAgentAsync(_llmProvider, saturnConfig);

            // Hook up Saturn events
            _internalAgent.OnToolCall += HandleSaturnToolCall;
            _internalAgent.OnStreaming += HandleSaturnStreaming;
            _internalAgent.OnStatusChanged += HandleSaturnStatusChanged;

            // Get capabilities from Saturn
            Capabilities = GetSaturnCapabilities();

            SetStatus(CoreAgentStatus.Ready);

            return new AgentInitializationResult
            {
                Success = true,
                Capabilities = Capabilities,
                InitializationTime = TimeSpan.FromSeconds(1)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Saturn agent");
            SetStatus(CoreAgentStatus.Error);
            return new AgentInitializationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<SaturnILLMProvider> CreateLLMProviderAsync(AgentConfiguration config)
    {
        // Use Saturn's provider factory
        var providerType = config.CustomSettings.GetValueOrDefault("Provider", "OpenRouter")?.ToString() ?? "OpenRouter";
        var saturnProviderType = providerType switch
        {
            "Anthropic" => SaturnProviderType.Anthropic,
            _ => SaturnProviderType.OpenRouter
        };
        
        return await _saturnCore.CreateProviderAsync(saturnProviderType, config.CustomSettings);
    }

    public async Task<IAsyncEnumerable<CoreAgentResponse>> SendMessageAsync(
        CoreAgentMessage message,
        CancellationToken cancellationToken = default)
    {
        SetStatus(CoreAgentStatus.Busy);
        try
        {
            if (_internalAgent == null)
                throw new InvalidOperationException("Saturn agent not initialized");

            // Convert to Saturn message format
            var saturnMessage = ConvertToSaturnMessage(message);

            // Send through Saturn agent  
            var responseStream = await _internalAgent.ProcessMessageAsync(
                saturnMessage,
                cancellationToken);

            // Stream responses
            return StreamSaturnResponsesAsync(responseStream, message.Id, cancellationToken);
        }
        finally
        {
            SetStatus(CoreAgentStatus.Ready);
        }
    }

    private async IAsyncEnumerable<CoreAgentResponse> StreamSaturnResponsesAsync(
        IAsyncEnumerable<SaturnAgentResponse> saturnStream,
        string messageId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var saturnResponse in saturnStream.WithCancellation(cancellationToken))
        {
            yield return new CoreAgentResponse
            {
                MessageId = messageId,
                Content = saturnResponse.Content,
                Type = MapResponseType(saturnResponse.Type),
                IsComplete = saturnResponse.IsComplete,
                ToolCalls = saturnResponse.ToolCalls?.Select(MapToolCall).ToList(),
                Usage = new CoreTokenUsage
                {
                    InputTokens = 0, // Saturn models don't expose usage in this way
                    OutputTokens = 0,
                    TotalTokens = 0
                }
            };
        }
    }

    public async Task<CoreToolExecutionResult> ExecuteToolAsync(
        CoreToolCall toolCall,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_internalAgent == null)
                throw new InvalidOperationException("Saturn agent not initialized");

            // Use Saturn's tool execution
            var saturnToolCall = ConvertToSaturnToolCall(toolCall);
            var result = await _internalAgent.ExecuteToolAsync(saturnToolCall, cancellationToken);

            return new CoreToolExecutionResult
            {
                Success = result.Success,
                Output = result.Output,
                Error = result.Error,
                ExecutionTime = result.ExecutionTime
            };
        }
        catch (Exception ex)
        {
            return new CoreToolExecutionResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task ShutdownAsync()
    {
        SetStatus(CoreAgentStatus.Shutdown);

        if (_internalAgent != null)
        {
            await _internalAgent.ShutdownAsync();
            _internalAgent = null;
        }

        // Saturn providers don't implement IDisposable
        _llmProvider = null;
    }

    private AgentCapabilities GetSaturnCapabilities()
    {
        var tools = _saturnCore.GetAvailableTools();

        return new AgentCapabilities
        {
            SupportsStreaming = true,
            SupportsTools = true,
            SupportsFileOperations = true,
            SupportsWebSearch = false,
            SupportedModels = _configuration.SupportedModels,
            AvailableTools = tools.Select(t => new ToolDefinition
            {
                Name = t.Name,
                Description = t.Description,
                Schema = ConvertToolSchema(t.Parameters)
            }).ToList(),
            MaxTokens = 100000,
            MaxConcurrentRequests = 1
        };
    }

    private void SetStatus(CoreAgentStatus status)
    {
        var oldStatus = _status;
        _status = status;

        if (oldStatus != status)
        {
            StatusChanged?.Invoke(this, new AgentStatusChangedEventArgs
            {
                AgentId = Id,
                OldStatus = oldStatus,
                NewStatus = status
            });
        }
    }

    private void HandleSaturnToolCall(object? sender, OrchestratorChat.Saturn.Models.ToolCallEventArgs e)
    {
        // Handle Saturn tool call events
        OutputReceived?.Invoke(this, new AgentOutputEventArgs
        {
            AgentId = Id,
            Content = $"Tool call: {e.ToolName}"
        });
    }

    private void HandleSaturnStreaming(object? sender, OrchestratorChat.Saturn.Models.StreamingEventArgs e)
    {
        // Handle Saturn streaming events
        OutputReceived?.Invoke(this, new AgentOutputEventArgs
        {
            AgentId = Id,
            Content = e.Content
        });
    }
    
    private void HandleSaturnStatusChanged(object? sender, OrchestratorChat.Saturn.Models.StatusChangedEventArgs e)
    {
        // Map Saturn status to our status
        var newStatus = e.CurrentStatus switch
        {
            SaturnAgentStatus.Idle => CoreAgentStatus.Ready,
            SaturnAgentStatus.Processing => CoreAgentStatus.Busy,
            SaturnAgentStatus.ExecutingTool => CoreAgentStatus.Busy,
            SaturnAgentStatus.Error => CoreAgentStatus.Error,
            SaturnAgentStatus.Shutdown => CoreAgentStatus.Shutdown,
            _ => CoreAgentStatus.Ready
        };
        
        SetStatus(newStatus);
    }

    private SaturnAgentMessage ConvertToSaturnMessage(CoreAgentMessage message)
    {
        return new SaturnAgentMessage
        {
            Content = message.Content,
            Role = MapMessageRole(message.Role),
            SessionId = message.SessionId ?? Id,
            Metadata = message.Metadata ?? new Dictionary<string, object>(),
            Timestamp = message.Timestamp
        };
    }

    private SaturnMessageRole MapMessageRole(CoreMessageRole role)
    {
        return role switch
        {
            CoreMessageRole.User => SaturnMessageRole.User,
            CoreMessageRole.Assistant => SaturnMessageRole.Assistant,
            CoreMessageRole.System => SaturnMessageRole.System,
            CoreMessageRole.Tool => SaturnMessageRole.Tool,
            _ => SaturnMessageRole.User
        };
    }

    private CoreResponseType MapResponseType(SaturnResponseType type)
    {
        return type switch
        {
            SaturnResponseType.Text => CoreResponseType.Text,
            SaturnResponseType.ToolCall => CoreResponseType.ToolCall,
            SaturnResponseType.Error => CoreResponseType.Error,
            _ => CoreResponseType.Text
        };
    }

    private CoreToolCall MapToolCall(SaturnToolCall toolCall)
    {
        return new CoreToolCall
        {
            Id = toolCall.Id,
            ToolName = toolCall.Name,
            Parameters = toolCall.Parameters
        };
    }

    private SaturnToolCall ConvertToSaturnToolCall(CoreToolCall toolCall)
    {
        return new SaturnToolCall
        {
            Id = toolCall.Id,
            Name = toolCall.ToolName,
            Parameters = toolCall.Parameters,
            Command = toolCall.Parameters.GetValueOrDefault("command", "")?.ToString() ?? ""
        };
    }

    private ToolSchema ConvertToolSchema(List<SaturnToolParameter> parameters)
    {
        var properties = new Dictionary<string, ParameterSchema>();
        
        foreach (var param in parameters)
        {
            properties[param.Name] = new ParameterSchema
            {
                Type = param.Type,
                Description = param.Description
                // Saturn's ToolParameter.Required would need to be handled differently
                // For now, we skip this since ParameterSchema doesn't have Required
            };
        }
        
        return new ToolSchema
        {
            Type = "object",
            Properties = properties
        };
    }
}