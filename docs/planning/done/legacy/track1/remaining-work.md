# Track 1: Core & Data Layer - Remaining Work

## Status: 60% Complete - CRITICAL BLOCKERS

### Developer: Core & Data Team
### Priority: HIGHEST - Blocking all other tracks
### Estimated Time: 1-2 days

---

## 🔴 CRITICAL: Missing Service Implementations

These implementations are **blocking the entire solution** from compiling and running.

### 1. SessionManager Implementation
**File to Create**: `src/OrchestratorChat.Core/Sessions/SessionManager.cs`

```csharp
using OrchestratorChat.Core.Abstractions;
using OrchestratorChat.Core.Models;
using OrchestratorChat.Data;
using Microsoft.EntityFrameworkCore;

namespace OrchestratorChat.Core.Sessions;

public class SessionManager : ISessionManager
{
    private readonly OrchestratorDbContext _context;
    private readonly IEventBus _eventBus;
    private Session? _currentSession;

    public SessionManager(OrchestratorDbContext context, IEventBus eventBus)
    {
        _context = context;
        _eventBus = eventBus;
    }

    // REQUIRED METHODS TO IMPLEMENT:
    
    public async Task<Session> CreateSessionAsync(CreateSessionRequest request)
    {
        // TODO: Implement
        // 1. Create new Session entity
        // 2. Add participant agents
        // 3. Save to database
        // 4. Publish SessionCreatedEvent
        // 5. Return created session
    }

    public async Task<Session?> GetSessionAsync(string sessionId)
    {
        // TODO: Implement
        // 1. Query database for session
        // 2. Include related entities (Messages, Agents)
        // 3. Return session or null
    }

    public async Task<Session?> GetCurrentSessionAsync()
    {
        // TODO: Implement
        // Return _currentSession or load from context
    }

    public async Task<List<SessionSummary>> GetRecentSessions(int count)
    {
        // TODO: Implement
        // 1. Query database for recent sessions
        // 2. Project to SessionSummary DTO
        // 3. Order by LastActivityAt descending
        // 4. Take(count)
    }

    public async Task<bool> EndSessionAsync(string sessionId)
    {
        // TODO: Implement
        // 1. Find session
        // 2. Update status to Completed
        // 3. Save changes
        // 4. Publish SessionEndedEvent
    }

    public async Task AddMessageAsync(string sessionId, ChatMessage message)
    {
        // TODO: Implement
        // 1. Find session
        // 2. Add message to Messages collection
        // 3. Update LastActivityAt
        // 4. Save changes
        // 5. Publish MessageAddedEvent
    }

    public async Task UpdateSessionContextAsync(string sessionId, Dictionary<string, object> context)
    {
        // TODO: Implement
        // 1. Find session
        // 2. Update Context property
        // 3. Save changes
    }
}
```

### 2. Orchestrator Implementation
**File to Create**: `src/OrchestratorChat.Core/Orchestration/Orchestrator.cs`

```csharp
using OrchestratorChat.Core.Abstractions;
using OrchestratorChat.Core.Models;
using OrchestratorChat.Core.Agents;

namespace OrchestratorChat.Core.Orchestration;

public class Orchestrator : IOrchestrator
{
    private readonly IAgentFactory _agentFactory;
    private readonly IEventBus _eventBus;
    private readonly ILogger<Orchestrator> _logger;
    private readonly Dictionary<string, OrchestrationExecution> _activeExecutions;

    public Orchestrator(IAgentFactory agentFactory, IEventBus eventBus, ILogger<Orchestrator> logger)
    {
        _agentFactory = agentFactory;
        _eventBus = eventBus;
        _logger = logger;
        _activeExecutions = new Dictionary<string, OrchestrationExecution>();
    }

    // REQUIRED METHODS TO IMPLEMENT:

    public async Task<OrchestrationPlan> CreatePlanAsync(OrchestrationRequest request)
    {
        // TODO: Implement
        // 1. Analyze the goal
        // 2. Determine required agents and their capabilities
        // 3. Create execution steps based on strategy
        // 4. Handle dependencies between steps
        // 5. Return the plan
    }

    public async Task<OrchestrationResult> ExecutePlanAsync(
        OrchestrationPlan plan, 
        IProgress<OrchestrationProgress>? progress = null)
    {
        // TODO: Implement
        // 1. Validate plan
        // 2. Initialize agents
        // 3. Execute steps according to strategy (Sequential/Parallel/Adaptive)
        // 4. Report progress
        // 5. Handle failures and retry logic
        // 6. Collect results
        // 7. Return final result
    }

    public async Task<bool> CancelExecutionAsync(string executionId)
    {
        // TODO: Implement
        // 1. Find active execution
        // 2. Cancel all running tasks
        // 3. Clean up resources
        // 4. Publish cancellation event
    }

    public async Task<OrchestrationStatus> GetExecutionStatusAsync(string executionId)
    {
        // TODO: Implement
        // 1. Find execution
        // 2. Return current status
    }

    private async Task ExecuteStepAsync(OrchestrationStep step, OrchestrationContext context)
    {
        // TODO: Implement step execution logic
    }

    private async Task<bool> CheckDependencies(OrchestrationStep step, OrchestrationContext context)
    {
        // TODO: Check if all dependencies are satisfied
    }
}
```

