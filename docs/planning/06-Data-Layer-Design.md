# Data Layer Design Document

## Overview
This document specifies the data persistence layer for OrchestratorChat using Entity Framework Core with SQLite, including database schema, repositories, and migration strategy.

## Project: OrchestratorChat.Data

### Database Schema

#### Entity Models

```csharp
namespace OrchestratorChat.Data.Models
{
    public class SessionEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public SessionType Type { get; set; }
        public SessionStatus Status { get; set; }
        public string WorkingDirectory { get; set; }
        public string ProjectId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        // Navigation properties
        public virtual ICollection<MessageEntity> Messages { get; set; } = new List<MessageEntity>();
        public virtual ICollection<SessionAgentEntity> SessionAgents { get; set; } = new List<SessionAgentEntity>();
        public virtual ICollection<SessionSnapshotEntity> Snapshots { get; set; } = new List<SessionSnapshotEntity>();
        
        // JSON serialized data
        public string ContextJson { get; set; } // Dictionary<string, object>
        public string MetadataJson { get; set; } // Additional metadata
    }
    
    public class MessageEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SessionId { get; set; }
        public string Content { get; set; }
        public MessageRole Role { get; set; }
        public string AgentId { get; set; }
        public string ParentMessageId { get; set; }
        public DateTime Timestamp { get; set; }
        public int SequenceNumber { get; set; }
        
        // Navigation properties
        public virtual SessionEntity Session { get; set; }
        public virtual AgentEntity Agent { get; set; }
        public virtual ICollection<AttachmentEntity> Attachments { get; set; } = new List<AttachmentEntity>();
        public virtual ICollection<ToolCallEntity> ToolCalls { get; set; } = new List<ToolCallEntity>();
        
        // JSON serialized data
        public string MetadataJson { get; set; }
        public string TokenUsageJson { get; set; } // TokenUsage object
    }
    
    public class AgentEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public AgentType Type { get; set; }
        public string Description { get; set; }
        public string WorkingDirectory { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        
        // Navigation properties
        public virtual ICollection<SessionAgentEntity> SessionAgents { get; set; } = new List<SessionAgentEntity>();
        public virtual ICollection<MessageEntity> Messages { get; set; } = new List<MessageEntity>();
        public virtual AgentConfigurationEntity Configuration { get; set; }
        
        // Statistics
        public int TotalSessions { get; set; }
        public int TotalMessages { get; set; }
        public long TotalTokensUsed { get; set; }
    }
    
    public class AgentConfigurationEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string AgentId { get; set; }
        public string Model { get; set; }
        public double Temperature { get; set; }
        public int MaxTokens { get; set; }
        public string SystemPrompt { get; set; }
        public bool RequireApproval { get; set; }
        
        // Navigation property
        public virtual AgentEntity Agent { get; set; }
        
        // JSON serialized data
        public string CustomSettingsJson { get; set; } // Dictionary<string, object>
        public string EnabledToolsJson { get; set; } // List<string>
        public string CapabilitiesJson { get; set; } // AgentCapabilities
    }
    
    public class SessionAgentEntity
    {
        public string SessionId { get; set; }
        public string AgentId { get; set; }
        public DateTime JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }
        public string Role { get; set; } // Primary, Secondary, Observer
        
        // Navigation properties
        public virtual SessionEntity Session { get; set; }
        public virtual AgentEntity Agent { get; set; }
    }
    
    public class AttachmentEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string MessageId { get; set; }
        public string FileName { get; set; }
        public string MimeType { get; set; }
        public long Size { get; set; }
        public string StoragePath { get; set; }
        public string Url { get; set; }
        public DateTime UploadedAt { get; set; }
        
        // Navigation property
        public virtual MessageEntity Message { get; set; }
    }
    
    public class ToolCallEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string MessageId { get; set; }
        public string ToolName { get; set; }
        public string ParametersJson { get; set; } // Dictionary<string, object>
        public bool Success { get; set; }
        public string Output { get; set; }
        public string Error { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public DateTime ExecutedAt { get; set; }
        
        // Navigation property
        public virtual MessageEntity Message { get; set; }
    }
    
    public class SessionSnapshotEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SessionId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public int MessageCount { get; set; }
        
        // Navigation property
        public virtual SessionEntity Session { get; set; }
        
        // JSON serialized data
        public string SessionStateJson { get; set; } // Full session state
        public string AgentStatesJson { get; set; } // Dictionary<string, AgentState>
    }
    
    public class OrchestrationPlanEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SessionId { get; set; }
        public string Goal { get; set; }
        public OrchestrationStrategy Strategy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool Success { get; set; }
        
        // Navigation properties
        public virtual SessionEntity Session { get; set; }
        public virtual ICollection<OrchestrationStepEntity> Steps { get; set; } = new List<OrchestrationStepEntity>();
        
        // JSON serialized data
        public string SharedContextJson { get; set; }
        public string ResultJson { get; set; }
    }
    
    public class OrchestrationStepEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string PlanId { get; set; }
        public int Order { get; set; }
        public string AgentId { get; set; }
        public string Task { get; set; }
        public StepStatus Status { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public TimeSpan? Duration { get; set; }
        
        // Navigation properties
        public virtual OrchestrationPlanEntity Plan { get; set; }
        public virtual AgentEntity Agent { get; set; }
        
        // JSON serialized data
        public string InputJson { get; set; }
        public string OutputJson { get; set; }
        public string DependsOnJson { get; set; } // List<string> of step IDs
    }
    
    public enum StepStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Skipped
    }
}
```

