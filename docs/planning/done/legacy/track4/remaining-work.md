# Track 4: SignalR & Orchestration - Remaining Work

## Status: 50% Complete - NEEDS ENGINE IMPLEMENTATION

### Developer: SignalR Team
### Priority: HIGH - Critical for real-time features
### Estimated Time: 1-1.5 days

---

## 🔴 CRITICAL: Orchestration Engine Missing

The SignalR hubs are defined but have no orchestration engine to execute plans.

### 1. Complete OrchestratorHub Implementation
**File**: `src/OrchestratorChat.SignalR/Hubs/OrchestratorHub.cs`

Current state has method signatures but no implementation. Complete these:

```csharp
public async Task<SessionCreatedResponse> CreateSession(CreateSessionRequest request)
{
    // TODO: IMPLEMENT
    // 1. Validate request
    // 2. Create session via ISessionManager
    // 3. Initialize requested agents via IAgentFactory
    // 4. Join user to SignalR group for session
    // 5. Notify all clients in session
    // 6. Return response with session details
    
    try
    {
        // Validate
        if (string.IsNullOrEmpty(request.Name))
            throw new ArgumentException("Session name required");
        
        // Create session
        var session = await _sessionManager.CreateSessionAsync(request);
        
        // Join SignalR group
        await Groups.AddToGroupAsync(Context.ConnectionId, session.Id);
        
        // Initialize agents
        foreach (var agentId in request.AgentIds)
        {
            var agent = await _agentFactory.GetAgentAsync(agentId);
            if (agent != null)
            {
                await agent.JoinSessionAsync(session.Id);
            }
        }
        
        // Notify clients
        await Clients.Group(session.Id).SessionCreated(session);
        
        return new SessionCreatedResponse
        {
            SessionId = session.Id,
            Success = true,
            Session = session
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to create session");
        return new SessionCreatedResponse
        {
            Success = false,
            ErrorMessage = ex.Message
        };
    }
}

public async Task<OrchestrationPlan> CreateOrchestrationPlan(CreatePlanRequest request)
{
    // TODO: IMPLEMENT
    // 1. Validate request
    // 2. Use IOrchestrator to create plan
    // 3. Store plan in session context
    // 4. Notify clients of plan creation
    // 5. Return plan
    
    var orchRequest = new OrchestrationRequest
    {
        Goal = request.Goal,
        Strategy = request.Strategy,
        AvailableAgentIds = request.AvailableAgentIds,
        SessionId = request.SessionId
    };
    
    var plan = await _orchestrator.CreatePlanAsync(orchRequest);
    
    // Notify clients in session
    await Clients.Group(request.SessionId).OrchestrationPlanCreated(plan);
    
    return plan;
}

public async Task ExecutePlan(string planId)
{
    // TODO: IMPLEMENT
    // 1. Retrieve plan
    // 2. Create progress reporter
    // 3. Execute via IOrchestrator
    // 4. Stream progress to clients
    // 5. Handle completion/failure
    
    var progress = new Progress<OrchestrationProgress>(async p =>
    {
        // Stream progress to clients
        await Clients.Caller.OrchestrationProgress(p);
    });
    
    var result = await _orchestrator.ExecutePlanAsync(plan, progress);
    
    // Send completion
    await Clients.Caller.OrchestrationCompleted(result);
}
```

### 2. Complete AgentHub Implementation
**File**: `src/OrchestratorChat.SignalR/Hubs/AgentHub.cs`

