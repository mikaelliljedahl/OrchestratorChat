# Agent System Implementation Plan

## Overview
The agent system is the core execution engine that orchestrates LLM interactions, tool usage, and multi-agent coordination. Currently, OrchestratorChat.Saturn has minimal agent implementation. This document provides a complete plan based on the sophisticated agent system in SaturnFork.

## Current State vs Required State

### Current State (OrchestratorChat.Saturn)
- Basic `SaturnAgent.cs` and `AgentManager.cs` skeletons
- No execution engine
- No configuration management
- No streaming support
- No multi-agent coordination

### Required State (from SaturnFork)
- Complete `AgentBase` execution engine
- Agent configuration and mode system
- Streaming response handling
- Multi-agent task management
- Review and approval workflows
- Context management and state persistence

## Core Agent Components

### 1. AgentBase - The Execution Engine

#### 1.1 AgentBase.cs
**Location**: `src/OrchestratorChat.Saturn/Agents/AgentBase.cs`

**Purpose**: Abstract base class providing core agent functionality

**Implementation**:
```csharp
public abstract class AgentBase : IAgent, IDisposable
{
    protected readonly ILLMClient _llmClient;
    protected readonly ToolRegistry _toolRegistry;
    protected readonly ILogger<AgentBase> _logger;
    protected readonly AgentConfiguration _configuration;
    
    // State management
    protected AgentStatus _status = AgentStatus.Idle;
    protected List<ChatMessage> _conversationHistory = new();
    protected Dictionary<string, object> _context = new();
    protected CancellationTokenSource _cancellationTokenSource;
    
    // Events
    public event EventHandler<AgentOutputEventArgs> OutputReceived;
    public event EventHandler<AgentStatusChangedEventArgs> StatusChanged;
    public event EventHandler<ToolExecutionEventArgs> ToolExecuting;
    public event EventHandler<ToolExecutionEventArgs> ToolExecuted;
    
    // Core methods
    public async Task<AgentResponse> ProcessAsync(string input, AgentRequest request = null)
    {
        try
        {
            UpdateStatus(AgentStatus.Processing);
            
            // Build conversation context
            var messages = BuildMessages(input, request);
            
            // Add available tools
            var tools = GetAvailableTools(request);
            
            // Create LLM request
            var llmRequest = new ChatRequest
            {
                Messages = messages,
                Model = _configuration.Model,
                Temperature = _configuration.Temperature,
                MaxTokens = _configuration.MaxTokens,
                Tools = tools,
                Stream = _configuration.EnableStreaming
            };
            
            // Process with streaming or non-streaming
            if (_configuration.EnableStreaming)
            {
                return await ProcessStreamingAsync(llmRequest);
            }
            else
            {
                return await ProcessNonStreamingAsync(llmRequest);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent processing failed");
            UpdateStatus(AgentStatus.Error);
            throw;
        }
        finally
        {
            UpdateStatus(AgentStatus.Idle);
        }
    }
    
    protected async Task<AgentResponse> ProcessStreamingAsync(ChatRequest request)
    {
        var responseBuilder = new StringBuilder();
        var toolCalls = new List<ToolCall>();
        
        await foreach (var chunk in _llmClient.StreamMessageAsync(request))
        {
            // Handle text chunks
            if (!string.IsNullOrEmpty(chunk.Delta))
            {
                responseBuilder.Append(chunk.Delta);
                OnOutputReceived(new AgentOutputEventArgs
                {
                    Content = chunk.Delta,
                    IsStreaming = true
                });
            }
            
            // Handle tool call chunks
            if (chunk.ToolCall != null)
            {
                toolCalls.Add(chunk.ToolCall);
            }
        }
        
        // Execute any tool calls
        if (toolCalls.Any())
        {
            var toolResults = await ExecuteToolsAsync(toolCalls);
            // Continue conversation with tool results
            return await ProcessToolResultsAsync(toolResults);
        }
        
        return new AgentResponse
        {
            Content = responseBuilder.ToString(),
            Success = true
        };
    }
    
    protected async Task<List<ToolResult>> ExecuteToolsAsync(List<ToolCall> toolCalls)
    {
        var results = new List<ToolResult>();
        
        foreach (var toolCall in toolCalls)
        {
            OnToolExecuting(new ToolExecutionEventArgs
            {
                ToolName = toolCall.Name,
                Parameters = toolCall.Arguments
            });
            
            var tool = _toolRegistry.GetTool(toolCall.Name);
            if (tool == null)
            {
                results.Add(new ToolResult
                {
                    Success = false,
                    Error = $"Tool '{toolCall.Name}' not found"
                });
                continue;
            }
            
            var context = new AgentContext
            {
                AgentId = Id,
                SessionId = _configuration.SessionId,
                WorkingDirectory = _configuration.WorkingDirectory,
                Variables = _context,
                Logger = _logger
            };
            
            var result = await tool.ExecuteAsync(toolCall.Arguments, context);
            results.Add(result);
            
            OnToolExecuted(new ToolExecutionEventArgs
            {
                ToolName = toolCall.Name,
                Parameters = toolCall.Arguments,
                Result = result
            });
        }
        
        return results;
    }
    
    protected List<ChatMessage> BuildMessages(string input, AgentRequest request)
    {
        var messages = new List<ChatMessage>();
        
        // Add system prompt
        if (!string.IsNullOrEmpty(_configuration.SystemPrompt))
        {
            messages.Add(new ChatMessage
            {
                Role = "system",
                Content = _configuration.SystemPrompt
            });
        }
        
        // Add conversation history
        messages.AddRange(_conversationHistory);
        
        // Add current input
        messages.Add(new ChatMessage
        {
            Role = "user",
            Content = input
        });
        
        return messages;
    }
    
    protected void UpdateStatus(AgentStatus newStatus)
    {
        var oldStatus = _status;
        _status = newStatus;
        
        StatusChanged?.Invoke(this, new AgentStatusChangedEventArgs
        {
            OldStatus = oldStatus,
            NewStatus = newStatus
        });
    }
}
```

