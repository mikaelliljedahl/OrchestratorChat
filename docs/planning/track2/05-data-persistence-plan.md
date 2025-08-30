# Data Persistence Implementation Plan

## Overview
The data persistence layer provides storage for chat sessions, messages, tool executions, and agent state. Currently, OrchestratorChat already has a basic Entity Framework setup. This document extends it with the comprehensive persistence system from SaturnFork.

## Current State vs Required State

### Current State (OrchestratorChat.Data)
- Basic Entity Framework with SQLite
- Simple entity models (Session, Message, Agent)
- Repository patterns ready but not implemented
- Basic database context

### Required State (from SaturnFork)
- Complete ChatHistoryRepository implementation
- Extended entity models with tool calls and metadata
- Session management with persistence
- Migration support for schema updates
- Query optimization and indexing

## Entity Models Enhancement

### 1. Enhanced Chat Session Model

#### 1.1 ChatSession Entity
**Location**: `src/OrchestratorChat.Saturn/Data/Models/ChatSession.cs`

```csharp
public class ChatSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;
    public string AgentId { get; set; }
    public string UserId { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Active;
    public string Model { get; set; }
    public string Provider { get; set; }
    
    // Navigation properties
    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    public virtual ICollection<ToolExecution> ToolExecutions { get; set; } = new List<ToolExecution>();
    public virtual SessionMetadata Metadata { get; set; }
    
    // Statistics
    public int TotalTokensUsed { get; set; }
    public decimal EstimatedCost { get; set; }
    public TimeSpan TotalDuration { get; set; }
}

public enum SessionStatus
{
    Active,
    Paused,
    Completed,
    Failed,
    Archived
}

public class SessionMetadata
{
    public string Id { get; set; }
    public string SessionId { get; set; }
    public Dictionary<string, object> CustomData { get; set; }
    public List<string> Tags { get; set; }
    public string Summary { get; set; }
    
    // Foreign key
    public virtual ChatSession Session { get; set; }
}
```

#### 1.2 Enhanced ChatMessage Entity
**Location**: `src/OrchestratorChat.Saturn/Data/Models/ChatMessage.cs`

```csharp
public class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; }
    public string Role { get; set; } // system, user, assistant, tool
    public string Content { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int TokenCount { get; set; }
    public int SequenceNumber { get; set; }
    
    // Tool-related
    public string ToolCallId { get; set; }
    public virtual ICollection<ToolCall> ToolCalls { get; set; } = new List<ToolCall>();
    
    // Metadata
    public string ParentMessageId { get; set; }
    public MessageStatus Status { get; set; } = MessageStatus.Completed;
    public string Error { get; set; }
    
    // Navigation properties
    public virtual ChatSession Session { get; set; }
    public virtual ChatMessage ParentMessage { get; set; }
    public virtual ICollection<ChatMessage> ChildMessages { get; set; } = new List<ChatMessage>();
    public virtual ICollection<MessageAttachment> Attachments { get; set; } = new List<MessageAttachment>();
}

public enum MessageStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Cancelled
}

public class MessageAttachment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string MessageId { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public long Size { get; set; }
    public byte[] Data { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    public virtual ChatMessage Message { get; set; }
}
```

#### 1.3 Tool Execution Models
**Location**: `src/OrchestratorChat.Saturn/Data/Models/ToolCall.cs`

```csharp
public class ToolCall
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string MessageId { get; set; }
    public string Name { get; set; }
    public string Arguments { get; set; } // JSON string
    public string Result { get; set; } // JSON string
    public bool Success { get; set; }
    public string Error { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan Duration { get; set; }
    
    public virtual ChatMessage Message { get; set; }
    public virtual ToolExecution Execution { get; set; }
}

public class ToolExecution
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; }
    public string ToolCallId { get; set; }
    public string ToolName { get; set; }
    public string Input { get; set; }
    public string Output { get; set; }
    public ExecutionStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public string AgentId { get; set; }
    
    // Approval tracking
    public bool RequiredApproval { get; set; }
    public bool? Approved { get; set; }
    public string ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    
    public virtual ChatSession Session { get; set; }
    public virtual ToolCall ToolCall { get; set; }
}

public enum ExecutionStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled,
    AwaitingApproval
}
```

### 2. Repository Implementation

#### 2.1 ChatHistoryRepository
**Location**: `src/OrchestratorChat.Saturn/Data/ChatHistoryRepository.cs`

