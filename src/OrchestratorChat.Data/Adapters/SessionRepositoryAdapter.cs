using OrchestratorChat.Core.Messages;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace OrchestratorChat.Data.Adapters;

/// <summary>
/// Adapter that implements Core ISessionRepository interface and delegates to Data layer
/// </summary>
public class SessionRepositoryAdapter : Core.Sessions.ISessionRepository
{
    private readonly OrchestratorDbContext _context;

    public SessionRepositoryAdapter(OrchestratorDbContext context)
    {
        _context = context;
    }

    public async Task<Session> CreateSessionAsync(Session session)
    {
        var entity = MapToEntity(session);
        _context.Sessions.Add(entity);
        await _context.SaveChangesAsync();
        return MapToDomain(entity);
    }

    public async Task<Session?> GetSessionByIdAsync(string sessionId)
    {
        var entity = await _context.Sessions.FindAsync(sessionId);
        return entity != null ? MapToDomain(entity) : null;
    }

    public async Task<List<Session>> GetRecentSessionsAsync(int count)
    {
        var entities = await _context.Sessions
            .OrderByDescending(s => s.LastActivityAt)
            .Take(count)
            .ToListAsync();
        return entities.Select(MapToDomain).ToList();
    }

    public async Task<List<Session>> GetActiveSessionsAsync()
    {
        var entities = await _context.Sessions
            .Where(s => s.Status == SessionStatus.Active)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync();
        return entities.Select(MapToDomain).ToList();
    }

    public async Task<bool> UpdateSessionAsync(Session session)
    {
        var entity = await _context.Sessions.FindAsync(session.Id);
        if (entity == null) return false;

        UpdateEntity(entity, session);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteSessionAsync(string sessionId)
    {
        var entity = await _context.Sessions.FindAsync(sessionId);
        if (entity == null) return false;

        _context.Sessions.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task AddMessageAsync(string sessionId, AgentMessage message)
    {
        var messageEntity = new MessageEntity
        {
            Id = Guid.NewGuid().ToString(),
            SessionId = sessionId,
            Content = message.Content,
            Role = (MessageRole)message.Role,
            AgentId = message.AgentId,
            Timestamp = message.Timestamp
        };

        _context.Messages.Add(messageEntity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateSessionContextAsync(string sessionId, Dictionary<string, object> context)
    {
        var entity = await _context.Sessions.FindAsync(sessionId);
        if (entity != null)
        {
            entity.ContextJson = JsonSerializer.Serialize(context);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<SessionSnapshot> CreateSnapshotAsync(string sessionId, SessionSnapshot snapshot)
    {
        var snapshotEntity = new SessionSnapshotEntity
        {
            Id = Guid.NewGuid().ToString(),
            SessionId = sessionId,
            Name = snapshot.Description,
            Description = snapshot.Description,
            CreatedAt = DateTime.UtcNow,
            SessionStateJson = JsonSerializer.Serialize(snapshot.SessionState)
        };

        _context.SessionSnapshots.Add(snapshotEntity);
        await _context.SaveChangesAsync();

        return new SessionSnapshot
        {
            Id = snapshotEntity.Id,
            SessionId = snapshotEntity.SessionId,
            Description = snapshotEntity.Description ?? string.Empty,
            CreatedAt = snapshotEntity.CreatedAt,
            SessionState = snapshot.SessionState
        };
    }

    public async Task<SessionSnapshot?> GetSnapshotAsync(string snapshotId)
    {
        var entity = await _context.SessionSnapshots
            .FirstOrDefaultAsync(s => s.Id == snapshotId);

        if (entity == null) return null;

        Session? sessionState = null;
        if (!string.IsNullOrEmpty(entity.SessionStateJson))
        {
            sessionState = JsonSerializer.Deserialize<Session>(entity.SessionStateJson);
        }

        return new SessionSnapshot
        {
            Id = entity.Id,
            SessionId = entity.SessionId,
            Description = entity.Description ?? string.Empty,
            CreatedAt = entity.CreatedAt,
            SessionState = sessionState ?? new Session()
        };
    }

    private SessionEntity MapToEntity(Session session)
    {
        return new SessionEntity
        {
            Id = session.Id,
            Name = session.Name,
            Type = session.Type,
            Status = session.Status,
            WorkingDirectory = session.WorkingDirectory,
            ProjectId = session.ProjectId,
            CreatedAt = session.CreatedAt,
            LastActivityAt = session.LastActivityAt,
            ContextJson = JsonSerializer.Serialize(session.Context)
        };
    }

    private Session MapToDomain(SessionEntity entity)
    {
        Dictionary<string, object> context = new();
        if (!string.IsNullOrEmpty(entity.ContextJson))
        {
            try
            {
                context = JsonSerializer.Deserialize<Dictionary<string, object>>(entity.ContextJson) ?? new();
            }
            catch
            {
                context = new();
            }
        }

        return new Session
        {
            Id = entity.Id,
            Name = entity.Name,
            Type = entity.Type,
            Status = entity.Status,
            WorkingDirectory = entity.WorkingDirectory,
            ProjectId = entity.ProjectId,
            CreatedAt = entity.CreatedAt,
            LastActivityAt = entity.LastActivityAt,
            Context = context
        };
    }

    private void UpdateEntity(SessionEntity entity, Session session)
    {
        entity.Name = session.Name;
        entity.Type = session.Type;
        entity.Status = session.Status;
        entity.WorkingDirectory = session.WorkingDirectory;
        entity.ProjectId = session.ProjectId;
        entity.LastActivityAt = session.LastActivityAt;
        entity.ContextJson = JsonSerializer.Serialize(session.Context);
    }
}