### 2. Agent Configuration System

#### 2.1 AgentConfiguration.cs
**Location**: `src/OrchestratorChat.Saturn/Agents/Core/AgentConfiguration.cs`

**Purpose**: Comprehensive agent configuration

**Implementation**:
```csharp
public class AgentConfiguration
{
    // Identity
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public string Description { get; set; }
    
    // LLM Settings
    public string Model { get; set; }
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 4096;
    public double TopP { get; set; } = 1.0;
    
    // Behavior
    public AgentMode Mode { get; set; } = AgentMode.Balanced;
    public string SystemPrompt { get; set; }
    public bool EnableStreaming { get; set; } = true;
    public int MaxIterations { get; set; } = 10;
    
    // Tools
    public List<string> EnabledTools { get; set; } = new();
    public bool AutoApproveTools { get; set; } = false;
    
    // Context
    public string WorkingDirectory { get; set; }
    public string SessionId { get; set; }
    public Dictionary<string, object> InitialContext { get; set; } = new();
    
    // Multi-Agent
    public bool CanCreateSubAgents { get; set; } = false;
    public int MaxSubAgents { get; set; } = 5;
    public List<string> AllowedSubAgentModels { get; set; } = new();
    
    // Resource Limits
    public TimeSpan ExecutionTimeout { get; set; } = TimeSpan.FromMinutes(10);
    public long MaxMemoryMB { get; set; } = 1024;
    public int MaxConcurrentTools { get; set; } = 3;
}

public enum AgentMode
{
    Minimal,      // Minimal tool use, prefer direct answers
    Balanced,     // Balance between tools and reasoning
    Comprehensive, // Use all available tools extensively
    Code,         // Optimized for coding tasks
    Research,     // Optimized for research and analysis
    Creative      // Higher temperature, more creative responses
}
```

#### 2.2 Mode System Implementation
**Location**: `src/OrchestratorChat.Saturn/Agents/Core/AgentModes.cs`

**Purpose**: Define behavior patterns for different modes