```csharp
public interface IChatHistoryRepository
{
    // Session operations
    Task<ChatSession> CreateSessionAsync(ChatSession session);
    Task<ChatSession> GetSessionAsync(string sessionId);
    Task<IEnumerable<ChatSession>> GetSessionsAsync(string userId, int limit = 50);
    Task UpdateSessionAsync(ChatSession session);
    Task DeleteSessionAsync(string sessionId);
    Task<IEnumerable<ChatSession>> SearchSessionsAsync(string query, string userId);
    
    // Message operations
    Task<ChatMessage> AddMessageAsync(ChatMessage message);
    Task<IEnumerable<ChatMessage>> GetMessagesAsync(string sessionId, int limit = 100);
    Task<ChatMessage> GetMessageAsync(string messageId);
    Task UpdateMessageAsync(ChatMessage message);
    Task DeleteMessageAsync(string messageId);
    
    // Tool operations
    Task<ToolCall> AddToolCallAsync(ToolCall toolCall);
    Task<ToolExecution> AddToolExecutionAsync(ToolExecution execution);
    Task UpdateToolExecutionAsync(ToolExecution execution);
    Task<IEnumerable<ToolExecution>> GetToolExecutionsAsync(string sessionId);
    
    // Analytics
    Task<SessionStatistics> GetSessionStatisticsAsync(string sessionId);
    Task<UserStatistics> GetUserStatisticsAsync(string userId);
}

public class ChatHistoryRepository : IChatHistoryRepository
{
    private readonly OrchestratorChatDbContext _context;
    private readonly ILogger<ChatHistoryRepository> _logger;
    
    public ChatHistoryRepository(
        OrchestratorChatDbContext context,
        ILogger<ChatHistoryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<ChatSession> CreateSessionAsync(ChatSession session)
    {
        try
        {
            _context.ChatSessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create session");
            throw;
        }
    }
    
    public async Task<ChatSession> GetSessionAsync(string sessionId)
    {
        return await _context.ChatSessions
            .Include(s => s.Messages)
                .ThenInclude(m => m.ToolCalls)
            .Include(s => s.ToolExecutions)
            .Include(s => s.Metadata)
            .FirstOrDefaultAsync(s => s.Id == sessionId);
    }
    
    public async Task<IEnumerable<ChatSession>> GetSessionsAsync(string userId, int limit = 50)
    {
        return await _context.ChatSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LastModifiedAt)
            .Take(limit)
            .Include(s => s.Metadata)
            .ToListAsync();
    }
    
    public async Task<ChatMessage> AddMessageAsync(ChatMessage message)
    {
        // Get the next sequence number
        var lastSequence = await _context.ChatMessages
            .Where(m => m.SessionId == message.SessionId)
            .MaxAsync(m => (int?)m.SequenceNumber) ?? 0;
        
        message.SequenceNumber = lastSequence + 1;
        
        _context.ChatMessages.Add(message);
        
        // Update session timestamp
        var session = await _context.ChatSessions
            .FindAsync(message.SessionId);
        if (session != null)
        {
            session.LastModifiedAt = DateTime.UtcNow;
            session.TotalTokensUsed += message.TokenCount;
        }
        
        await _context.SaveChangesAsync();
        return message;
    }
    
    public async Task<IEnumerable<ChatMessage>> GetMessagesAsync(string sessionId, int limit = 100)
    {
        return await _context.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.SequenceNumber)
            .Take(limit)
            .Include(m => m.ToolCalls)
            .Include(m => m.Attachments)
            .ToListAsync();
    }
    
    public async Task<ToolExecution> AddToolExecutionAsync(ToolExecution execution)
    {
        _context.ToolExecutions.Add(execution);
        await _context.SaveChangesAsync();
        return execution;
    }
    
    public async Task UpdateToolExecutionAsync(ToolExecution execution)
    {
        execution.CompletedAt = DateTime.UtcNow;
        execution.Duration = execution.CompletedAt.Value - execution.StartedAt;
        
        _context.ToolExecutions.Update(execution);
        await _context.SaveChangesAsync();
    }
    
    public async Task<SessionStatistics> GetSessionStatisticsAsync(string sessionId)
    {
        var session = await GetSessionAsync(sessionId);
        
        return new SessionStatistics
        {
            SessionId = sessionId,
            TotalMessages = session.Messages.Count,
            TotalTokens = session.TotalTokensUsed,
            EstimatedCost = session.EstimatedCost,
            Duration = session.TotalDuration,
            ToolExecutions = session.ToolExecutions.Count,
            AverageResponseTime = CalculateAverageResponseTime(session.Messages)
        };
    }
    
    private TimeSpan CalculateAverageResponseTime(ICollection<ChatMessage> messages)
    {
        var responseTimes = new List<TimeSpan>();
        ChatMessage previousUserMessage = null;
        
        foreach (var message in messages.OrderBy(m => m.SequenceNumber))
        {
            if (message.Role == "user")
            {
                previousUserMessage = message;
            }
            else if (message.Role == "assistant" && previousUserMessage != null)
            {
                responseTimes.Add(message.Timestamp - previousUserMessage.Timestamp);
                previousUserMessage = null;
            }
        }
        
        return responseTimes.Any() 
            ? TimeSpan.FromMilliseconds(responseTimes.Average(t => t.TotalMilliseconds))
            : TimeSpan.Zero;
    }
}
```

