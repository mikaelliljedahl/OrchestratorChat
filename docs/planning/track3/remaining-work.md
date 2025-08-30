# Track 3: Web UI - Remaining Work

## Status: 35% Complete - COMPILATION ERRORS

### Developer: Web UI Team
### Priority: MEDIUM - Blocked by Track 1
### Estimated Time: 0.5-1 day (after Track 1 completes)

---

## 🔴 BLOCKED: Waiting for Track 1

**Cannot proceed until Track 1 implements:**
- ✋ SessionManager implementation
- ✋ Orchestrator implementation
- ✋ EventBus implementation
- ✋ Model property additions

---

## 🟡 Compilation Errors to Fix

### Current Error Count: 33 errors

Once Track 1 completes their work, these errors should resolve. However, you may need to:

### 1. Update Service Registrations
**File**: `src/OrchestratorChat.Web/Program.cs`

After Track 1 implements services, uncomment and verify:
```csharp
// Lines 33-36 - Uncomment when services exist:
builder.Services.AddScoped<IEventBus, EventBus>();
builder.Services.AddScoped<ISessionManager, SessionManager>();
builder.Services.AddScoped<IOrchestrator, Orchestrator>();

// Verify these registrations work
```

### 2. Fix MudBlazor Timeline Components
**File**: `src/OrchestratorChat.Web/Components/OrchestrationTimeline.razor`

Replace non-existent MudBlazor components:
```razor
@* Lines 14-17 - Replace with valid MudBlazor components *@
@* MudTimelineOpposite doesn't exist in MudBlazor *@

<!-- CURRENT (BROKEN): -->
<MudTimelineOpposite>
    <MudText>@step.Duration</MudText>
</MudTimelineOpposite>

<!-- REPLACE WITH: -->
<div class="timeline-opposite">
    <MudText Typo="Typo.caption">@step.Duration</MudText>
</div>

<!-- CURRENT (BROKEN): -->
<MudTimelineContent>
    <MudCard>
        <!-- content -->
    </MudCard>
</MudTimelineContent>

<!-- REPLACE WITH: -->
<div class="timeline-content">
    <MudCard>
        <!-- content -->
    </MudCard>
</div>
```

Add CSS for timeline styling:
```css
/* Add to wwwroot/css/app.css */
.timeline-opposite {
    flex: 0 0 100px;
    text-align: right;
    padding-right: 1rem;
}

.timeline-content {
    flex: 1;
    padding-left: 1rem;
}
```

### 3. Create Missing SessionService
**File to Create**: `src/OrchestratorChat.Web/Services/SessionService.cs`

```csharp
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Web.Models;

namespace OrchestratorChat.Web.Services;

public class SessionService : ISessionService
{
    private readonly ISessionManager _sessionManager;
    private readonly ILogger<SessionService> _logger;

    public SessionService(ISessionManager sessionManager, ILogger<SessionService> logger)
    {
        _sessionManager = sessionManager;
        _logger = logger;
    }

    public async Task<Session?> GetCurrentSessionAsync()
    {
        // Delegate to SessionManager
        return await _sessionManager.GetCurrentSessionAsync();
    }

    public async Task<List<SessionSummary>> GetRecentSessionsAsync(int count = 20)
    {
        // Get from SessionManager and convert to view models
        var sessions = await _sessionManager.GetRecentSessions(count);
        return sessions; // Assuming compatible types
    }

    public async Task<Session> CreateSessionAsync(string name, List<string> agentIds)
    {
        var request = new CreateSessionRequest
        {
            Name = name,
            AgentIds = agentIds,
            Type = SessionType.MultiAgent
        };
        
        return await _sessionManager.CreateSessionAsync(request);
    }

    public async Task EndSessionAsync(string sessionId)
    {
        await _sessionManager.EndSessionAsync(sessionId);
    }
}

public interface ISessionService
{
    Task<Session?> GetCurrentSessionAsync();
    Task<List<SessionSummary>> GetRecentSessionsAsync(int count = 20);
    Task<Session> CreateSessionAsync(string name, List<string> agentIds);
    Task EndSessionAsync(string sessionId);
}
```

Register in Program.cs:
```csharp
builder.Services.AddScoped<ISessionService, SessionService>();
```

---

## 🟢 Components Already Complete

✅ **Project Structure**
- Blazor Server project created
- MudBlazor configured
- SignalR Client added

✅ **All Pages Created**
- Dashboard.razor
- ChatInterface.razor
- Orchestrator.razor
- Settings.razor

