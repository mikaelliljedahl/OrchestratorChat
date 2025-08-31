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
    private readonly ConcurrentDictionary<string, IAgent> _activeAgents = new();

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

        // Track the created agent
        _activeAgents[agent.Id] = agent;

        return agent;
    }

    public List<AgentType> GetSupportedTypes()
    {
        return Enum.GetValues<AgentType>()
            .Where(t => t != AgentType.Custom)
            .ToList();
    }


    public Task<IEnumerable<AgentType>> GetAvailableAgentTypesAsync(CancellationToken cancellationToken = default)
    {
        // Return available agent types that can be created
        var availableTypes = new List<AgentType>
        {
            AgentType.Claude,
            AgentType.Saturn
        };
        
        return Task.FromResult<IEnumerable<AgentType>>(availableTypes);
    }

    public Task<IAgent?> GetAgentAsync(string agentId)
    {
        _activeAgents.TryGetValue(agentId, out var agent);
        return Task.FromResult(agent);
    }

    public async Task DisposeAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (_activeAgents.TryRemove(agentId, out var agent))
        {
            try
            {
                if (agent is IDisposable disposableAgent)
                {
                    disposableAgent.Dispose();
                }
                else if (agent is IAsyncDisposable asyncDisposableAgent)
                {
                    await asyncDisposableAgent.DisposeAsync();
                }
                else
                {
                    // If agent doesn't implement disposal interfaces, try to shutdown gracefully
                    await agent.ShutdownAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing agent {AgentId}", agentId);
            }
        }
    }

}