### DbContext

```csharp
namespace OrchestratorChat.Data
{
    public class OrchestratorDbContext : DbContext
    {
        public OrchestratorDbContext(DbContextOptions<OrchestratorDbContext> options)
            : base(options)
        {
        }
        
        public DbSet<SessionEntity> Sessions { get; set; }
        public DbSet<MessageEntity> Messages { get; set; }
        public DbSet<AgentEntity> Agents { get; set; }
        public DbSet<AgentConfigurationEntity> AgentConfigurations { get; set; }
        public DbSet<SessionAgentEntity> SessionAgents { get; set; }
        public DbSet<AttachmentEntity> Attachments { get; set; }
        public DbSet<ToolCallEntity> ToolCalls { get; set; }
        public DbSet<SessionSnapshotEntity> SessionSnapshots { get; set; }
        public DbSet<OrchestrationPlanEntity> OrchestrationPlans { get; set; }
        public DbSet<OrchestrationStepEntity> OrchestrationSteps { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Session configuration
            modelBuilder.Entity<SessionEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.ProjectId);
                
                entity.HasMany(e => e.Messages)
                    .WithOne(m => m.Session)
                    .HasForeignKey(m => m.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasMany(e => e.Snapshots)
                    .WithOne(s => s.Session)
                    .HasForeignKey(s => s.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            
            // Message configuration
            modelBuilder.Entity<MessageEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.SessionId);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.AgentId);
                
                entity.HasMany(e => e.Attachments)
                    .WithOne(a => a.Message)
                    .HasForeignKey(a => a.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasMany(e => e.ToolCalls)
                    .WithOne(t => t.Message)
                    .HasForeignKey(t => t.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            
            // Agent configuration
            modelBuilder.Entity<AgentEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.IsActive);
                
                entity.HasOne(e => e.Configuration)
                    .WithOne(c => c.Agent)
                    .HasForeignKey<AgentConfigurationEntity>(c => c.AgentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            
            // Many-to-many: Session-Agent
            modelBuilder.Entity<SessionAgentEntity>(entity =>
            {
                entity.HasKey(e => new { e.SessionId, e.AgentId });
                
                entity.HasOne(e => e.Session)
                    .WithMany(s => s.SessionAgents)
                    .HasForeignKey(e => e.SessionId);
                
                entity.HasOne(e => e.Agent)
                    .WithMany(a => a.SessionAgents)
                    .HasForeignKey(e => e.AgentId);
            });
            
            // Orchestration configuration
            modelBuilder.Entity<OrchestrationPlanEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.SessionId);
                entity.HasIndex(e => e.CreatedAt);
                
                entity.HasMany(e => e.Steps)
                    .WithOne(s => s.Plan)
                    .HasForeignKey(s => s.PlanId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            
            modelBuilder.Entity<OrchestrationStepEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.PlanId, e.Order });
                entity.HasIndex(e => e.Status);
            });
        }
    }
}
```

### Repository Pattern