✅ **All Components Created**
- MessageBubble.razor
- AgentCard.razor
- McpConfiguration.razor
- SessionHistory.razor
- MessageInput.razor
- TypingIndicator.razor
- AgentSidebar.razor
- OrchestrationPlan.razor
- OrchestrationTimeline.razor
- MarkdownRenderer.razor
- AttachmentChip.razor
- SessionIndicator.razor

✅ **Assets Complete**
- wwwroot/css/app.css (all styles)
- wwwroot/js/app.js (all interop)

---

## 🔧 After Track 1 Completes

### 1. Verify Model Properties
Once Track 1 adds properties, verify these components work:

**Pages/Orchestrator.razor** - Check these properties exist:
- `OrchestrationPlan.Goal`
- `OrchestrationPlan.Strategy`
- `OrchestrationPlan.Steps`

**Components/OrchestrationPlan.razor** - Check these exist:
- `OrchestrationStep.Description`
- `OrchestrationStep.AssignedAgentId`
- `OrchestrationStep.ExpectedDuration`

**Components/MessageBubble.razor** - Check this exists:
- `ChatMessage.SenderId`

### 2. Test SignalR Connections
**File**: `src/OrchestratorChat.Web/Pages/ChatInterface.razor`

Verify hub connections work:
```csharp
// Line 224 - Verify hub URL is correct
_agentHub = new HubConnectionBuilder()
    .WithUrl(Navigation.ToAbsoluteUri("/hubs/agent"))
    .Build();

// Test connection
try 
{
    await _agentHub.StartAsync();
    _logger.LogInformation("SignalR connected successfully");
}
catch (Exception ex)
{
    _logger.LogError(ex, "SignalR connection failed");
}
```

### 3. Implement Real Data Loading
Replace TODO placeholders with actual service calls:

**Pages/Dashboard.razor**
```csharp
private async Task LoadAgents()
{
    // Replace TODO with:
    _agents = await AgentFactory.GetConfiguredAgents();
    StateHasChanged();
}
```

**Components/SessionHistory.razor**
```csharp
private async Task LoadSessions()
{
    // Replace TODO with:
    _sessions = await SessionManager.GetRecentSessions(20);
    StateHasChanged();
}
```

---

## 📋 Testing Checklist

Once compilation errors are resolved:

### 1. Page Navigation Tests
- [ ] Home/Dashboard loads
- [ ] Chat interface loads
- [ ] Orchestrator page loads
- [ ] Settings page loads
- [ ] Navigation menu works

### 2. Component Rendering Tests
- [ ] Agent cards display
- [ ] Message bubbles render
- [ ] Typing indicator animates
- [ ] Session history drawer opens
- [ ] MCP configuration loads

### 3. Real-time Features
- [ ] SignalR connects
- [ ] Messages stream in real-time
- [ ] Agent status updates
- [ ] Progress indicators work

### 4. Responsive Design
- [ ] Mobile view works
- [ ] Tablet view works
- [ ] Desktop view works
- [ ] Drawer toggles properly

---

## 🚨 Known Issues

### Issue 1: MudBlazor Version
Current: 6.11.2
- Some components from specification might not exist
- May need to use alternative MudBlazor components

### Issue 2: SignalR Authentication
- Currently no authentication configured
- May need to add auth later

### Issue 3: File Attachments
- File upload not fully implemented
- Needs backend support

---

## ✅ Definition of Done

- [ ] All compilation errors resolved
- [ ] All pages load without errors
- [ ] SignalR connections establish
- [ ] Real-time updates working
- [ ] Agent cards display actual data
- [ ] Chat interface sends/receives messages
- [ ] Orchestration planning works
- [ ] Session history shows real sessions
- [ ] No console errors in browser
- [ ] Responsive on all screen sizes

---

## 📝 UI Polish Tasks (Optional)

After core functionality works:

1. **Loading States**
   - Add skeletons while loading
   - Progress indicators
   - Smooth transitions

2. **Error Handling**
   - User-friendly error messages
   - Retry mechanisms
   - Offline indicators

3. **Accessibility**
   - ARIA labels
   - Keyboard navigation
   - Screen reader support

4. **Performance**
   - Lazy loading
   - Virtual scrolling for long lists
   - Debounce search inputs

---

## 📞 Coordination with Other Tracks

### Need from Track 1:
- SessionManager implementation
- Orchestrator implementation
- Model properties

### Need from Track 2:
- Agent status updates
- Tool execution results

### Need from Track 4:
- SignalR hub implementations
- Real-time event handling

---

## 🎨 Optional Enhancements

If time permits after core functionality:

1. **Advanced Features**
   - Export chat history
   - Search messages
   - Filter agents
   - Keyboard shortcuts