**Implementation**:
```csharp
public static class AgentModes
{
    public static AgentConfiguration GetModeConfiguration(AgentMode mode)
    {
        return mode switch
        {
            AgentMode.Minimal => new AgentConfiguration
            {
                Model = "anthropic/claude-3.5-haiku", // Faster, cheaper model for simple tasks
                Temperature = 0.3,
                MaxIterations = 3,
                SystemPrompt = "Provide direct, concise answers. Use tools only when absolutely necessary."
            },
            
            AgentMode.Comprehensive => new AgentConfiguration
            {
                Model = "anthropic/claude-opus-4.1", // Most advanced model for complex analysis
                Temperature = 0.5,
                MaxIterations = 20,
                SystemPrompt = "Thoroughly explore the problem using all available tools. Be comprehensive in your analysis."
            },
            
            AgentMode.Code => new AgentConfiguration
            {
                Model = "anthropic/claude-sonnet-4", // Default model, good balance for coding
                Temperature = 0.2,
                MaxIterations = 15,
                SystemPrompt = "You are a coding assistant. Write clean, well-documented code. Use tools to read, write, and test code.",
                EnabledTools = new List<string> 
                { 
                    "read_file", "write_file", "execute_command", 
                    "search_and_replace", "apply_diff", "glob", "grep" 
                }
            },
            
            AgentMode.Research => new AgentConfiguration
            {
                Model = "anthropic/claude-sonnet-4", // Good for analysis and research
                Temperature = 0.4,
                MaxIterations = 25,
                SystemPrompt = "You are a research assistant. Gather information from multiple sources, analyze thoroughly, and provide well-referenced conclusions.",
                EnabledTools = new List<string> 
                { 
                    "web_fetch", "read_file", "grep", "glob", "list_files" 
                }
            },
            
            AgentMode.Creative => new AgentConfiguration
            {
                Model = "anthropic/claude-opus-4.1", // Most capable for creative tasks
                Temperature = 0.9,
                MaxIterations = 10,
                SystemPrompt = "Be creative and think outside the box. Generate novel ideas and unique solutions."
            },
            
            _ => new AgentConfiguration
            {
                Model = "anthropic/claude-sonnet-4" // Default balanced mode uses Sonnet 4
            }
        };
    }
}
```

### 3. Multi-Agent Coordination

#### 3.1 AgentManager.cs
**Location**: `src/OrchestratorChat.Saturn/Agents/AgentManager.cs`

**Purpose**: Manage multiple agent instances

**Implementation**:
```csharp
public class AgentManager : IAgentManager
{
    private readonly Dictionary<string, IAgent> _agents = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AgentManager> _logger;
    private readonly SemaphoreSlim _agentCreationLock = new(1);
    
    public async Task<IAgent> CreateAgentAsync(AgentConfiguration configuration)
    {
        await _agentCreationLock.WaitAsync();
        try
        {
            // Validate configuration
            ValidateConfiguration(configuration);
            
            // Check resource limits
            if (_agents.Count >= configuration.MaxSubAgents)
            {
                throw new InvalidOperationException("Maximum number of agents reached");
            }
            
            // Create agent instance
            var agent = new ManagedAgent(_serviceProvider, configuration);
            await agent.InitializeAsync();
            
            // Register agent
            _agents[agent.Id] = agent;
            
            // Wire up events
            agent.StatusChanged += OnAgentStatusChanged;
            agent.OutputReceived += OnAgentOutputReceived;
            
            _logger.LogInformation("Created agent: {AgentId} - {AgentName}", 
                agent.Id, configuration.Name);
            
            return agent;
        }
        finally
        {
            _agentCreationLock.Release();
        }
    }
    
    public async Task<AgentTaskResult> ExecuteTaskAsync(string agentId, AgentTask task)
    {
        if (!_agents.TryGetValue(agentId, out var agent))
        {
            throw new InvalidOperationException($"Agent {agentId} not found");
        }
        
        var result = new AgentTaskResult
        {
            TaskId = task.Id,
            AgentId = agentId,
            Status = TaskStatus.Running
        };
        
        try
        {
            // Execute task
            var response = await agent.ProcessAsync(task.Input, new AgentRequest
            {
                Context = task.Context,
                Tools = task.RequiredTools,
                MaxIterations = task.MaxIterations
            });
            
            result.Status = TaskStatus.Completed;
            result.Output = response.Content;
            result.Success = response.Success;
        }
        catch (Exception ex)
        {
            result.Status = TaskStatus.Failed;
            result.Error = ex.Message;
            _logger.LogError(ex, "Task execution failed: {TaskId}", task.Id);
        }
        
        return result;
    }
    
    public async Task<bool> TerminateAgentAsync(string agentId, bool force = false)
    {
        if (!_agents.TryGetValue(agentId, out var agent))
        {
            return false;
        }
        
        try
        {
            if (force)
            {
                agent.Cancel();
            }
            else
            {
                await agent.ShutdownAsync();
            }
            
            _agents.Remove(agentId);
            agent.Dispose();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to terminate agent: {AgentId}", agentId);
            return false;
        }
    }
    
    public AgentStatus GetAgentStatus(string agentId)
    {
        return _agents.TryGetValue(agentId, out var agent) 
            ? agent.Status 
            : AgentStatus.NotFound;
    }
    
    public IEnumerable<AgentInfo> GetAllAgents()
    {
        return _agents.Values.Select(agent => new AgentInfo
        {
            Id = agent.Id,
            Name = agent.Configuration.Name,
            Status = agent.Status,
            CreatedAt = agent.CreatedAt,
            LastActivity = agent.LastActivity
        });
    }
}
```

