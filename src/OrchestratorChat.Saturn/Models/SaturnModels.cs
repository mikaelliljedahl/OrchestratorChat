using System.Text.Json.Serialization;

namespace OrchestratorChat.Saturn.Models;

/// <summary>
/// Configuration for a Saturn agent
/// </summary>
public class SaturnAgentConfiguration
{
    public string Model { get; set; } = "claude-3-sonnet";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 4096;
    public string SystemPrompt { get; set; } = string.Empty;
    public bool EnableTools { get; set; } = true;
    public List<string> ToolNames { get; set; } = new();
    public bool RequireApproval { get; set; } = true;
    public ProviderType ProviderType { get; set; } = ProviderType.OpenRouter;
    public Dictionary<string, object> ProviderSettings { get; set; } = new();
}

/// <summary>
/// Saturn configuration
/// </summary>
public class SaturnConfiguration
{
    public Dictionary<string, ProviderConfiguration> Providers { get; set; } = new();
    public SaturnAgentConfiguration DefaultConfiguration { get; set; } = new();
    public ToolConfiguration Tools { get; set; } = new();
    public MultiAgentConfiguration MultiAgent { get; set; } = new();
}

/// <summary>
/// Provider configuration
/// </summary>
public class ProviderConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public string DefaultModel { get; set; } = string.Empty;
    public Dictionary<string, object> Settings { get; set; } = new();
}

/// <summary>
/// Tool configuration
/// </summary>
public class ToolConfiguration
{
    public List<string> Enabled { get; set; } = new();
    public List<string> RequireApproval { get; set; } = new();
}

/// <summary>
/// Multi-agent configuration
/// </summary>
public class MultiAgentConfiguration
{
    public bool Enabled { get; set; } = true;
    public int MaxConcurrentAgents { get; set; } = 5;
}

/// <summary>
/// Provider types
/// </summary>
public enum ProviderType
{
    OpenRouter,
    Anthropic
}

/// <summary>
/// Provider information
/// </summary>
public class ProviderInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ProviderType Type { get; set; }
    public List<string> SupportedModels { get; set; } = new();
    public bool IsConfigured { get; set; }
}

/// <summary>
/// Tool information
/// </summary>
public class ToolInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool RequiresApproval { get; set; }
    public List<ToolParameter> Parameters { get; set; } = new();
}

/// <summary>
/// Tool parameter information
/// </summary>
public class ToolParameter
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Required { get; set; }
}

/// <summary>
/// Agent status
/// </summary>
public enum AgentStatus
{
    Idle,
    Processing,
    ExecutingTool,
    Error,
    Shutdown
}

/// <summary>
/// Agent message
/// </summary>
public class AgentMessage
{
    public string Content { get; set; } = string.Empty;
    public MessageRole Role { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Message roles
/// </summary>
public enum MessageRole
{
    User,
    Assistant,
    System,
    Tool
}

/// <summary>
/// Agent response
/// </summary>
public class AgentResponse
{
    public string Content { get; set; } = string.Empty;
    public ResponseType Type { get; set; }
    public bool IsComplete { get; set; }
    public List<ToolCall> ToolCalls { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Response types
/// </summary>
public enum ResponseType
{
    Text,
    ToolCall,
    Error
}

/// <summary>
/// Tool call representation
/// </summary>
public class ToolCall
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string Command { get; set; } = string.Empty;
}

/// <summary>
/// Tool execution result
/// </summary>
public class ToolExecutionResult
{
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public TimeSpan ExecutionTime { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Event args for tool calls
/// </summary>
public class ToolCallEventArgs : EventArgs
{
    public string ToolName { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string AgentId { get; set; } = string.Empty;
}

/// <summary>
/// Event args for streaming responses
/// </summary>
public class StreamingEventArgs : EventArgs
{
    public string Content { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
    public string AgentId { get; set; } = string.Empty;
}

/// <summary>
/// Event args for status changes
/// </summary>
public class StatusChangedEventArgs : EventArgs
{
    public AgentStatus PreviousStatus { get; set; }
    public AgentStatus CurrentStatus { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
}