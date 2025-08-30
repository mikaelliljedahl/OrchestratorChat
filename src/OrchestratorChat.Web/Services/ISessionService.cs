using OrchestratorChat.Core.Sessions;

namespace OrchestratorChat.Web.Services;

public interface ISessionService
{
    Task<Session?> GetCurrentSessionAsync();
    Task<Session> CreateSessionAsync(string name, string? description = null);
    Task<Session?> GetSessionAsync(string sessionId);
    Task<List<Session>> GetRecentSessionsAsync(int count = 10);
    Task UpdateSessionAsync(Session session);
    Task DeleteSessionAsync(string sessionId);
    Task<bool> SetCurrentSessionAsync(string sessionId);
}