#### 3.2 Multi-Agent Task Objects
**Location**: `src/OrchestratorChat.Saturn/Agents/MultiAgent/Objects/`

```csharp
public class AgentTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public string Input { get; set; }
    public Dictionary<string, object> Context { get; set; }
    public List<string> RequiredTools { get; set; }
    public int MaxIterations { get; set; } = 10;
    public TaskPriority Priority { get; set; } = TaskPriority.Normal;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AgentTaskResult
{
    public string TaskId { get; set; }
    public string AgentId { get; set; }
    public TaskStatus Status { get; set; }
    public string Output { get; set; }
    public string Error { get; set; }
    public bool Success { get; set; }
    public DateTime CompletedAt { get; set; }
    public TimeSpan Duration { get; set; }
}

public enum TaskStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}

public enum TaskPriority
{
    Low,
    Normal,
    High,
    Critical
}

public class SubAgentContext
{
    public string ParentAgentId { get; set; }
    public string TaskId { get; set; }
    public Dictionary<string, object> SharedContext { get; set; }
    public List<string> InheritedTools { get; set; }
    public int MaxDepth { get; set; } = 3;
    public int CurrentDepth { get; set; }
}
```

### 4. Streaming and Event System

#### 4.1 Streaming Support
**Location**: `src/OrchestratorChat.Saturn/Agents/Core/StreamingSupport.cs`

```csharp
public class StreamingToolCall
{
    public string Id { get; set; }
    public string Name { get; set; }
    public StringBuilder ArgumentsBuilder { get; set; } = new();
    public bool IsComplete { get; set; }
    
    public ToolCall ToToolCall()
    {
        return new ToolCall
        {
            Id = Id,
            Name = Name,
            Arguments = ArgumentsBuilder.ToString()
        };
    }
}

public class StreamingResponseHandler
{
    private readonly StringBuilder _contentBuilder = new();
    private readonly Dictionary<string, StreamingToolCall> _toolCalls = new();
    
    public event EventHandler<string> ContentReceived;
    public event EventHandler<ToolCall> ToolCallCompleted;
    
    public void ProcessChunk(StreamChunk chunk)
    {
        // Handle content chunks
        if (!string.IsNullOrEmpty(chunk.Delta))
        {
            _contentBuilder.Append(chunk.Delta);
            ContentReceived?.Invoke(this, chunk.Delta);
        }
        
        // Handle tool call chunks
        if (chunk.ToolCall != null)
        {
            if (!_toolCalls.ContainsKey(chunk.ToolCall.Id))
            {
                _toolCalls[chunk.ToolCall.Id] = new StreamingToolCall
                {
                    Id = chunk.ToolCall.Id,
                    Name = chunk.ToolCall.Name
                };
            }
            
            var streamingCall = _toolCalls[chunk.ToolCall.Id];
            streamingCall.ArgumentsBuilder.Append(chunk.ToolCall.Arguments);
            
            if (chunk.IsComplete)
            {
                streamingCall.IsComplete = true;
                ToolCallCompleted?.Invoke(this, streamingCall.ToToolCall());
            }
        }
    }
    
    public string GetCompleteContent() => _contentBuilder.ToString();
    
    public List<ToolCall> GetCompletedToolCalls() => 
        _toolCalls.Values
            .Where(tc => tc.IsComplete)
            .Select(tc => tc.ToToolCall())
            .ToList();
}
```