#### Base Repository
```csharp
namespace OrchestratorChat.Data.Repositories
{
    public interface IRepository<TEntity> where TEntity : class
    {
        Task<TEntity> GetByIdAsync(string id);
        Task<IEnumerable<TEntity>> GetAllAsync();
        Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);
        Task<TEntity> AddAsync(TEntity entity);
        Task UpdateAsync(TEntity entity);
        Task DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
        Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate = null);
    }
    
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        protected readonly OrchestratorDbContext _context;
        protected readonly DbSet<TEntity> _dbSet;
        
        public Repository(OrchestratorDbContext context)
        {
            _context = context;
            _dbSet = context.Set<TEntity>();
        }
        
        public virtual async Task<TEntity> GetByIdAsync(string id)
        {
            return await _dbSet.FindAsync(id);
        }
        
        public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }
        
        public virtual async Task<IEnumerable<TEntity>> FindAsync(
            Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }
        
        public virtual async Task<TEntity> AddAsync(TEntity entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }
        
        public virtual async Task UpdateAsync(TEntity entity)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
        }
        
        public virtual async Task DeleteAsync(string id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
        
        public virtual async Task<bool> ExistsAsync(string id)
        {
            return await _dbSet.FindAsync(id) != null;
        }
        
        public virtual async Task<int> CountAsync(
            Expression<Func<TEntity, bool>> predicate = null)
        {
            return predicate == null 
                ? await _dbSet.CountAsync()
                : await _dbSet.CountAsync(predicate);
        }
    }
}
```

#### Specific Repositories

```csharp
namespace OrchestratorChat.Data.Repositories
{
    public interface ISessionRepository : IRepository<SessionEntity>
    {
        Task<SessionEntity> GetWithMessagesAsync(string sessionId);
        Task<IEnumerable<SessionEntity>> GetActiveSessionsAsync();
        Task<IEnumerable<SessionEntity>> GetRecentSessionsAsync(int count);
        Task<IEnumerable<SessionEntity>> GetSessionsByProjectAsync(string projectId);
        Task<SessionStatistics> GetSessionStatisticsAsync(string sessionId);
        Task<bool> AddMessageAsync(string sessionId, MessageEntity message);
        Task<SessionSnapshotEntity> CreateSnapshotAsync(string sessionId, string name);
        Task<SessionEntity> RestoreFromSnapshotAsync(string snapshotId);
    }
    
    public class SessionRepository : Repository<SessionEntity>, ISessionRepository
    {
        public SessionRepository(OrchestratorDbContext context) : base(context)
        {
        }
        
        public async Task<SessionEntity> GetWithMessagesAsync(string sessionId)
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
        
        public async Task<SessionStatistics> GetSessionStatisticsAsync(string sessionId)
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
        
        public async Task<SessionSnapshotEntity> CreateSnapshotAsync(string sessionId, string name)
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
                        sa => new { sa.Agent.Status, sa.Agent.Configuration }))
            };
            
            await _context.SessionSnapshots.AddAsync(snapshot);
            await _context.SaveChangesAsync();
            
            return snapshot;
        }
        
        public async Task<SessionEntity> RestoreFromSnapshotAsync(string snapshotId)
        {
            var snapshot = await _context.SessionSnapshots
                .Include(s => s.Session)
                .FirstOrDefaultAsync(s => s.Id == snapshotId);
            
            if (snapshot == null) return null;
            
            var restoredSession = JsonSerializer.Deserialize<SessionEntity>(
                snapshot.SessionStateJson);
            
            restoredSession.Id = Guid.NewGuid().ToString();
            restoredSession.Name = $"{restoredSession.Name} (Restored)";
            restoredSession.CreatedAt = DateTime.UtcNow;
            restoredSession.LastActivityAt = DateTime.UtcNow;
            
            await _context.Sessions.AddAsync(restoredSession);
            await _context.SaveChangesAsync();
            
            return restoredSession;
        }
    }
    
    public interface IAgentRepository : IRepository<AgentEntity>
    {
        Task<AgentEntity> GetWithConfigurationAsync(string agentId);
        Task<IEnumerable<AgentEntity>> GetActiveAgentsAsync();
        Task<bool> UpdateConfigurationAsync(string agentId, AgentConfigurationEntity config);
        Task<AgentUsageStatistics> GetUsageStatisticsAsync(string agentId, DateTime? from = null);
        Task IncrementUsageAsync(string agentId, int messageCount, long tokensUsed);
    }
    
    public class AgentRepository : Repository<AgentEntity>, IAgentRepository
    {
        public AgentRepository(OrchestratorDbContext context) : base(context)
        {
        }
        
        public async Task<AgentEntity> GetWithConfigurationAsync(string agentId)
        {
            return await _dbSet
                .Include(a => a.Configuration)
                .FirstOrDefaultAsync(a => a.Id == agentId);
        }
        
        public async Task<IEnumerable<AgentEntity>> GetActiveAgentsAsync()
        {
            return await _dbSet
                .Where(a => a.IsActive)
                .Include(a => a.Configuration)
                .OrderBy(a => a.Name)
                .ToListAsync();
        }
        
        public async Task<bool> UpdateConfigurationAsync(
            string agentId, 
            AgentConfigurationEntity config)
        {
            var agent = await GetWithConfigurationAsync(agentId);
            if (agent == null) return false;
            
            if (agent.Configuration == null)
            {
                config.AgentId = agentId;
                await _context.AgentConfigurations.AddAsync(config);
            }
            else
            {
                agent.Configuration.Model = config.Model;
                agent.Configuration.Temperature = config.Temperature;
                agent.Configuration.MaxTokens = config.MaxTokens;
                agent.Configuration.SystemPrompt = config.SystemPrompt;
                agent.Configuration.RequireApproval = config.RequireApproval;
                agent.Configuration.CustomSettingsJson = config.CustomSettingsJson;
                agent.Configuration.EnabledToolsJson = config.EnabledToolsJson;
                agent.Configuration.CapabilitiesJson = config.CapabilitiesJson;
                
                _context.AgentConfigurations.Update(agent.Configuration);
            }
            
            await _context.SaveChangesAsync();
            return true;
        }
        
        public async Task<AgentUsageStatistics> GetUsageStatisticsAsync(
            string agentId, 
            DateTime? from = null)
        {
            var query = _context.Messages
                .Where(m => m.AgentId == agentId);
            
            if (from.HasValue)
            {
                query = query.Where(m => m.Timestamp >= from.Value);
            }
            
            var messages = await query.ToListAsync();
            
            return new AgentUsageStatistics
            {
                AgentId = agentId,
                MessageCount = messages.Count,
                SessionCount = messages.Select(m => m.SessionId).Distinct().Count(),
                TotalTokensUsed = messages
                    .Where(m => !string.IsNullOrEmpty(m.TokenUsageJson))
                    .Select(m => JsonSerializer.Deserialize<TokenUsage>(m.TokenUsageJson))
                    .Sum(t => t?.TotalTokens ?? 0),
                ToolCallCount = await _context.ToolCalls
                    .CountAsync(t => messages.Select(m => m.Id).Contains(t.MessageId)),
                Period = from.HasValue ? DateTime.UtcNow - from.Value : null
            };
        }
        
        public async Task IncrementUsageAsync(string agentId, int messageCount, long tokensUsed)
        {
            var agent = await GetByIdAsync(agentId);
            if (agent == null) return;
            
            agent.TotalMessages += messageCount;
            agent.TotalTokensUsed += tokensUsed;
            agent.LastUsedAt = DateTime.UtcNow;
            
            await UpdateAsync(agent);
        }
    }
}
```