#### 2.2 Statistics Models
**Location**: `src/OrchestratorChat.Saturn/Data/Models/Statistics.cs`

```csharp
public class SessionStatistics
{
    public string SessionId { get; set; }
    public int TotalMessages { get; set; }
    public int TotalTokens { get; set; }
    public decimal EstimatedCost { get; set; }
    public TimeSpan Duration { get; set; }
    public int ToolExecutions { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public Dictionary<string, int> MessagesByRole { get; set; }
    public Dictionary<string, int> ToolUsageCount { get; set; }
}

public class UserStatistics
{
    public string UserId { get; set; }
    public int TotalSessions { get; set; }
    public int TotalMessages { get; set; }
    public int TotalTokensUsed { get; set; }
    public decimal TotalCost { get; set; }
    public DateTime FirstSessionDate { get; set; }
    public DateTime LastSessionDate { get; set; }
    public Dictionary<string, int> ModelUsage { get; set; }
    public List<string> MostUsedTools { get; set; }
}
```

### 3. Database Context Enhancement

#### 3.1 Enhanced DbContext
**Location**: `src/OrchestratorChat.Data/OrchestratorChatDbContext.cs` (Enhancement)

```csharp
public class OrchestratorChatDbContext : DbContext
{
    public DbSet<ChatSession> ChatSessions { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<ToolCall> ToolCalls { get; set; }
    public DbSet<ToolExecution> ToolExecutions { get; set; }
    public DbSet<SessionMetadata> SessionMetadata { get; set; }
    public DbSet<MessageAttachment> MessageAttachments { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ChatSession configuration
        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.UserId, e.LastModifiedAt });
            
            entity.HasMany(e => e.Messages)
                .WithOne(m => m.Session)
                .HasForeignKey(m => m.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Metadata)
                .WithOne(m => m.Session)
                .HasForeignKey<SessionMetadata>(m => m.SessionId);
            
            // Store complex types as JSON
            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<SessionMetadata>(v, (JsonSerializerOptions)null));
        });
        
        // ChatMessage configuration
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.SessionId, e.SequenceNumber });
            entity.HasIndex(e => e.Timestamp);
            
            entity.HasOne(e => e.ParentMessage)
                .WithMany(m => m.ChildMessages)
                .HasForeignKey(e => e.ParentMessageId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasMany(e => e.ToolCalls)
                .WithOne(tc => tc.Message)
                .HasForeignKey(tc => tc.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // ToolCall configuration
        modelBuilder.Entity<ToolCall>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MessageId);
            entity.HasIndex(e => e.Name);
            
            entity.Property(e => e.Arguments)
                .HasMaxLength(8000); // Limit JSON size
            
            entity.Property(e => e.Result)
                .HasMaxLength(8000);
        });
        
        // ToolExecution configuration
        modelBuilder.Entity<ToolExecution>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.ToolName);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StartedAt);
            
            entity.HasOne(e => e.ToolCall)
                .WithOne(tc => tc.Execution)
                .HasForeignKey<ToolExecution>(e => e.ToolCallId);
        });
        
        // MessageAttachment configuration
        modelBuilder.Entity<MessageAttachment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MessageId);
            
            entity.Property(e => e.Data)
                .HasMaxLength(10485760); // 10MB max
        });
    }
}
```

### 4. Migration Support

#### 4.1 Initial Migration
**Location**: `src/OrchestratorChat.Data/Migrations/`

