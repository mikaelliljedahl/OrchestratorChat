using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using OrchestratorChat.Core.Agents;

namespace OrchestratorChat.Agents;

/// <summary>
/// Registry for managing active agent instances
/// </summary>
public class AgentRegistry : IAgentRegistry, IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, IAgent> _agents = new();
    private readonly IServiceProvider _serviceProvider;

    public AgentRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Find an active agent by ID
    /// </summary>
    /// <param name="agentId">Agent identifier</param>
    /// <returns>Agent instance if found, null otherwise</returns>
    public Task<IAgent?> FindAsync(string agentId)
    {
        if (string.IsNullOrWhiteSpace(agentId))
            return Task.FromResult<IAgent?>(null);

        _agents.TryGetValue(agentId, out var agent);
        return Task.FromResult(agent);
    }

    /// <summary>
    /// Get an existing agent or create a new one using the provided factory
    /// </summary>
    /// <param name="agentId">Agent identifier</param>
    /// <param name="factory">Factory function to create agent type and configuration</param>
    /// <returns>Agent instance</returns>
    public async Task<IAgent> GetOrCreateAsync(string agentId, Func<Task<(AgentType type, AgentConfiguration cfg)>> factory)
    {
        if (string.IsNullOrWhiteSpace(agentId))
            throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentId));

        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        if (_agents.TryGetValue(agentId, out var existingAgent))
            return existingAgent;

        var (type, cfg) = await factory();
        
        // Resolve IAgentFactory from service provider when needed
        using var scope = _serviceProvider.CreateScope();
        var agentFactory = scope.ServiceProvider.GetRequiredService<IAgentFactory>();
        var agent = await agentFactory.CreateAgentAsync(type, cfg);
        
        _agents[agentId] = agent;
        return agent;
    }

    /// <summary>
    /// Remove an agent from the registry and dispose it properly
    /// </summary>
    /// <param name="agentId">Agent identifier</param>
    /// <returns>Task representing the removal operation</returns>
    public async Task RemoveAsync(string agentId)
    {
        if (string.IsNullOrWhiteSpace(agentId))
            return;

        if (_agents.TryRemove(agentId, out var agent))
        {
            await DisposeAgentAsync(agent);
        }
    }

    /// <summary>
    /// Dispose all agents and clean up resources
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        var agents = _agents.Values.ToList();
        _agents.Clear();

        var disposeTasks = agents.Select(DisposeAgentAsync);
        await Task.WhenAll(disposeTasks);

        GC.SuppressFinalize(this);
    }

    private static async Task DisposeAgentAsync(IAgent agent)
    {
        try
        {
            if (agent is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (agent is IDisposable disposable)
            {
                disposable.Dispose();
            }
            else
            {
                // Try graceful shutdown if available
                await agent.ShutdownAsync();
            }
        }
        catch (Exception)
        {
            // Log exception but don't rethrow during disposal
            // This prevents disposal chains from breaking
        }
    }
}