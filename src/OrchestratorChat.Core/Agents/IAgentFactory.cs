using OrchestratorChat.Core.Agents;

namespace OrchestratorChat.Core.Agents;

public interface IAgentFactory
{
    Task<IAgent> CreateAgentAsync(AgentType type, AgentConfiguration configuration);
    Task<List<AgentInfo>> GetConfiguredAgents();
    Task<IAgent?> GetAgentAsync(string agentId);
    void RegisterAgent(string agentId, IAgent agent);
    List<AgentType> GetSupportedTypes();
}