```csharp
public partial class AddSaturnDataModels : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add new columns to existing tables
        migrationBuilder.AddColumn<int>(
            name: "TokenCount",
            table: "ChatMessages",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);
        
        migrationBuilder.AddColumn<int>(
            name: "SequenceNumber",
            table: "ChatMessages",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);
        
        // Create new tables
        migrationBuilder.CreateTable(
            name: "ToolCalls",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", nullable: false),
                MessageId = table.Column<string>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Arguments = table.Column<string>(type: "TEXT", maxLength: 8000),
                Result = table.Column<string>(type: "TEXT", maxLength: 8000),
                Success = table.Column<bool>(type: "INTEGER", nullable: false),
                Error = table.Column<string>(type: "TEXT"),
                ExecutedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                Duration = table.Column<TimeSpan>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ToolCalls", x => x.Id);
                table.ForeignKey(
                    name: "FK_ToolCalls_ChatMessages_MessageId",
                    column: x => x.MessageId,
                    principalTable: "ChatMessages",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
        
        // Create indexes
        migrationBuilder.CreateIndex(
            name: "IX_ChatMessages_SessionId_SequenceNumber",
            table: "ChatMessages",
            columns: new[] { "SessionId", "SequenceNumber" });
        
        migrationBuilder.CreateIndex(
            name: "IX_ToolCalls_MessageId",
            table: "ToolCalls",
            column: "MessageId");
    }
    
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ToolCalls");
        migrationBuilder.DropColumn(name: "TokenCount", table: "ChatMessages");
        migrationBuilder.DropColumn(name: "SequenceNumber", table: "ChatMessages");
    }
}
```

### 5. Caching Layer

#### 5.1 Memory Cache Implementation
**Location**: `src/OrchestratorChat.Saturn/Data/Caching/SessionCache.cs`

```csharp
public interface ISessionCache
{
    Task<ChatSession> GetSessionAsync(string sessionId);
    void SetSession(ChatSession session);
    void InvalidateSession(string sessionId);
    void Clear();
}

public class SessionCache : ISessionCache
{
    private readonly IMemoryCache _cache;
    private readonly IChatHistoryRepository _repository;
    private readonly MemoryCacheEntryOptions _cacheOptions;
    
    public SessionCache(
        IMemoryCache cache,
        IChatHistoryRepository repository)
    {
        _cache = cache;
        _repository = repository;
        _cacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(15),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        };
    }
    
    public async Task<ChatSession> GetSessionAsync(string sessionId)
    {
        if (_cache.TryGetValue<ChatSession>($"session_{sessionId}", out var cached))
        {
            return cached;
        }
        
        var session = await _repository.GetSessionAsync(sessionId);
        if (session != null)
        {
            SetSession(session);
        }
        
        return session;
    }
    
    public void SetSession(ChatSession session)
    {
        _cache.Set($"session_{session.Id}", session, _cacheOptions);
    }
    
    public void InvalidateSession(string sessionId)
    {
        _cache.Remove($"session_{sessionId}");
    }
    
    public void Clear()
    {
        // Note: IMemoryCache doesn't have a Clear method
        // This would need custom implementation with tracking keys
    }
}
```

## Implementation Priority

### Phase 1: Entity Models (Day 1-2)
1. Enhance existing entity models
2. Add new entity classes
3. Update relationships and navigation properties

### Phase 2: Repository Implementation (Day 3-4)
1. Implement ChatHistoryRepository
2. Add query methods
3. Implement statistics calculations

### Phase 3: Database Context (Day 5)
1. Update DbContext configuration
2. Add indexes and constraints
3. Configure JSON serialization

### Phase 4: Migrations & Testing (Day 6-7)
1. Create and test migrations
2. Add caching layer
3. Performance optimization

## Testing Requirements

### Unit Tests
- Repository method testing with in-memory database
- Entity validation
- Statistics calculations
- Cache behavior

### Integration Tests
- Database operations with SQLite
- Migration testing
- Concurrent access scenarios
- Performance benchmarks

## Performance Considerations

1. **Indexing Strategy**:
   - Index on UserId for user queries
   - Composite index on SessionId + SequenceNumber
   - Index on timestamps for date ranges

2. **Query Optimization**:
   - Use projection for list views
   - Implement pagination
   - Lazy load related data when appropriate

3. **Caching**:
   - Cache active sessions
   - Cache user statistics
   - Implement cache invalidation

## Dependencies to Add

```xml
<PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0" />
```

## Validation Checklist

- [ ] Entity models enhanced
- [ ] Repository fully implemented
- [ ] Database context configured
- [ ] Migrations created and tested
- [ ] Caching layer operational
- [ ] Indexes optimized
- [ ] Unit tests passing
- [ ] Integration tests passing
- [ ] Performance benchmarks met