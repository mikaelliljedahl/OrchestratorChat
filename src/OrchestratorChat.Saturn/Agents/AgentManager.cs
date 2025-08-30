using OrchestratorChat.Saturn.Core;

namespace OrchestratorChat.Saturn.Agents;

/// <summary>
/// Agent manager implementation for multi-agent orchestration
/// </summary>
public class AgentManager : IAgentManager
{
    private readonly Dictionary<string, ISaturnAgent> _agents = new();
    private readonly object _lock = new();

    public Task<ISaturnAgent> GetAgentAsync(string agentId)
    {
        lock (_lock)
        {
            _agents.TryGetValue(agentId, out var agent);
            return Task.FromResult(agent!);
        }
    }

    public Task<List<ISaturnAgent>> GetAllAgentsAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_agents.Values.ToList());
        }
    }

    public Task RemoveAgentAsync(string agentId)
    {
        lock (_lock)
        {
            if (_agents.TryGetValue(agentId, out var agent))
            {
                _agents.Remove(agentId);
                // Shutdown agent if needed
                _ = Task.Run(async () => await agent.ShutdownAsync());
            }
        }
        return Task.CompletedTask;
    }

    public Task<Dictionary<string, object>> GetAgentStatusAsync()
    {
        lock (_lock)
        {
            var status = new Dictionary<string, object>
            {
                ["total_agents"] = _agents.Count,
                ["agents"] = _agents.Values.Select(agent => new
                {
                    id = agent.Id,
                    name = agent.Name,
                    status = agent.Status.ToString()
                }).ToList()
            };
            return Task.FromResult(status);
        }
    }

    public void RegisterAgent(ISaturnAgent agent)
    {
        if (agent == null)
            throw new ArgumentNullException(nameof(agent));

        lock (_lock)
        {
            _agents[agent.Id] = agent;
        }
    }

    public bool UnregisterAgent(string agentId)
    {
        lock (_lock)
        {
            return _agents.Remove(agentId);
        }
    }

    public int AgentCount
    {
        get
        {
            lock (_lock)
            {
                return _agents.Count;
            }
        }
    }
}