#### 4.2 Event Arguments
**Location**: `src/OrchestratorChat.Saturn/Agents/Core/Events/`

```csharp
public class AgentOutputEventArgs : EventArgs
{
    public string Content { get; set; }
    public bool IsStreaming { get; set; }
    public OutputType Type { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public enum OutputType
{
    Text,
    ToolCall,
    ToolResult,
    Error,
    Status
}

public class AgentStatusChangedEventArgs : EventArgs
{
    public AgentStatus OldStatus { get; set; }
    public AgentStatus NewStatus { get; set; }
    public string Reason { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ToolExecutionEventArgs : EventArgs
{
    public string ToolName { get; set; }
    public string Parameters { get; set; }
    public ToolResult Result { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

### 5. Review and Validation System

#### 5.1 ReviewerContext
**Location**: `src/OrchestratorChat.Saturn/Agents/MultiAgent/Objects/ReviewerContext.cs`

```csharp
public class ReviewerContext
{
    public string TaskDescription { get; set; }
    public string AgentOutput { get; set; }
    public List<ToolExecution> ToolExecutions { get; set; }
    public Dictionary<string, object> Metrics { get; set; }
    
    public class ToolExecution
    {
        public string ToolName { get; set; }
        public string Input { get; set; }
        public string Output { get; set; }
        public bool Success { get; set; }
    }
}

public class ReviewDecision
{
    public bool Approved { get; set; }
    public string Feedback { get; set; }
    public List<string> RequiredChanges { get; set; }
    public ReviewSeverity Severity { get; set; }
}

public enum ReviewSeverity
{
    Info,
    Warning,
    Error,
    Critical
}
```

## Implementation Priority

### Phase 1: Core Agent Infrastructure (Week 1)
1. Implement AgentBase abstract class
2. Create AgentConfiguration system
3. Add basic message processing
4. Implement status management

### Phase 2: Execution Engine (Week 2)
1. Add streaming support
2. Implement tool execution pipeline
3. Create event system
4. Add context management

### Phase 3: Multi-Agent System (Week 3)
1. Implement AgentManager
2. Add task management
3. Create sub-agent coordination
4. Implement resource limits

### Phase 4: Advanced Features (Week 4)
1. Add review system
2. Implement mode system
3. Add persistence support
4. Create comprehensive tests

## Testing Requirements

### Unit Tests
- Agent configuration validation
- Message building logic
- Tool execution pipeline
- Event firing and handling

### Integration Tests
- End-to-end agent execution
- Multi-agent coordination
- Streaming response handling
- Resource limit enforcement

## Performance Considerations

1. **Memory Management**:
   - Limit conversation history size
   - Implement context pruning
   - Monitor memory usage
   - Clean up completed agents

2. **Concurrency**:
   - Thread-safe agent operations
   - Concurrent tool execution
   - Async/await throughout
   - Proper cancellation support

3. **Streaming**:
   - Efficient buffer management
   - Backpressure handling
   - Chunk aggregation
   - Error recovery

## Dependencies to Add

```xml
<PackageReference Include="Microsoft.Extensions.ObjectPool" Version="8.0.0" />
<PackageReference Include="System.Threading.Channels" Version="8.0.0" />
<PackageReference Include="Polly" Version="8.0.0" />
```

## Migration Notes

When porting from SaturnFork:
1. Remove Terminal.Gui dependencies
2. Replace console output with events
3. Adapt for web-based execution
4. Add proper DI integration
5. Implement cancellation tokens

## Validation Checklist

- [ ] AgentBase fully implemented
- [ ] Configuration system complete
- [ ] Streaming support working
- [ ] Multi-agent coordination functional
- [ ] Event system operational
- [ ] Review system implemented
- [ ] Mode system working
- [ ] Resource limits enforced
- [ ] Tests passing
- [ ] Performance optimized