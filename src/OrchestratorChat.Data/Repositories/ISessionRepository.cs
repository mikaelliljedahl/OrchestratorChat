using OrchestratorChat.Data.Models;

namespace OrchestratorChat.Data.Repositories;

public interface ISessionRepository : IRepository<SessionEntity>
{
    Task<SessionEntity?> GetWithMessagesAsync(string sessionId);
    Task<IEnumerable<SessionEntity>> GetActiveSessionsAsync();
    Task<IEnumerable<SessionEntity>> GetRecentSessionsAsync(int count);
    Task<IEnumerable<SessionEntity>> GetSessionsByProjectAsync(string projectId);
    Task<SessionStatistics?> GetSessionStatisticsAsync(string sessionId);
    Task<bool> AddMessageAsync(string sessionId, MessageEntity message);
    Task<SessionSnapshotEntity?> CreateSnapshotAsync(string sessionId, string name);
    Task<SessionEntity?> RestoreFromSnapshotAsync(string snapshotId);
    Task AddSessionAgentAsync(SessionAgentEntity sessionAgent);
}