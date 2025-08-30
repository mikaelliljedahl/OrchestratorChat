using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrchestratorChat.Core.Messages;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Data.Models;

namespace OrchestratorChat.Data.Repositories;

public class SessionRepository : Repository<SessionEntity>, ISessionRepository
{
    public SessionRepository(OrchestratorDbContext context) : base(context)
    {
    }
    
    public async Task<SessionEntity?> GetWithMessagesAsync(string sessionId)
    {
        return await _dbSet
            .Include(s => s.Messages)
                .ThenInclude(m => m.Attachments)
            .Include(s => s.Messages)
                .ThenInclude(m => m.ToolCalls)
            .Include(s => s.SessionAgents)
                .ThenInclude(sa => sa.Agent)
            .FirstOrDefaultAsync(s => s.Id == sessionId);
    }
    
    public async Task<IEnumerable<SessionEntity>> GetActiveSessionsAsync()
    {
        return await _dbSet
            .Where(s => s.Status == SessionStatus.Active)
            .Include(s => s.SessionAgents)
                .ThenInclude(sa => sa.Agent)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<SessionEntity>> GetRecentSessionsAsync(int count)
    {
        return await _dbSet
            .OrderByDescending(s => s.LastActivityAt)
            .Take(count)
            .Include(s => s.Messages.OrderByDescending(m => m.Timestamp).Take(1))
            .ToListAsync();
    }
    
    public async Task<IEnumerable<SessionEntity>> GetSessionsByProjectAsync(string projectId)
    {
        return await _dbSet
            .Where(s => s.ProjectId == projectId)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync();
    }
    
    public async Task<SessionStatistics?> GetSessionStatisticsAsync(string sessionId)
    {
        var session = await GetWithMessagesAsync(sessionId);
        if (session == null) return null;
        
        return new SessionStatistics
        {
            SessionId = sessionId,
            MessageCount = session.Messages.Count,
            AgentCount = session.SessionAgents.Count,
            Duration = session.CompletedAt.HasValue 
                ? session.CompletedAt.Value - session.CreatedAt 
                : DateTime.UtcNow - session.CreatedAt,
            TotalTokensUsed = session.Messages
                .Where(m => !string.IsNullOrEmpty(m.TokenUsageJson))
                .Select(m => JsonSerializer.Deserialize<TokenUsage>(m.TokenUsageJson))
                .Sum(t => t?.TotalTokens ?? 0),
            ToolCallCount = session.Messages
                .SelectMany(m => m.ToolCalls)
                .Count()
        };
    }
    
    public async Task<bool> AddMessageAsync(string sessionId, MessageEntity message)
    {
        var session = await GetByIdAsync(sessionId);
        if (session == null) return false;
        
        message.SessionId = sessionId;
        message.SequenceNumber = await _context.Messages
            .Where(m => m.SessionId == sessionId)
            .CountAsync() + 1;
        
        await _context.Messages.AddAsync(message);
        
        session.LastActivityAt = DateTime.UtcNow;
        _context.Sessions.Update(session);
        
        await _context.SaveChangesAsync();
        return true;
    }
    
    public async Task<SessionSnapshotEntity?> CreateSnapshotAsync(string sessionId, string name)
    {
        var session = await GetWithMessagesAsync(sessionId);
        if (session == null) return null;
        
        var snapshot = new SessionSnapshotEntity
        {
            SessionId = sessionId,
            Name = name,
            Description = $"Snapshot of {session.Name}",
            CreatedAt = DateTime.UtcNow,
            MessageCount = session.Messages.Count,
            SessionStateJson = JsonSerializer.Serialize(session),
            AgentStatesJson = JsonSerializer.Serialize(
                session.SessionAgents.ToDictionary(
                    sa => sa.AgentId,
                    sa => new { sa.Agent.IsActive, sa.Agent.Configuration }))
        };
        
        await _context.SessionSnapshots.AddAsync(snapshot);
        await _context.SaveChangesAsync();
        
        return snapshot;
    }
    
    public async Task<SessionEntity?> RestoreFromSnapshotAsync(string snapshotId)
    {
        var snapshot = await _context.SessionSnapshots
            .Include(s => s.Session)
            .FirstOrDefaultAsync(s => s.Id == snapshotId);
        
        if (snapshot == null) return null;
        
        var restoredSession = JsonSerializer.Deserialize<SessionEntity>(
            snapshot.SessionStateJson);
        
        if (restoredSession == null) return null;
        
        restoredSession.Id = Guid.NewGuid().ToString();
        restoredSession.Name = $"{restoredSession.Name} (Restored)";
        restoredSession.CreatedAt = DateTime.UtcNow;
        restoredSession.LastActivityAt = DateTime.UtcNow;
        
        await _context.Sessions.AddAsync(restoredSession);
        await _context.SaveChangesAsync();
        
        return restoredSession;
    }
    
    public async Task AddSessionAgentAsync(SessionAgentEntity sessionAgent)
    {
        if (sessionAgent == null)
            throw new ArgumentNullException(nameof(sessionAgent));
            
        await _context.Set<SessionAgentEntity>().AddAsync(sessionAgent);
    }
}