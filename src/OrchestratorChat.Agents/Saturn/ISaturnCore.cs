using OrchestratorChat.Core.Agents;
using OrchestratorChat.Saturn.Core;
using OrchestratorChat.Saturn.Models;
using OrchestratorChat.Saturn.Providers;
using OrchestratorChat.Saturn.Agents;
using OrchestratorChat.Saturn.Tools;
using ISaturnAgent = OrchestratorChat.Saturn.Core.ISaturnAgent;
using SaturnAgentConfiguration = OrchestratorChat.Saturn.Models.SaturnAgentConfiguration;
using SaturnTool = OrchestratorChat.Saturn.Tools.ITool;
using SaturnToolInfo = OrchestratorChat.Saturn.Models.ToolInfo;
using SaturnAgentMessage = OrchestratorChat.Saturn.Models.AgentMessage;
using SaturnToolRegistry = OrchestratorChat.Saturn.Tools.ToolRegistry;
using SaturnILLMProvider = OrchestratorChat.Saturn.Providers.ILLMProvider;

namespace OrchestratorChat.Agents.Saturn;

/// <summary>
/// Core Saturn functionality exposed as a library
/// </summary>
public interface ISaturnCore
{
    /// <summary>
    /// Create a new Saturn agent instance
    /// </summary>
    Task<ISaturnAgent> CreateAgentAsync(
        SaturnILLMProvider provider,
        SaturnAgentConfiguration configuration);

    /// <summary>
    /// Create an LLM provider
    /// </summary>
    Task<SaturnILLMProvider> CreateProviderAsync(
        ProviderType providerType,
        Dictionary<string, object> settings);

    /// <summary>
    /// Get available tools
    /// </summary>
    List<SaturnToolInfo> GetAvailableTools();

    /// <summary>
    /// Register custom tool
    /// </summary>
    void RegisterTool(SaturnTool tool);

    /// <summary>
    /// Get or create agent manager for multi-agent scenarios
    /// </summary>
    IAgentManager GetAgentManager();
}

/// <summary>
/// Saturn core implementation
/// </summary>
public class SaturnCore : ISaturnCore
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SaturnToolRegistry _toolRegistry;
    private readonly IAgentManager _agentManager;

    public SaturnCore(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _toolRegistry = new SaturnToolRegistry();
        _agentManager = new AgentManager();

        // Register default Saturn tools
        RegisterDefaultTools();
    }

    public async Task<ISaturnAgent> CreateAgentAsync(
        SaturnILLMProvider provider,
        SaturnAgentConfiguration configuration)
    {
        var agent = new OrchestratorChat.Saturn.Agents.SaturnAgent(provider, _toolRegistry)
        {
            Name = configuration.Model,
            Configuration = configuration
        };

        await agent.InitializeAsync();
        return agent;
    }

    public async Task<SaturnILLMProvider> CreateProviderAsync(
        ProviderType providerType,
        Dictionary<string, object> settings)
    {
        return providerType switch
        {
            ProviderType.OpenRouter => await CreateOpenRouterProvider(settings),
            ProviderType.Anthropic => await CreateAnthropicProvider(settings),
            _ => throw new NotSupportedException($"Provider {providerType} not supported")
        };
    }

    public List<SaturnToolInfo> GetAvailableTools()
    {
        return _toolRegistry.GetToolInfos();
    }

    public void RegisterTool(SaturnTool tool)
    {
        _toolRegistry.Register(tool);
    }

    public IAgentManager GetAgentManager()
    {
        return _agentManager;
    }

    private void RegisterDefaultTools()
    {
        // Register Saturn's built-in tools
        _toolRegistry.Register(new OrchestratorChat.Saturn.Tools.Implementations.ReadFileTool());
        _toolRegistry.Register(new OrchestratorChat.Saturn.Tools.Implementations.WriteFileTool());
        _toolRegistry.Register(new OrchestratorChat.Saturn.Tools.Implementations.BashTool());
        _toolRegistry.Register(new OrchestratorChat.Saturn.Tools.Implementations.GrepTool());
    }
    
    private async Task<SaturnILLMProvider> CreateOpenRouterProvider(Dictionary<string, object> settings)
    {
        // Create and initialize OpenRouter provider
        var apiKey = settings.GetValueOrDefault("ApiKey", "")?.ToString() ?? "";
        var provider = new OrchestratorChat.Saturn.Providers.OpenRouterProvider(apiKey);
        await provider.InitializeAsync();
        return provider;
    }
    
    private async Task<SaturnILLMProvider> CreateAnthropicProvider(Dictionary<string, object> settings)
    {
        // Create and initialize Anthropic provider
        var provider = new OrchestratorChat.Saturn.Providers.AnthropicProvider(settings);
        await provider.InitializeAsync();
        return provider;
    }
}

// Agent manager stub - would be implemented in the Saturn library

public class AgentManager : IAgentManager
{
    private readonly Dictionary<string, ISaturnAgent> _agents = new();

    public Task<ISaturnAgent> GetAgentAsync(string agentId)
    {
        _agents.TryGetValue(agentId, out var agent);
        var result = agent ?? throw new ArgumentException($"Agent {agentId} not found");
        return Task.FromResult(result);
    }

    public Task<List<ISaturnAgent>> GetAllAgentsAsync()
    {
        return Task.FromResult(_agents.Values.ToList());
    }

    public async Task RemoveAgentAsync(string agentId)
    {
        if (_agents.TryGetValue(agentId, out var agent))
        {
            await agent.ShutdownAsync();
            _agents.Remove(agentId);
        }
    }

    public Task<Dictionary<string, object>> GetAgentStatusAsync()
    {
        var status = new Dictionary<string, object>();
        foreach (var kvp in _agents)
        {
            status[kvp.Key] = kvp.Value.Status;
        }
        return Task.FromResult(status);
    }
}