### 3. EventBus Implementation
**File to Create**: `src/OrchestratorChat.Core/Events/EventBus.cs`

```csharp
using OrchestratorChat.Core.Abstractions;

namespace OrchestratorChat.Core.Events;

public class EventBus : IEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers;
    private readonly ILogger<EventBus> _logger;

    public EventBus(ILogger<EventBus> logger)
    {
        _logger = logger;
        _handlers = new Dictionary<Type, List<Delegate>>();
    }

    // REQUIRED METHODS TO IMPLEMENT:

    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent
    {
        // TODO: Implement
        // 1. Get or create handler list for event type
        // 2. Add handler to list
    }

    public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent
    {
        // TODO: Implement
        // 1. Find handler list
        // 2. Remove handler
    }

    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent
    {
        // TODO: Implement
        // 1. Find handlers for event type
        // 2. Execute each handler asynchronously
        // 3. Handle exceptions
        // 4. Log event publication
    }

    public void Publish<TEvent>(TEvent @event) where TEvent : IEvent
    {
        // TODO: Implement synchronous version
    }
}
```

---

## 🟡 Model Property Additions

### OrchestrationPlan Model Updates
**File**: `src/OrchestratorChat.Core/Orchestration/OrchestrationPlan.cs`

Add these properties:
```csharp
public string Goal { get; set; } = string.Empty;
public OrchestrationStrategy Strategy { get; set; }
public List<OrchestrationStep> Steps { get; set; } = new();
public string Name { get; set; } = string.Empty;
```

### OrchestrationStep Model Updates
**File**: `src/OrchestratorChat.Core/Orchestration/OrchestrationStep.cs`

Add these properties:
```csharp
public string Description { get; set; } = string.Empty;
public string AssignedAgentId { get; set; } = string.Empty;
public TimeSpan ExpectedDuration { get; set; }
```

### ChatMessage Model Updates
**File**: `src/OrchestratorChat.Core/Models/ChatMessage.cs`

Add this property:
```csharp
public string? SenderId { get; set; }
```

---

## 🟢 Already Completed
- ✅ Core abstractions and interfaces
- ✅ Database context and entities
- ✅ Repository pattern implementation
- ✅ Configuration models
- ✅ Basic DTOs

---

## 📋 Testing Requirements

Once implementations are complete, create unit tests:

1. **SessionManagerTests**
   - Test session creation
   - Test message addition
   - Test session retrieval
   - Test concurrent session handling

2. **OrchestratorTests**
   - Test plan creation
   - Test sequential execution
   - Test parallel execution
   - Test failure handling
   - Test cancellation

3. **EventBusTests**
   - Test subscription/unsubscription
   - Test event publication
   - Test multiple handlers
   - Test exception handling

---

## 🔧 Configuration Updates

Update `appsettings.json` in Web project:
```json
{
  "Orchestration": {
    "MaxConcurrentAgents": 10,
    "DefaultTimeout": 300000,
    "RetryPolicy": {
      "MaxRetries": 3,
      "RetryDelay": 1000
    }
  },
  "Session": {
    "MaxMessagesPerSession": 1000,
    "SessionTimeout": 3600000,
    "AutoSaveInterval": 30000
  }
}
```

---

## 🚨 Blocking Issues for Other Tracks

Your work is blocking:
- **Track 3 (Web UI)**: Cannot compile without SessionManager and model properties
- **Track 4 (SignalR)**: Cannot implement hubs without Orchestrator
- **Track 2 (Agents)**: Need IAgentFactory in Core namespace

---

## 📞 Coordination Points

- Coordinate with Track 2 on IAgentFactory interface location
- Coordinate with Track 3 on exact property requirements
- Coordinate with Track 4 on event types needed

---

## ✅ Definition of Done

- [ ] SessionManager fully implemented with all interface methods
- [ ] Orchestrator fully implemented with plan creation and execution
- [ ] EventBus fully implemented with pub/sub functionality
- [ ] All model properties added
- [ ] Unit tests written and passing
- [ ] Solution builds without errors
- [ ] Integration tested with Web UI

---

## 📝 Notes

- Use async/await throughout for I/O operations
- Implement proper error handling and logging
- Consider using MediatR for event handling if preferred
- Ensure thread-safety in EventBus implementation
- Add cancellation token support where appropriate