### Unit of Work

```csharp
namespace OrchestratorChat.Data
{
    public interface IUnitOfWork : IDisposable
    {
        ISessionRepository Sessions { get; }
        IAgentRepository Agents { get; }
        IRepository<MessageEntity> Messages { get; }
        IRepository<AttachmentEntity> Attachments { get; }
        IRepository<ToolCallEntity> ToolCalls { get; }
        IRepository<SessionSnapshotEntity> Snapshots { get; }
        IRepository<OrchestrationPlanEntity> OrchestrationPlans { get; }
        IRepository<OrchestrationStepEntity> OrchestrationSteps { get; }
        
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
    
    public class UnitOfWork : IUnitOfWork
    {
        private readonly OrchestratorDbContext _context;
        private IDbContextTransaction _transaction;
        
        public UnitOfWork(OrchestratorDbContext context)
        {
            _context = context;
            Sessions = new SessionRepository(context);
            Agents = new AgentRepository(context);
            Messages = new Repository<MessageEntity>(context);
            Attachments = new Repository<AttachmentEntity>(context);
            ToolCalls = new Repository<ToolCallEntity>(context);
            Snapshots = new Repository<SessionSnapshotEntity>(context);
            OrchestrationPlans = new Repository<OrchestrationPlanEntity>(context);
            OrchestrationSteps = new Repository<OrchestrationStepEntity>(context);
        }
        
        public ISessionRepository Sessions { get; }
        public IAgentRepository Agents { get; }
        public IRepository<MessageEntity> Messages { get; }
        public IRepository<AttachmentEntity> Attachments { get; }
        public IRepository<ToolCallEntity> ToolCalls { get; }
        public IRepository<SessionSnapshotEntity> Snapshots { get; }
        public IRepository<OrchestrationPlanEntity> OrchestrationPlans { get; }
        public IRepository<OrchestrationStepEntity> OrchestrationSteps { get; }
        
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
        
        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }
        
        public async Task CommitTransactionAsync()
        {
            await _transaction?.CommitAsync();
        }
        
        public async Task RollbackTransactionAsync()
        {
            await _transaction?.RollbackAsync();
        }
        
        public void Dispose()
        {
            _transaction?.Dispose();
            _context?.Dispose();
        }
    }
}
```

