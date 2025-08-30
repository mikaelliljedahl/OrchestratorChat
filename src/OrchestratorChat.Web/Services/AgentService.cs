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

    public async Task<List<AgentInfo>> GetConfiguredAgentsAsync()
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

        return agentInfos;
    }

    public async Task<AgentInfo?> GetAgentAsync(string agentId)
    {
        if (_agents.TryGetValue(agentId, out var agent))
        {
            return new AgentInfo
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
        }

        return null;
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

    public async Task UpdateAgentAsync(AgentInfo agentInfo)
    {
        if (_agents.TryGetValue(agentInfo.Id, out var agent))
        {
            agent.Name = agentInfo.Name;
            agent.WorkingDirectory = agentInfo.WorkingDirectory;
        }
    }

    public async Task DeleteAgentAsync(string agentId)
    {
        if (_agents.TryGetValue(agentId, out var agent))
        {
            // await agent.DisposeAsync(); // TODO: Add cleanup when available
            _agents.Remove(agentId);
        }
    }

    public async Task<bool> IsAgentAvailableAsync(string agentId)
    {
        if (_agents.TryGetValue(agentId, out var agent))
        {
            return agent.Status == AgentStatus.Ready;
        }
        
        return false;
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