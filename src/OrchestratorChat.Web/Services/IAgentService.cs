using OrchestratorChat.Core.Agents;
using OrchestratorChat.Web.Models;

namespace OrchestratorChat.Web.Services;

public interface IAgentService
{
    Task<List<AgentInfo>> GetConfiguredAgentsAsync();
    Task<AgentInfo?> GetAgentAsync(string agentId);
    Task<AgentInfo> CreateAgentAsync(AgentType type, AgentConfiguration configuration);
    Task UpdateAgentAsync(AgentInfo agent);
    Task DeleteAgentAsync(string agentId);
    Task<bool> IsAgentAvailableAsync(string agentId);
    Task<AgentInfo?> GetDefaultAgentAsync();
    Task<bool> SetDefaultAgentAsync(string agentId);
}