### Migrations

```csharp
// Initial migration command:
// dotnet ef migrations add InitialCreate -p OrchestratorChat.Data -s OrchestratorChat.Web
// dotnet ef database update -p OrchestratorChat.Data -s OrchestratorChat.Web

namespace OrchestratorChat.Data
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeAsync(OrchestratorDbContext context)
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();
            
            // Apply any pending migrations
            await context.Database.MigrateAsync();
            
            // Seed initial data if needed
            await SeedDataAsync(context);
        }
        
        private static async Task SeedDataAsync(OrchestratorDbContext context)
        {
            // Check if already seeded
            if (await context.Agents.AnyAsync())
                return;
            
            // Add default Claude agent
            var claudeAgent = new AgentEntity
            {
                Id = "default-claude",
                Name = "Claude Assistant",
                Type = AgentType.Claude,
                Description = "Default Claude agent for general assistance",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Configuration = new AgentConfigurationEntity
                {
                    Model = "claude-3-sonnet-20240229",
                    Temperature = 0.7,
                    MaxTokens = 4096,
                    SystemPrompt = "You are a helpful AI assistant.",
                    RequireApproval = false,
                    CapabilitiesJson = JsonSerializer.Serialize(new AgentCapabilities
                    {
                        SupportsStreaming = true,
                        SupportsTools = true,
                        SupportsFileOperations = true,
                        MaxTokens = 100000
                    })
                }
            };
            
            await context.Agents.AddAsync(claudeAgent);
            
            // Add default Saturn agent
            var saturnAgent = new AgentEntity
            {
                Id = "default-saturn",
                Name = "Saturn Developer",
                Type = AgentType.Saturn,
                Description = "Saturn agent for development tasks",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Configuration = new AgentConfigurationEntity
                {
                    Model = "claude-3-opus-20240229",
                    Temperature = 0.3,
                    MaxTokens = 8192,
                    SystemPrompt = "You are an expert software developer.",
                    RequireApproval = true,
                    CapabilitiesJson = JsonSerializer.Serialize(new AgentCapabilities
                    {
                        SupportsStreaming = true,
                        SupportsTools = true,
                        SupportsFileOperations = true,
                        MaxTokens = 100000
                    })
                }
            };
            
            await context.Agents.AddAsync(saturnAgent);
            await context.SaveChangesAsync();
        }
    }
}
```

### Configuration

```csharp
// In Program.cs or Startup.cs
services.AddDbContext<OrchestratorDbContext>(options =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=orchestrator.db";
    options.UseSqlite(connectionString);
    
    if (environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

services.AddScoped<IUnitOfWork, UnitOfWork>();

// Database initialization
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();
    await DatabaseInitializer.InitializeAsync(context);
}
```

## Performance Optimizations

### Indexing Strategy
- Session queries by status, project, and date
- Message queries by session and timestamp
- Agent queries by type and active status
- Full-text search on message content

### Caching
```csharp
services.AddMemoryCache();
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration.GetConnectionString("Redis");
});
```

### Query Optimization
- Use projection for read-only queries
- Implement pagination for large result sets
- Use compiled queries for hot paths
- Batch database operations when possible

## Testing

```csharp
[TestClass]
public class SessionRepositoryTests
{
    private OrchestratorDbContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<OrchestratorDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new OrchestratorDbContext(options);
    }
    
    [TestMethod]
    public async Task AddMessageAsync_ValidSession_ReturnsTrue()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var repository = new SessionRepository(context);
        
        var session = new SessionEntity { Name = "Test Session" };
        await repository.AddAsync(session);
        
        var message = new MessageEntity
        {
            Content = "Test message",
            Role = MessageRole.User
        };
        
        // Act
        var result = await repository.AddMessageAsync(session.Id, message);
        
        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(1, await context.Messages.CountAsync());
    }
}
```

## Next Steps
1. Create Entity Framework migrations
2. Implement repository interfaces
3. Set up unit of work pattern
4. Add caching layer
5. Create database seeders
6. Write integration tests

## Version History
- v1.0 - Initial specification
- Date: 2024-01-30
- Status: Ready for implementation