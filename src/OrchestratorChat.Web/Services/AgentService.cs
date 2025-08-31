using OrchestratorChat.Core.Agents;
using OrchestratorChat.Agents;
using OrchestratorChat.Agents.Claude;
using OrchestratorChat.Agents.Saturn;
using OrchestratorChat.Web.Models;

namespace OrchestratorChat.Web.Services;

public class AgentService : IAgentService
{
    private readonly IAgentFactory _agentFactory;
    private readonly Dictionary<string, IAgent> _agents = new();

    public AgentService(IAgentFactory agentFactory)
    {
        _agentFactory = agentFactory;
    }

    public Task<List<AgentInfo>> GetConfiguredAgentsAsync()
    {
        var agentInfos = new List<AgentInfo>();
        
        foreach (var agent in _agents.Values)
        {
            agentInfos.Add(new AgentInfo
            {
                Id = agent.Id,
                Name = agent.Name,
                Type = GetAgentTypeFromAgent(agent),
                Description = $"{GetAgentTypeFromAgent(agent)} Agent",
                Status = agent.Status,
                Capabilities = agent.Capabilities,
                LastActive = DateTime.UtcNow,
                WorkingDirectory = agent.WorkingDirectory
            });
        }

        return Task.FromResult(agentInfos);
    }

    public Task<AgentInfo?> GetAgentAsync(string agentId)
    {
        if (_agents.TryGetValue(agentId, out var agent))
        {
            var agentInfo = new AgentInfo
            {
                Id = agent.Id,
                Name = agent.Name,
                Type = GetAgentTypeFromAgent(agent),
                Description = $"{GetAgentTypeFromAgent(agent)} Agent",
                Status = agent.Status,
                Capabilities = agent.Capabilities,
                LastActive = DateTime.UtcNow,
                WorkingDirectory = agent.WorkingDirectory
            };
            return Task.FromResult<AgentInfo?>(agentInfo);
        }

        return Task.FromResult<AgentInfo?>(null);
    }

    public async Task<AgentInfo> CreateAgentAsync(AgentType type, AgentConfiguration configuration)
    {
        var agent = await _agentFactory.CreateAgentAsync(type, configuration);
        _agents[agent.Id] = agent;

        return new AgentInfo
        {
            Id = agent.Id,
            Name = agent.Name,
            Type = type,
            Description = $"{type} Agent",
            Status = agent.Status,
            Capabilities = agent.Capabilities,
            LastActive = DateTime.UtcNow,
            WorkingDirectory = agent.WorkingDirectory
        };
    }

    public Task UpdateAgentAsync(AgentInfo agentInfo)
    {
        if (_agents.TryGetValue(agentInfo.Id, out var agent))
        {
            agent.Name = agentInfo.Name;
            agent.WorkingDirectory = agentInfo.WorkingDirectory;
        }
        
        return Task.CompletedTask;
    }

    public Task DeleteAgentAsync(string agentId)
    {
        if (_agents.TryGetValue(agentId, out var agent))
        {
            // await agent.DisposeAsync(); // TODO: Add cleanup when available
            _agents.Remove(agentId);
        }
        
        return Task.CompletedTask;
    }

    public Task<bool> IsAgentAvailableAsync(string agentId)
    {
        if (_agents.TryGetValue(agentId, out var agent))
        {
            return Task.FromResult(agent.Status == AgentStatus.Ready);
        }
        
        return Task.FromResult(false);
    }

    private AgentType GetAgentTypeFromAgent(IAgent agent)
    {
        return agent.GetType().Name switch
        {
            nameof(ClaudeAgent) => AgentType.Claude,
            nameof(SaturnAgent) => AgentType.Saturn,
            _ => AgentType.Custom
        };
    }
}