```csharp
public async Task SendAgentMessage(AgentMessageRequest request)
{
    // TODO: IMPLEMENT
    // 1. Validate request
    // 2. Get agent from factory
    // 3. Send message to agent
    // 4. Handle streaming response
    // 5. Broadcast to session group
    
    try
    {
        var agent = await _agentFactory.GetAgentAsync(request.AgentId);
        if (agent == null)
        {
            await Clients.Caller.ReceiveError(new ErrorResponse
            {
                Code = "AGENT_NOT_FOUND",
                Message = $"Agent {request.AgentId} not found"
            });
            return;
        }
        
        // Create agent message
        var agentMessage = new AgentMessage
        {
            Content = request.Content,
            SessionId = request.SessionId,
            Attachments = request.Attachments
        };
        
        // Process with streaming
        await foreach (var response in agent.ProcessMessageStreamAsync(agentMessage))
        {
            var dto = new AgentResponseDto
            {
                AgentId = request.AgentId,
                Response = response,
                SessionId = request.SessionId
            };
            
            // Stream to all clients in session
            await Clients.Group(request.SessionId).ReceiveStreamingUpdate(dto);
        }
        
        // Send completion
        await Clients.Group(request.SessionId).ReceiveAgentResponse(new AgentResponseDto
        {
            AgentId = request.AgentId,
            Response = new AgentResponse { IsComplete = true }
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing agent message");
        await Clients.Caller.ReceiveError(new ErrorResponse
        {
            Code = "PROCESSING_ERROR",
            Message = ex.Message
        });
    }
}

public async Task ExecuteTool(ToolExecutionRequest request)
{
    // TODO: IMPLEMENT
    // 1. Get agent
    // 2. Check if tool requires approval
    // 3. Execute tool via agent
    // 4. Return result
    
    var agent = await _agentFactory.GetAgentAsync(request.AgentId);
    if (agent == null)
        throw new InvalidOperationException("Agent not found");
    
    // Notify start
    await Clients.Caller.ToolExecutionStarted(new ToolExecutionUpdate
    {
        ToolName = request.ToolName,
        Status = "Starting",
        AgentId = request.AgentId
    });
    
    // Check approval
    if (request.RequireApproval)
    {
        var approvalRequest = new ToolApprovalRequest
        {
            Id = Guid.NewGuid().ToString(),
            ToolName = request.ToolName,
            Parameters = request.Parameters,
            AgentId = request.AgentId
        };
        
        await Clients.Caller.ToolApprovalRequired(approvalRequest);
        // Wait for approval (implement approval mechanism)
    }
    
    // Execute
    var result = await agent.ExecuteToolAsync(request.ToolName, request.Parameters);
    
    // Send result
    await Clients.Caller.ToolExecutionCompleted(new ToolExecutionResult
    {
        Success = result.Success,
        Output = result.Output,
        ExecutionTime = result.ExecutionTime
    });
}
```

---

## 🟡 Message Routing Implementation

### Create Message Router
**File to Create**: `src/OrchestratorChat.SignalR/Services/MessageRouter.cs`

```csharp
using OrchestratorChat.Core.Abstractions;

namespace OrchestratorChat.SignalR.Services;

public class MessageRouter : IMessageRouter
{
    private readonly IHubContext<AgentHub> _agentHubContext;
    private readonly IHubContext<OrchestratorHub> _orchestratorHubContext;
    private readonly ILogger<MessageRouter> _logger;
    
    public MessageRouter(
        IHubContext<AgentHub> agentHubContext,
        IHubContext<OrchestratorHub> orchestratorHubContext,
        ILogger<MessageRouter> logger)
    {
        _agentHubContext = agentHubContext;
        _orchestratorHubContext = orchestratorHubContext;
        _logger = logger;
    }
    
    // IMPLEMENT:
    
    public async Task RouteAgentMessageAsync(string sessionId, AgentMessage message)
    {
        // Route message to appropriate clients in session
        await _agentHubContext.Clients.Group(sessionId)
            .SendAsync("ReceiveAgentMessage", message);
    }
    
    public async Task RouteOrchestrationUpdateAsync(string sessionId, OrchestrationProgress progress)
    {
        // Route orchestration updates
        await _orchestratorHubContext.Clients.Group(sessionId)
            .SendAsync("OrchestrationProgress", progress);
    }
    
    public async Task BroadcastToSessionAsync(string sessionId, string method, object data)
    {
        // Generic broadcast to session
        await _agentHubContext.Clients.Group(sessionId)
            .SendAsync(method, data);
    }
}
```

---

## 🟢 Connection Management Enhancement

### Enhance ConnectionManager
**File**: `src/OrchestratorChat.SignalR/Services/ConnectionManager.cs`

Add missing functionality:

```csharp
public class ConnectionManager
{
    // ADD these methods:
    
    public async Task<bool> AddUserToSessionAsync(string connectionId, string sessionId)
    {
        // Track user-session mapping
        if (!_userSessions.ContainsKey(connectionId))
        {
            _userSessions[connectionId] = new HashSet<string>();
        }
        
        _userSessions[connectionId].Add(sessionId);
        
        // Add to SignalR group
        // Note: This needs IHubContext injection
        
        return true;
    }
    
    public async Task<bool> RemoveUserFromSessionAsync(string connectionId, string sessionId)
    {
        // Remove user from session
        if (_userSessions.ContainsKey(connectionId))
        {
            _userSessions[connectionId].Remove(sessionId);
            
            if (_userSessions[connectionId].Count == 0)
            {
                _userSessions.Remove(connectionId);
            }
        }
        
        return true;
    }
    
    public List<string> GetUserSessions(string connectionId)
    {
        // Get all sessions for a user
        return _userSessions.ContainsKey(connectionId) 
            ? _userSessions[connectionId].ToList() 
            : new List<string>();
    }
    
    public List<string> GetSessionUsers(string sessionId)
    {
        // Get all users in a session
        return _userSessions
            .Where(kvp => kvp.Value.Contains(sessionId))
            .Select(kvp => kvp.Key)
            .ToList();
    }
}
```

