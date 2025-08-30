# SessionManager Test Scenarios

## Component Under Test: SessionManager
**Location**: `src/OrchestratorChat.Core/Sessions/SessionManager.cs`

---

## Test Categories

### 1. Session Creation Tests

#### Test: CreateSessionAsync_WithValidData_ReturnsNewSession
**Given**: Valid CreateSessionRequest with all required fields
**When**: CreateSessionAsync is called
**Then**: 
- Returns non-null Session
- Session has unique ID
- Session status is Active
- Session name matches request
- SessionCreatedEvent is published
- Session is persisted to database

#### Test: CreateSessionAsync_WithDuplicateName_HandlesGracefully
**Given**: Session with name "Test" already exists
**When**: CreateSessionAsync called with same name
**Then**:
- Either throws meaningful exception OR
- Creates session with modified name (e.g., "Test (2)")
- Logs warning about duplicate

#### Test: CreateSessionAsync_WithNullRequest_ThrowsArgumentNullException
**Given**: Null CreateSessionRequest
**When**: CreateSessionAsync is called
**Then**: Throws ArgumentNullException with parameter name

#### Test: CreateSessionAsync_WithEmptyName_ThrowsValidationException
**Given**: CreateSessionRequest with empty/whitespace name
**When**: CreateSessionAsync is called
**Then**: Throws ValidationException with meaningful message

#### Test: CreateSessionAsync_WithInvalidAgentIds_LogsWarning
**Given**: CreateSessionRequest with non-existent agent IDs
**When**: CreateSessionAsync is called
**Then**:
- Session is created
- Warning is logged for invalid agents
- Valid agents are added, invalid ones ignored

---

### 2. Session Retrieval Tests

#### Test: GetSessionAsync_WithExistingId_ReturnsSession
**Given**: Session with ID "session-123" exists
**When**: GetSessionAsync("session-123") is called
**Then**:
- Returns correct session
- Session includes all properties
- Related entities are loaded (Messages, Agents)

#### Test: GetSessionAsync_WithNonExistentId_ReturnsNull
**Given**: No session with ID "invalid-id" exists
**When**: GetSessionAsync("invalid-id") is called
**Then**:
- Returns null
- No exception thrown
- No error logged

#### Test: GetSessionAsync_WithNullId_ThrowsArgumentException
**Given**: Null session ID
**When**: GetSessionAsync(null) is called
**Then**: Throws ArgumentException

#### Test: GetCurrentSessionAsync_WithActiveSession_ReturnsCurrent
**Given**: A session is marked as current
**When**: GetCurrentSessionAsync is called
**Then**: Returns the current active session

#### Test: GetCurrentSessionAsync_WithNoActiveSession_ReturnsNull
**Given**: No current session set
**When**: GetCurrentSessionAsync is called
**Then**: Returns null

---

### 3. Session History Tests

#### Test: GetRecentSessions_WithMultipleSessions_ReturnsOrderedList
**Given**: 10 sessions exist with different timestamps
**When**: GetRecentSessions(5) is called
**Then**:
- Returns exactly 5 sessions
- Sessions ordered by LastActivityAt descending
- Each SessionSummary has required fields populated

#### Test: GetRecentSessions_WithFewerSessionsThanRequested_ReturnsAll
**Given**: Only 3 sessions exist
**When**: GetRecentSessions(10) is called
**Then**: Returns all 3 sessions

#### Test: GetRecentSessions_WithZeroCount_ReturnsEmptyList
**Given**: Any number of sessions
**When**: GetRecentSessions(0) is called
**Then**: Returns empty list

#### Test: GetRecentSessions_WithNegativeCount_ThrowsArgumentException
**Given**: Any state
**When**: GetRecentSessions(-1) is called
**Then**: Throws ArgumentException

---

### 4. Message Management Tests

#### Test: AddMessageAsync_ToExistingSession_AddsMessage
**Given**: Active session exists
**When**: AddMessageAsync called with valid message
**Then**:
- Message added to session
- Session.LastActivityAt updated
- MessageAddedEvent published
- Message persisted to database

#### Test: AddMessageAsync_ToNonExistentSession_ThrowsException
**Given**: Session ID doesn't exist
**When**: AddMessageAsync called
**Then**: Throws SessionNotFoundException

#### Test: AddMessageAsync_WithNullMessage_ThrowsArgumentNullException
**Given**: Valid session exists
**When**: AddMessageAsync called with null message
**Then**: Throws ArgumentNullException

#### Test: AddMessageAsync_ToCompletedSession_ThrowsInvalidOperationException
**Given**: Session with status Completed
**When**: AddMessageAsync called
**Then**: Throws InvalidOperationException("Cannot add message to completed session")

#### Test: AddMessageAsync_ExceedsMaxMessages_HandlesGracefully
**Given**: Session at maximum message capacity
**When**: AddMessageAsync called
**Then**:
- Either throws exception OR
- Removes oldest message (rolling window)
- Logs warning about limit

---

### 5. Session Lifecycle Tests

#### Test: EndSessionAsync_WithActiveSession_MarksCompleted
**Given**: Active session exists
**When**: EndSessionAsync called
**Then**:
- Session status changed to Completed
- EndedAt timestamp set
- SessionEndedEvent published
- Related resources cleaned up

#### Test: EndSessionAsync_WithAlreadyEndedSession_NoOp
**Given**: Session already completed
**When**: EndSessionAsync called again
**Then**:
- No changes made
- No events published
- Returns success

