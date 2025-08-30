using OrchestratorChat.Saturn.Core;
using OrchestratorChat.Saturn.Models;
using OrchestratorChat.Saturn.Providers;
using OrchestratorChat.Saturn.Tools;

namespace OrchestratorChat.Saturn.Agents;

/// <summary>
/// Saturn agent implementation
/// </summary>
public class SaturnAgent : ISaturnAgent
{
    private readonly ILLMProvider _provider;
    private readonly IToolRegistry _toolRegistry;
    private readonly List<AgentMessage> _conversationHistory = new();

    public string Id { get; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Saturn Agent";
    public AgentStatus Status { get; private set; } = AgentStatus.Idle;
    
    public SaturnAgentConfiguration Configuration { get; set; } = new();

    public event EventHandler<ToolCallEventArgs>? OnToolCall;
    public event EventHandler<StreamingEventArgs>? OnStreaming;
    public event EventHandler<StatusChangedEventArgs>? OnStatusChanged;

    public SaturnAgent(ILLMProvider provider, IToolRegistry toolRegistry)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
    }

    public Task<IAsyncEnumerable<AgentResponse>> ProcessMessageAsync(
        AgentMessage message, 
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ProcessMessageInternalAsync(message, cancellationToken));
    }

    private async IAsyncEnumerable<AgentResponse> ProcessMessageInternalAsync(
        AgentMessage message,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ChangeStatus(AgentStatus.Processing, "Processing user message");

        // Add user message to conversation history
        _conversationHistory.Add(message);

        // Add system prompt if this is the first message
        var messages = new List<AgentMessage>();
        if (_conversationHistory.Count == 1 && !string.IsNullOrEmpty(Configuration.SystemPrompt))
        {
            messages.Add(new AgentMessage
            {
                Role = MessageRole.System,
                Content = Configuration.SystemPrompt
            });
        }
        messages.AddRange(_conversationHistory);

        // Get streaming response from provider
        var streamResult = _provider.StreamCompletionAsync(
            messages,
            Configuration.Model,
            Configuration.Temperature,
            Configuration.MaxTokens,
            cancellationToken);

        var hasError = false;
        var errorMessage = string.Empty;

        await foreach (var chunk in streamResult.WithCancellation(cancellationToken))
        {
            // Raise streaming event
            OnStreaming?.Invoke(this, new StreamingEventArgs
            {
                Content = chunk,
                IsComplete = false,
                AgentId = Id
            });

            yield return new AgentResponse
            {
                Content = chunk,
                Type = ResponseType.Text,
                IsComplete = false
            };
        }

        if (!hasError)
        {
            // Mark response as complete
            OnStreaming?.Invoke(this, new StreamingEventArgs
            {
                Content = string.Empty,
                IsComplete = true,
                AgentId = Id
            });

            yield return new AgentResponse
            {
                Content = string.Empty,
                Type = ResponseType.Text,
                IsComplete = true
            };

            ChangeStatus(AgentStatus.Idle, "Message processing completed");
        }
        else
        {
            ChangeStatus(AgentStatus.Error, $"Error processing message: {errorMessage}");
            
            yield return new AgentResponse
            {
                Content = $"Error: {errorMessage}",
                Type = ResponseType.Error,
                IsComplete = true
            };
        }
    }

    public async Task<ToolExecutionResult> ExecuteToolAsync(
        ToolCall toolCall, 
        CancellationToken cancellationToken = default)
    {
        ChangeStatus(AgentStatus.ExecutingTool, $"Executing tool: {toolCall.Name}");

        try
        {
            var tool = _toolRegistry.GetTool(toolCall.Name);
            if (tool == null)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    Error = $"Tool not found: {toolCall.Name}"
                };
            }

            // Raise tool call event
            OnToolCall?.Invoke(this, new ToolCallEventArgs
            {
                ToolName = toolCall.Name,
                Parameters = toolCall.Parameters,
                AgentId = Id
            });

            // Execute the tool
            var result = await tool.ExecuteAsync(toolCall, cancellationToken);
            
            ChangeStatus(AgentStatus.Idle, "Tool execution completed");
            return result;
        }
        catch (Exception ex)
        {
            ChangeStatus(AgentStatus.Error, $"Error executing tool: {ex.Message}");
            
            return new ToolExecutionResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task ShutdownAsync()
    {
        ChangeStatus(AgentStatus.Shutdown, "Agent shutting down");
        
        // Clear conversation history
        _conversationHistory.Clear();
        
        // TODO: Cleanup any resources
        
        await Task.CompletedTask;
    }

    private void ChangeStatus(AgentStatus newStatus, string reason)
    {
        var previousStatus = Status;
        Status = newStatus;
        
        OnStatusChanged?.Invoke(this, new StatusChangedEventArgs
        {
            PreviousStatus = previousStatus,
            CurrentStatus = newStatus,
            Reason = reason,
            AgentId = Id
        });
    }

    public async Task InitializeAsync()
    {
        if (!_provider.IsInitialized)
        {
            await _provider.InitializeAsync();
        }
        
        ChangeStatus(AgentStatus.Idle, "Agent initialized");
    }
}