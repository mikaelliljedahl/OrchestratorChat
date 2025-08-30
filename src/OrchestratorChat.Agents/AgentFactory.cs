using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Agents.Claude;
using OrchestratorChat.Agents.Saturn;
using OrchestratorChat.Agents.Exceptions;
using System.Collections.Concurrent;

namespace OrchestratorChat.Agents;

public class AgentFactory : IAgentFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AgentFactory> _logger;
    private readonly ConcurrentDictionary<string, IAgent> _agentRegistry = new();

    public AgentFactory(
        IServiceProvider serviceProvider,
        ILogger<AgentFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<IAgent> CreateAgentAsync(
        AgentType type,
        AgentConfiguration configuration)
    {
        IAgent agent = type switch
        {
            AgentType.Claude => _serviceProvider.GetRequiredService<ClaudeAgent>(),
            AgentType.Saturn => _serviceProvider.GetRequiredService<SaturnAgent>(),
            _ => throw new NotSupportedException($"Agent type {type} not supported")
        };

        agent.Name = configuration.Name ?? $"{type} Agent";
        agent.WorkingDirectory = configuration.WorkingDirectory ?? Directory.GetCurrentDirectory();

        var result = await agent.InitializeAsync(configuration);
        if (!result.Success)
        {
            throw new AgentException($"Failed to initialize {type} agent: {result.ErrorMessage}", agent.Id);
        }

        // Register the created agent
        RegisterAgent(agent.Id, agent);

        return agent;
    }

    public List<AgentType> GetSupportedTypes()
    {
        return Enum.GetValues<AgentType>()
            .Where(t => t != AgentType.Custom)
            .ToList();
    }

    public async Task<List<AgentInfo>> GetConfiguredAgents()
    {
        var agentInfos = new List<AgentInfo>();
        
        foreach (var kvp in _agentRegistry)
        {
            var agent = kvp.Value;
            var agentInfo = new AgentInfo
            {
                Id = agent.Id,
                Name = agent.Name,
                Type = Enum.TryParse<AgentType>(agent.GetType().Name.Replace("Agent", ""), out var type) ? type : AgentType.Custom,
                Description = $"Agent with {(agent.Capabilities?.AvailableTools.Count ?? 0)} tools and {(agent.Capabilities?.SupportedModels.Count ?? 0)} models",
                Status = agent.Status,
                Capabilities = agent.Capabilities,
                LastActive = DateTime.UtcNow, // TODO: Track actual last active time
                WorkingDirectory = agent.WorkingDirectory,
                Configuration = new Dictionary<string, object>() // TODO: Add actual configuration data
            };
            
            agentInfos.Add(agentInfo);
        }
        
        return await Task.FromResult(agentInfos);
    }

    public async Task<IAgent?> GetAgentAsync(string agentId)
    {
        _agentRegistry.TryGetValue(agentId, out var agent);
        return await Task.FromResult(agent);
    }

    public void RegisterAgent(string agentId, IAgent agent)
    {
        _agentRegistry.AddOrUpdate(agentId, agent, (key, oldValue) =>
        {
            _logger.LogWarning("Agent with ID {AgentId} already exists. Replacing with new agent.", agentId);
            return agent;
        });
        
        _logger.LogInformation("Registered agent {AgentId} of type {AgentType}", agentId, agent.GetType().Name);
    }
}