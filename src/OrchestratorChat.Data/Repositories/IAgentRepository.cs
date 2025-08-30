using OrchestratorChat.Data.Models;

namespace OrchestratorChat.Data.Repositories;

public interface IAgentRepository : IRepository<AgentEntity>
{
    Task<AgentEntity?> GetWithConfigurationAsync(string agentId);
    Task<IEnumerable<AgentEntity>> GetActiveAgentsAsync();
    Task<bool> UpdateConfigurationAsync(string agentId, AgentConfigurationEntity config);
    Task<AgentUsageStatistics?> GetUsageStatisticsAsync(string agentId, DateTime? from = null);
    Task IncrementUsageAsync(string agentId, int messageCount, long tokensUsed);
}