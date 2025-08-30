# Core Abstractions Specification

## Overview
This document defines the core interfaces, models, and contracts that form the foundation of OrchestratorChat. These abstractions ensure loose coupling between components and enable parallel development.

## Project: OrchestratorChat.Core

### Agent Abstractions

#### IAgent Interface
```csharp
namespace OrchestratorChat.Core.Agents
{
    /// <summary>
    /// Base interface for all agent implementations
    /// </summary>
    public interface IAgent
    {
        /// <summary>
        /// Unique identifier for the agent instance
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Display name for the agent
        /// </summary>
        string Name { get; set; }
        
        /// <summary>
        /// Agent type (Claude, Saturn, Custom, etc.)
        /// </summary>
        AgentType Type { get; }
        
        /// <summary>
        /// Current agent status
        /// </summary>
        AgentStatus Status { get; }
        
        /// <summary>
        /// Agent capabilities and metadata
        /// </summary>
        AgentCapabilities Capabilities { get; }
        
        /// <summary>
        /// Working directory for the agent
        /// </summary>
        string WorkingDirectory { get; set; }
        
        /// <summary>
        /// Initialize the agent
        /// </summary>
        Task<AgentInitializationResult> InitializeAsync(AgentConfiguration configuration);
        
        /// <summary>
        /// Send a message to the agent
        /// </summary>
        Task<IAsyncEnumerable<AgentResponse>> SendMessageAsync(
            AgentMessage message, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Execute a tool/function
        /// </summary>
        Task<ToolExecutionResult> ExecuteToolAsync(
            ToolCall toolCall, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Shutdown the agent gracefully
        /// </summary>
        Task ShutdownAsync();
        
        /// <summary>
        /// Event raised when agent status changes
        /// </summary>
        event EventHandler<AgentStatusChangedEventArgs> StatusChanged;
        
        /// <summary>
        /// Event raised when agent emits output
        /// </summary>
        event EventHandler<AgentOutputEventArgs> OutputReceived;
    }
}
```

#### Supporting Types
```csharp
public enum AgentType
{
    Claude,
    Saturn,
    Custom,
    Orchestrator
}

public enum AgentStatus
{
    Uninitialized,
    Initializing,
    Ready,
    Busy,
    Error,
    Shutdown
}

public class AgentCapabilities
{
    public bool SupportsStreaming { get; set; }
    public bool SupportsTools { get; set; }
    public bool SupportsFileOperations { get; set; }
    public bool SupportsWebSearch { get; set; }
    public List<string> SupportedModels { get; set; } = new();
    public List<ToolDefinition> AvailableTools { get; set; } = new();
    public int MaxTokens { get; set; }
    public int MaxConcurrentRequests { get; set; } = 1;
}

public class AgentConfiguration
{
    public string Model { get; set; } = "claude-3-sonnet";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 4096;
    public string SystemPrompt { get; set; }
    public Dictionary<string, object> CustomSettings { get; set; } = new();
    public List<string> EnabledTools { get; set; } = new();
    public bool RequireApproval { get; set; } = false;
}

public class AgentInitializationResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public AgentCapabilities Capabilities { get; set; }
    public TimeSpan InitializationTime { get; set; }
}
```

### Message Models

#### Core Message Types
```csharp
namespace OrchestratorChat.Core.Messages
{
    public class AgentMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Content { get; set; }
        public MessageRole Role { get; set; }
        public string AgentId { get; set; }
        public string SessionId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public List<Attachment> Attachments { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public string ParentMessageId { get; set; }
    }
    
    public enum MessageRole
    {
        User,
        Assistant,
        System,
        Tool
    }
    
    public class AgentResponse
    {
        public string MessageId { get; set; }
        public string Content { get; set; }
        public ResponseType Type { get; set; }
        public bool IsComplete { get; set; }
        public List<ToolCall> ToolCalls { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public TokenUsage Usage { get; set; }
    }
    
    public enum ResponseType
    {
        Text,
        ToolCall,
        Error,
        Status,
        Thinking
    }
    
    public class Attachment
    {
        public string Id { get; set; }
        public string FileName { get; set; }
        public string MimeType { get; set; }
        public long Size { get; set; }
        public byte[] Content { get; set; }
        public string Url { get; set; }
    }
    
    public class TokenUsage
    {
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public int TotalTokens { get; set; }
        public decimal EstimatedCost { get; set; }
    }
}
```

