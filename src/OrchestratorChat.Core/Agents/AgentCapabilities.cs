using OrchestratorChat.Core.Tools;

namespace OrchestratorChat.Core.Agents;

/// <summary>
/// Describes the capabilities and features supported by an agent
/// </summary>
public class AgentCapabilities
{
    /// <summary>
    /// Whether the agent supports streaming responses
    /// </summary>
    public bool SupportsStreaming { get; set; }
    
    /// <summary>
    /// Whether the agent supports tool execution
    /// </summary>
    public bool SupportsTools { get; set; }
    
    /// <summary>
    /// Whether the agent supports file operations
    /// </summary>
    public bool SupportsFileOperations { get; set; }
    
    /// <summary>
    /// Whether the agent supports web search functionality
    /// </summary>
    public bool SupportsWebSearch { get; set; }
    
    /// <summary>
    /// List of models supported by this agent
    /// </summary>
    public List<string> SupportedModels { get; set; } = new();
    
    /// <summary>
    /// List of tools available to this agent
    /// </summary>
    public List<ToolDefinition> AvailableTools { get; set; } = new();
    
    /// <summary>
    /// Maximum number of tokens the agent can process
    /// </summary>
    public int MaxTokens { get; set; }
    
    /// <summary>
    /// Maximum number of concurrent requests the agent can handle
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 1;
}