---

## 🔧 Real-time Event Handling

### Create Event Handlers
**Directory**: `src/OrchestratorChat.SignalR/EventHandlers/`

#### AgentEventHandler.cs
```csharp
public class AgentEventHandler : IEventHandler<AgentMessageEvent>
{
    private readonly IHubContext<AgentHub> _hubContext;
    
    public async Task HandleAsync(AgentMessageEvent @event)
    {
        // Forward agent events to SignalR clients
        await _hubContext.Clients.Group(@event.SessionId)
            .SendAsync("AgentMessage", @event.Message);
    }
}
```

#### OrchestrationEventHandler.cs
```csharp
public class OrchestrationEventHandler : IEventHandler<OrchestrationStepCompletedEvent>
{
    private readonly IHubContext<OrchestratorHub> _hubContext;
    
    public async Task HandleAsync(OrchestrationStepCompletedEvent @event)
    {
        // Forward orchestration events
        await _hubContext.Clients.Group(@event.SessionId)
            .SendAsync("StepCompleted", @event.Step);
    }
}
```

---

## 📋 Testing Requirements

### 1. Hub Connection Tests
- Test client connection/disconnection
- Test group join/leave
- Test reconnection handling
- Test connection state management

### 2. Message Flow Tests
- Test agent message routing
- Test orchestration updates
- Test broadcasting to groups
- Test error propagation

### 3. Concurrency Tests
- Test multiple concurrent sessions
- Test multiple agents per session
- Test race conditions
- Test connection limits

### 4. Integration Tests
- Test with real agents
- Test with Web UI
- Test error scenarios

---

## 🚨 Dependencies on Other Tracks

### From Track 1 (Core):
- ✋ **BLOCKED**: Need IOrchestrator implementation
- ✋ **BLOCKED**: Need ISessionManager implementation
- Need IEventBus for event handling

### From Track 2 (Agents):
- Need IAgentFactory in Core namespace
- Need agent streaming capability
- Need tool execution support

### From Track 3 (Web UI):
- UI expects specific hub methods
- UI expects streaming updates
- UI expects progress reporting

---

## 📞 Integration Points

### 1. SignalR Groups
- Session-based groups for isolation
- Agent-specific groups for direct communication
- Broadcast groups for notifications

### 2. Streaming
- Implement IAsyncEnumerable for streaming
- Handle backpressure
- Buffer management

### 3. Error Handling
- Graceful disconnection handling
- Automatic reconnection
- Error propagation to clients

---

## ✅ Definition of Done

- [ ] OrchestratorHub fully implemented
- [ ] AgentHub fully implemented
- [ ] Message routing working
- [ ] Connection management complete
- [ ] Event handlers wired up
- [ ] Streaming responses working
- [ ] Error handling robust
- [ ] Groups/sessions working
- [ ] Integration tested with Web UI

---

## 📝 Implementation Notes

### SignalR Best Practices
1. **Use Groups for Session Isolation**
   - Each session is a SignalR group
   - Prevents message leakage between sessions

2. **Implement Heartbeat**
   - Keep connections alive
   - Detect stale connections
   - Clean up resources

3. **Handle Reconnection**
   - Preserve state on reconnect
   - Rejoin groups automatically
   - Resume message streams

4. **Optimize Message Size**
   - Don't send unnecessary data
   - Compress large payloads
   - Batch small messages

### Performance Considerations
1. **Connection Pooling**
   - Limit concurrent connections
   - Reuse connections where possible

2. **Message Queuing**
   - Buffer messages during high load
   - Implement backpressure
   - Priority queues for important messages

3. **Scaling**
   - Redis backplane for scale-out
   - Sticky sessions for stateful connections
   - Load balancing strategies

### Security Considerations
1. **Authentication**
   - Validate JWT tokens
   - Authorize hub methods
   - Session-based permissions

2. **Input Validation**
   - Sanitize all inputs
   - Validate message size
   - Rate limiting

3. **Data Isolation**
   - Enforce session boundaries
   - Validate agent access
   - Audit logging

---

## 🔥 Quick Start After Dependencies

Once Track 1 implements core services:

1. Uncomment service references in hubs
2. Test basic hub connections
3. Implement message flow
4. Test with Web UI
5. Add error handling
6. Performance tune