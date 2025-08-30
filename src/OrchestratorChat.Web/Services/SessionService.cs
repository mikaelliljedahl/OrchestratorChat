using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Data;
using OrchestratorChat.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace OrchestratorChat.Web.Services;

public class SessionService : ISessionService
{
    private readonly OrchestratorDbContext _context;
    private string? _currentSessionId;

    public SessionService(OrchestratorDbContext context)
    {
        _context = context;
    }

    public async Task<Session?> GetCurrentSessionAsync()
    {
        if (_currentSessionId == null)
            return null;

        var entity = await _context.Sessions
            .FirstOrDefaultAsync(s => s.Id == _currentSessionId);
            
        return entity != null ? MapToCore(entity) : null;
    }

    public async Task<Session> CreateSessionAsync(string name, string? description = null)
    {
        var entity = new SessionEntity
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Type = SessionType.SingleAgent,
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            WorkingDirectory = Environment.CurrentDirectory
        };

        _context.Sessions.Add(entity);
        await _context.SaveChangesAsync();
        
        _currentSessionId = entity.Id;
        return MapToCore(entity);
    }

    public async Task<Session?> GetSessionAsync(string sessionId)
    {
        var entity = await _context.Sessions
            .FirstOrDefaultAsync(s => s.Id == sessionId);
            
        return entity != null ? MapToCore(entity) : null;
    }

    public async Task<List<Session>> GetRecentSessionsAsync(int count = 10)
    {
        var entities = await _context.Sessions
            .OrderByDescending(s => s.LastActivityAt)
            .Take(count)
            .ToListAsync();
            
        return entities.Select(MapToCore).ToList();
    }

    public async Task UpdateSessionAsync(Session session)
    {
        var entity = await _context.Sessions.FindAsync(session.Id);
        if (entity != null)
        {
            entity.Name = session.Name;
            entity.Status = session.Status;
            entity.LastActivityAt = DateTime.UtcNow;
            entity.WorkingDirectory = session.WorkingDirectory;
            entity.ProjectId = session.ProjectId;
            
            _context.Sessions.Update(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteSessionAsync(string sessionId)
    {
        var entity = await _context.Sessions.FindAsync(sessionId);
        if (entity != null)
        {
            _context.Sessions.Remove(entity);
            await _context.SaveChangesAsync();
            
            if (_currentSessionId == sessionId)
            {
                _currentSessionId = null;
            }
        }
    }

    public async Task<bool> SetCurrentSessionAsync(string sessionId)
    {
        var entity = await _context.Sessions.FindAsync(sessionId);
        if (entity != null)
        {
            _currentSessionId = sessionId;
            return true;
        }
        return false;
    }

    private static Session MapToCore(SessionEntity entity)
    {
        return new Session
        {
            Id = entity.Id,
            Name = entity.Name,
            Type = entity.Type,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt,
            LastActivityAt = entity.LastActivityAt,
            WorkingDirectory = entity.WorkingDirectory,
            ProjectId = entity.ProjectId,
            ParticipantAgentIds = new List<string>(), // TODO: Map from SessionAgents
            Messages = new List<Core.Messages.AgentMessage>(), // TODO: Map from MessageEntities
            Context = new Dictionary<string, object>() // TODO: Deserialize from ContextJson
        };
    }
}