### Tool System

#### Tool Abstractions
```csharp
namespace OrchestratorChat.Core.Tools
{
    public interface ITool
    {
        string Name { get; }
        string Description { get; }
        ToolSchema Schema { get; }
        bool RequiresApproval { get; }
        
        Task<ToolExecutionResult> ExecuteAsync(
            ToolCall call, 
            IExecutionContext context,
            CancellationToken cancellationToken = default);
        
        Task<ValidationResult> ValidateAsync(ToolCall call);
    }
    
    public class ToolDefinition
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ToolSchema Schema { get; set; }
        public bool RequiresApproval { get; set; }
        public List<string> Categories { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
    
    public class ToolCall
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ToolName { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public string AgentId { get; set; }
        public string SessionId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
    
    public class ToolExecutionResult
    {
        public bool Success { get; set; }
        public string Output { get; set; }
        public string Error { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
    
    public class ToolSchema
    {
        public string Type { get; set; } = "object";
        public Dictionary<string, ParameterSchema> Properties { get; set; } = new();
        public List<string> Required { get; set; } = new();
    }
    
    public class ParameterSchema
    {
        public string Type { get; set; }
        public string Description { get; set; }
        public object Default { get; set; }
        public List<object> Enum { get; set; }
        public object Pattern { get; set; }
    }
}
```

### Session Management

#### Session Abstractions
```csharp
namespace OrchestratorChat.Core.Sessions
{
    public interface ISessionManager
    {
        Task<Session> CreateSessionAsync(SessionConfiguration configuration);
        Task<Session> GetSessionAsync(string sessionId);
        Task<List<Session>> GetActiveSessionsAsync();
        Task<bool> UpdateSessionAsync(Session session);
        Task<bool> EndSessionAsync(string sessionId);
        Task<SessionSnapshot> CreateSnapshotAsync(string sessionId);
        Task<Session> RestoreFromSnapshotAsync(string snapshotId);
    }
    
    public class Session
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public SessionType Type { get; set; }
        public SessionStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public List<string> ParticipantAgentIds { get; set; } = new();
        public List<AgentMessage> Messages { get; set; } = new();
        public Dictionary<string, object> Context { get; set; } = new();
        public string WorkingDirectory { get; set; }
        public string ProjectId { get; set; }
    }
    
    public enum SessionType
    {
        SingleAgent,
        MultiAgent,
        Orchestrated
    }
    
    public enum SessionStatus
    {
        Active,
        Paused,
        Completed,
        Failed
    }
    
    public class SessionConfiguration
    {
        public string Name { get; set; }
        public SessionType Type { get; set; }
        public List<string> AgentIds { get; set; } = new();
        public string WorkingDirectory { get; set; }
        public bool PersistHistory { get; set; } = true;
        public int MaxMessages { get; set; } = 1000;
        public Dictionary<string, object> InitialContext { get; set; } = new();
    }
    
    public class SessionSnapshot
    {
        public string Id { get; set; }
        public string SessionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Description { get; set; }
        public Session SessionState { get; set; }
        public Dictionary<string, AgentState> AgentStates { get; set; } = new();
    }
}
```

### Orchestration