#### Test: EndSessionAsync_WithNonExistentSession_ReturnsFalse
**Given**: Session doesn't exist
**When**: EndSessionAsync called
**Then**:
- Returns false
- No exception thrown
- Warning logged

---

### 6. Context Management Tests

#### Test: UpdateSessionContextAsync_WithValidData_UpdatesContext
**Given**: Active session exists
**When**: UpdateSessionContextAsync called with dictionary
**Then**:
- Context updated with new values
- Existing values preserved if not in update
- Change persisted to database

#### Test: UpdateSessionContextAsync_WithNullContext_ClearsContext
**Given**: Session with existing context
**When**: UpdateSessionContextAsync called with null
**Then**: Context cleared

---

### 7. Concurrency Tests

#### Test: ConcurrentCreateSessions_HandlesRaceCondition
**Given**: Multiple threads
**When**: All call CreateSessionAsync simultaneously
**Then**:
- All sessions created successfully
- No ID collisions
- All events published

#### Test: ConcurrentAddMessages_MaintainsOrder
**Given**: Active session
**When**: Multiple threads add messages simultaneously
**Then**:
- All messages added
- Order preserved based on timestamp
- No messages lost

---

### 8. Error Handling Tests

#### Test: CreateSessionAsync_WhenDatabaseUnavailable_ThrowsDataException
**Given**: Database connection failed
**When**: CreateSessionAsync called
**Then**:
- Throws DataException with inner exception
- Error logged with details
- No partial data saved

#### Test: EventPublishFailure_DoesNotPreventSessionCreation
**Given**: EventBus.PublishAsync throws
**When**: CreateSessionAsync called
**Then**:
- Session still created
- Error logged
- Returns session successfully

---

### 9. Performance Tests

#### Test: CreateSessionAsync_Performance_Under100ms
**Given**: Normal conditions
**When**: CreateSessionAsync called
**Then**: Completes in under 100ms

#### Test: GetRecentSessions_With1000Sessions_PerformsEfficiently
**Given**: 1000 sessions in database
**When**: GetRecentSessions(20) called
**Then**:
- Returns in under 50ms
- Uses efficient query (no N+1)

---

### 10. Integration Tests

#### Test: SessionLifecycle_CompleteFlow_WorksEndToEnd
**Given**: Clean state
**When**: Create -> Add Messages -> Update Context -> End
**Then**: All operations succeed and persist

#### Test: SessionWithAgents_Integration_ProperlyLinked
**Given**: Agents exist
**When**: Session created with agent IDs
**Then**: Session properly linked to agents in database

---

## Test Data Builders

```csharp
public class SessionTestDataBuilder
{
    public CreateSessionRequest BuildValidRequest() => new()
    {
        Name = $"Test Session {Guid.NewGuid()}",
        Type = SessionType.MultiAgent,
        AgentIds = new List<string> { "agent-1", "agent-2" },
        WorkingDirectory = @"C:\test"
    };
    
    public Session BuildActiveSession() => new()
    {
        Id = Guid.NewGuid().ToString(),
        Name = "Active Session",
        Status = SessionStatus.Active,
        CreatedAt = DateTime.UtcNow,
        Messages = new List<AgentMessage>()
    };
    
    public AgentMessage BuildValidMessage() => new()
    {
        Content = "Test message",
        Role = MessageRole.User,
        Timestamp = DateTime.UtcNow
    };
}
```

---

## Mock Setup Examples

```csharp
public class SessionManagerTestBase
{
    protected Mock<IEventBus> EventBusMock;
    protected Mock<ILogger<SessionManager>> LoggerMock;
    protected OrchestratorDbContext DbContext;
    
    public SessionManagerTestBase()
    {
        EventBusMock = new Mock<IEventBus>();
        LoggerMock = new Mock<ILogger<SessionManager>>();
        
        var options = new DbContextOptionsBuilder<OrchestratorDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        DbContext = new OrchestratorDbContext(options);
    }
}
```

---

## Assertion Examples

```csharp
// Fluent Assertions
result.Should().NotBeNull();
result.Name.Should().Be(expected);
result.Status.Should().Be(SessionStatus.Active);

// Event verification
EventBusMock.Verify(x => x.PublishAsync(
    It.Is<SessionCreatedEvent>(e => e.SessionId == result.Id)), 
    Times.Once);

// Exception assertions
var act = () => manager.CreateSessionAsync(null);
await act.Should().ThrowAsync<ArgumentNullException>()
    .WithParameterName("request");
```

---

## Test Coverage Matrix

| Method | Happy Path | Error Cases | Edge Cases | Concurrency | Performance |
|--------|------------|-------------|------------|-------------|-------------|
| CreateSessionAsync | ✅ | ✅ | ✅ | ✅ | ✅ |
| GetSessionAsync | ✅ | ✅ | ✅ | ⚠️ | ✅ |
| GetCurrentSessionAsync | ✅ | ✅ | ✅ | ⚠️ | ⚠️ |
| GetRecentSessions | ✅ | ✅ | ✅ | ⚠️ | ✅ |
| AddMessageAsync | ✅ | ✅ | ✅ | ✅ | ⚠️ |
| EndSessionAsync | ✅ | ✅ | ✅ | ⚠️ | ⚠️ |
| UpdateSessionContextAsync | ✅ | ✅ | ✅ | ✅ | ⚠️ |

Legend: ✅ Required | ⚠️ Optional | ❌ Not Needed