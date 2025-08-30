using Microsoft.Extensions.DependencyInjection;
using OrchestratorChat.Saturn.Agents;
using OrchestratorChat.Saturn.Models;
using OrchestratorChat.Saturn.Providers;
using OrchestratorChat.Saturn.Tools;
using OrchestratorChat.Saturn.Tools.Implementations;
using System.Text.Json;

namespace OrchestratorChat.Saturn.Core;

/// <summary>
/// Main implementation of Saturn core functionality
/// </summary>
public class SaturnCore : ISaturnCore
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ToolRegistry _toolRegistry;
    private readonly Dictionary<string, ILLMProvider> _providers;
    private readonly AgentManager _agentManager;

    public SaturnCore(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _toolRegistry = new ToolRegistry();
        _providers = new Dictionary<string, ILLMProvider>();
        _agentManager = new AgentManager();
        
        InitializeDefaultTools();
    }

    public async Task<ISaturnAgent> CreateAgentAsync(
        ILLMProvider provider,
        SaturnAgentConfiguration configuration)
    {
        var agent = new SaturnAgent(provider, _toolRegistry)
        {
            Name = configuration.Model,
            Configuration = configuration
        };

        // Hook up tool approval if needed
        if (configuration.RequireApproval)
        {
            foreach (var tool in _toolRegistry.GetTools())
            {
                if (tool is ToolBase toolBase && tool.RequiresApproval)
                {
                    toolBase.OnApprovalRequired = async (call) =>
                    {
                        // This will be handled via SignalR callback in the future
                        // For now, auto-approve in development
                        return await RequestApprovalViaSignalR(call);
                    };
                }
            }
        }

        await agent.InitializeAsync();
        _agentManager.RegisterAgent(agent);
        
        return agent;
    }

    public async Task<IAgentManager> GetAgentManagerAsync()
    {
        return await Task.FromResult(_agentManager);
    }

    public async Task<ILLMProvider> CreateProviderAsync(
        ProviderType type,
        Dictionary<string, object> settings)
    {
        ILLMProvider provider = type switch
        {
            ProviderType.OpenRouter => new OpenRouterProvider(
                settings.GetValueOrDefault("ApiKey")?.ToString() ?? string.Empty),
            ProviderType.Anthropic => new AnthropicProvider(settings),
            _ => throw new NotSupportedException($"Provider {type} not supported")
        };

        await provider.InitializeAsync();
        _providers[provider.Id] = provider;

        return provider;
    }

    public List<ProviderInfo> GetAvailableProviders()
    {
        return new List<ProviderInfo>
        {
            new()
            {
                Id = "openrouter",
                Name = "OpenRouter",
                Type = ProviderType.OpenRouter,
                SupportedModels = new List<string>
                {
                    "claude-3-sonnet",
                    "claude-3-haiku",
                    "gpt-4",
                    "gpt-3.5-turbo"
                },
                IsConfigured = _providers.ContainsKey("openrouter")
            },
            new()
            {
                Id = "anthropic",
                Name = "Anthropic",
                Type = ProviderType.Anthropic,
                SupportedModels = new List<string>
                {
                    "claude-3-sonnet-20240229",
                    "claude-3-haiku-20240307"
                },
                IsConfigured = _providers.ContainsKey("anthropic")
            }
        };
    }

    public IToolRegistry GetToolRegistry()
    {
        return _toolRegistry;
    }

    public void RegisterTool(ITool tool)
    {
        _toolRegistry.Register(tool);
    }

    public List<ToolInfo> GetAvailableTools()
    {
        return _toolRegistry.GetToolInfos();
    }

    public async Task<SaturnConfiguration> LoadConfigurationAsync()
    {
        // TODO: Implement configuration loading from file or database
        // For now, return default configuration
        return await Task.FromResult(new SaturnConfiguration
        {
            DefaultConfiguration = new SaturnAgentConfiguration
            {
                Model = "claude-3-sonnet",
                Temperature = 0.7,
                MaxTokens = 4096,
                EnableTools = true,
                RequireApproval = true
            },
            Tools = new ToolConfiguration
            {
                Enabled = new List<string> { "read_file", "write_file", "bash", "grep" },
                RequireApproval = new List<string> { "bash", "write_file" }
            },
            MultiAgent = new MultiAgentConfiguration
            {
                Enabled = true,
                MaxConcurrentAgents = 5
            }
        });
    }

    public async Task SaveConfigurationAsync(SaturnConfiguration config)
    {
        // TODO: Implement configuration saving to file or database
        await Task.CompletedTask;
    }

    private void InitializeDefaultTools()
    {
        _toolRegistry.Register(new ReadFileTool());
        _toolRegistry.Register(new WriteFileTool());
        _toolRegistry.Register(new GrepTool());
        _toolRegistry.Register(new BashTool());

        // TODO: Add more tools as they are implemented
        // _toolRegistry.Register(new GlobTool());
        // _toolRegistry.Register(new ApplyDiffTool());
        // _toolRegistry.Register(new WebFetchTool());
        
        // Multi-agent tools (to be implemented)
        // _toolRegistry.Register(new HandOffToAgentTool());
        // _toolRegistry.Register(new WaitForAgentTool());
        // _toolRegistry.Register(new GetTaskResultTool());
        // _toolRegistry.Register(new GetAgentStatusTool());
    }

    private async Task<bool> RequestApprovalViaSignalR(ToolCall call)
    {
        // This will be implemented to request approval via SignalR
        // For now, auto-approve in development
        return await Task.FromResult(true);
    }
}