#### Orchestration Interfaces
```csharp
namespace OrchestratorChat.Core.Orchestration
{
    public interface IOrchestrator
    {
        Task<OrchestrationPlan> CreatePlanAsync(
            OrchestrationRequest request,
            CancellationToken cancellationToken = default);
        
        Task<OrchestrationResult> ExecutePlanAsync(
            OrchestrationPlan plan,
            IProgress<OrchestrationProgress> progress = null,
            CancellationToken cancellationToken = default);
        
        Task<bool> ValidatePlanAsync(OrchestrationPlan plan);
    }
    
    public class OrchestrationRequest
    {
        public string Goal { get; set; }
        public List<string> AvailableAgentIds { get; set; } = new();
        public OrchestrationStrategy Strategy { get; set; }
        public Dictionary<string, object> Constraints { get; set; } = new();
        public int MaxSteps { get; set; } = 10;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(30);
    }
    
    public enum OrchestrationStrategy
    {
        Sequential,
        Parallel,
        Adaptive,
        RoundRobin
    }
    
    public class OrchestrationPlan
    {
        public string Id { get; set; }
        public List<OrchestrationStep> Steps { get; set; } = new();
        public Dictionary<string, object> SharedContext { get; set; } = new();
        public List<string> RequiredAgents { get; set; } = new();
    }
    
    public class OrchestrationStep
    {
        public int Order { get; set; }
        public string AgentId { get; set; }
        public string Task { get; set; }
        public List<string> DependsOn { get; set; } = new();
        public Dictionary<string, object> Input { get; set; } = new();
        public TimeSpan Timeout { get; set; }
        public bool CanRunInParallel { get; set; }
    }
    
    public class OrchestrationResult
    {
        public bool Success { get; set; }
        public List<StepResult> StepResults { get; set; } = new();
        public string FinalOutput { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public Dictionary<string, object> FinalContext { get; set; } = new();
    }
    
    public class OrchestrationProgress
    {
        public int CurrentStep { get; set; }
        public int TotalSteps { get; set; }
        public string CurrentAgent { get; set; }
        public string CurrentTask { get; set; }
        public double PercentComplete { get; set; }
        public TimeSpan ElapsedTime { get; set; }
    }
}
```

### Events and Notifications

#### Event System
```csharp
namespace OrchestratorChat.Core.Events
{
    public interface IEventBus
    {
        Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent;
        void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent;
        void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent;
    }
    
    public interface IEvent
    {
        string Id { get; }
        DateTime Timestamp { get; }
        string Source { get; }
    }
    
    public class AgentStatusChangedEvent : IEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Source { get; set; }
        public string AgentId { get; set; }
        public AgentStatus OldStatus { get; set; }
        public AgentStatus NewStatus { get; set; }
        public string Reason { get; set; }
    }
    
    public class MessageReceivedEvent : IEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Source { get; set; }
        public AgentMessage Message { get; set; }
        public string SessionId { get; set; }
    }
    
    public class ToolExecutedEvent : IEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Source { get; set; }
        public ToolCall Call { get; set; }
        public ToolExecutionResult Result { get; set; }
    }
}
```

### Configuration

#### Configuration Interfaces
```csharp
namespace OrchestratorChat.Core.Configuration
{
    public interface IConfigurationProvider
    {
        Task<T> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value) where T : class;
        Task<bool> ExistsAsync(string key);
        Task DeleteAsync(string key);
        Task<Dictionary<string, object>> GetAllAsync();
    }
    
    public class ApplicationSettings
    {
        public DatabaseSettings Database { get; set; } = new();
        public SignalRSettings SignalR { get; set; } = new();
        public AgentSettings Agents { get; set; } = new();
        public SecuritySettings Security { get; set; } = new();
        public LoggingSettings Logging { get; set; } = new();
    }
    
    public class DatabaseSettings
    {
        public string ConnectionString { get; set; }
        public int CommandTimeout { get; set; } = 30;
        public bool EnableSensitiveDataLogging { get; set; } = false;
    }
    
    public class SignalRSettings
    {
        public int KeepAliveInterval { get; set; } = 15;
        public int ClientTimeoutInterval { get; set; } = 30;
        public int HandshakeTimeout { get; set; } = 15;
        public bool EnableDetailedErrors { get; set; } = false;
        public long MaximumReceiveMessageSize { get; set; } = 32 * 1024;
    }
    
    public class AgentSettings
    {
        public string ClaudeExecutablePath { get; set; }
        public string SaturnLibraryPath { get; set; }
        public int MaxConcurrentAgents { get; set; } = 10;
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(5);
        public Dictionary<string, AgentConfiguration> DefaultConfigurations { get; set; } = new();
    }
    
    public class SecuritySettings
    {
        public bool RequireAuthentication { get; set; } = true;
        public string JwtSecret { get; set; }
        public int TokenExpirationMinutes { get; set; } = 60;
        public List<string> AllowedOrigins { get; set; } = new();
    }
}
```

