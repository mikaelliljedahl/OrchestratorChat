using OrchestratorChat.Saturn.Agents;
using OrchestratorChat.Saturn.Models;
using OrchestratorChat.Saturn.Providers;
using OrchestratorChat.Saturn.Tools;

namespace OrchestratorChat.Saturn.Core;

/// <summary>
/// Main interface for embedded Saturn functionality
/// </summary>
public interface ISaturnCore
{
    // Agent Management
    Task<ISaturnAgent> CreateAgentAsync(
        ILLMProvider provider,
        SaturnAgentConfiguration configuration);
    
    Task<IAgentManager> GetAgentManagerAsync();
    
    // Provider Management
    Task<ILLMProvider> CreateProviderAsync(
        ProviderType type,
        Dictionary<string, object> settings);
    
    List<ProviderInfo> GetAvailableProviders();
    
    // Tool Management
    IToolRegistry GetToolRegistry();
    void RegisterTool(ITool tool);
    List<ToolInfo> GetAvailableTools();
    
    // Configuration
    Task<SaturnConfiguration> LoadConfigurationAsync();
    Task SaveConfigurationAsync(SaturnConfiguration config);
}

/// <summary>
/// Saturn agent interface
/// </summary>
public interface ISaturnAgent
{
    string Id { get; }
    string Name { get; set; }
    AgentStatus Status { get; }
    
    // Core operations
    Task<IAsyncEnumerable<AgentResponse>> ProcessMessageAsync(
        AgentMessage message,
        CancellationToken cancellationToken = default);
    
    Task<ToolExecutionResult> ExecuteToolAsync(
        ToolCall toolCall,
        CancellationToken cancellationToken = default);
    
    Task ShutdownAsync();
    
    // Events
    event EventHandler<ToolCallEventArgs> OnToolCall;
    event EventHandler<StreamingEventArgs> OnStreaming;
    event EventHandler<StatusChangedEventArgs> OnStatusChanged;
}

/// <summary>
/// Agent manager interface for multi-agent orchestration
/// </summary>
public interface IAgentManager
{
    Task<ISaturnAgent> GetAgentAsync(string agentId);
    Task<List<ISaturnAgent>> GetAllAgentsAsync();
    Task RemoveAgentAsync(string agentId);
    Task<Dictionary<string, object>> GetAgentStatusAsync();
}