### Exceptions

#### Custom Exceptions
```csharp
namespace OrchestratorChat.Core.Exceptions
{
    public class OrchestratorException : Exception
    {
        public string ErrorCode { get; set; }
        public Dictionary<string, object> Context { get; set; } = new();
        
        public OrchestratorException(string message, string errorCode = null) 
            : base(message)
        {
            ErrorCode = errorCode;
        }
    }
    
    public class AgentException : OrchestratorException
    {
        public string AgentId { get; set; }
        public AgentStatus AgentStatus { get; set; }
        
        public AgentException(string message, string agentId) 
            : base(message, "AGENT_ERROR")
        {
            AgentId = agentId;
        }
    }
    
    public class ToolExecutionException : OrchestratorException
    {
        public string ToolName { get; set; }
        public ToolCall Call { get; set; }
        
        public ToolExecutionException(string message, string toolName, ToolCall call) 
            : base(message, "TOOL_ERROR")
        {
            ToolName = toolName;
            Call = call;
        }
    }
    
    public class OrchestrationException : OrchestratorException
    {
        public OrchestrationPlan Plan { get; set; }
        public int FailedStep { get; set; }
        
        public OrchestrationException(string message, OrchestrationPlan plan) 
            : base(message, "ORCHESTRATION_ERROR")
        {
            Plan = plan;
        }
    }
}
```

## Dependency Injection Registration

```csharp
// In Program.cs or Startup.cs
services.AddScoped<ISessionManager, SessionManager>();
services.AddScoped<IOrchestrator, Orchestrator>();
services.AddSingleton<IEventBus, InMemoryEventBus>();
services.AddSingleton<IConfigurationProvider, JsonConfigurationProvider>();

// Agent registration
services.AddTransient<IAgent, ClaudeAgent>();
services.AddTransient<IAgent, SaturnAgent>();

// Tool registration
services.AddTransient<ITool, ReadFileTool>();
services.AddTransient<ITool, WriteFileTool>();
services.AddTransient<ITool, ExecuteCommandTool>();
```

## Testing Considerations

### Unit Testing
- All interfaces should have mock implementations
- Use dependency injection for testability
- Separate business logic from infrastructure

### Integration Testing
- Test agent adapters with real processes
- Test SignalR communication end-to-end
- Test data persistence and retrieval

## Cross-Cutting Concerns

### Logging
- Use ILogger<T> throughout
- Structured logging with Serilog
- Correlation IDs for request tracking

### Performance
- Async/await for all I/O operations
- Streaming for large responses
- Connection pooling for database

### Security
- Input validation on all public methods
- Sanitization of user inputs
- Secure storage of credentials

## Next Steps for Developers

1. **Core Developer**: Implement these interfaces in OrchestratorChat.Core project
2. **Agent Developer**: Use IAgent interface to build adapters
3. **UI Developer**: Consume these abstractions via dependency injection
4. **Data Developer**: Implement persistence for these models

## Version History
- v1.0 - Initial specification
- Date: 2024-01-30
- Status: Ready for implementation