# Track 2: Agent Adapters - Remaining Work

## Status: 40% Complete - NEEDS IMPLEMENTATION

### Developer: Agent Team
### Priority: HIGH - Required for agent functionality
### Estimated Time: 1-2 days

---

## 🔴 CRITICAL: Missing Agent Implementations

### 1. Saturn Core Implementation
**File to Create**: `src/OrchestratorChat.Agents/Saturn/SaturnCore.cs`

```csharp
using OrchestratorChat.Core.Agents;
using Saturn; // Reference to embedded Saturn library

namespace OrchestratorChat.Agents.Saturn;

public class SaturnCore : ISaturnCore
{
    private readonly ILogger<SaturnCore> _logger;
    private AgentManager? _agentManager; // Saturn's AgentManager
    private ILLMProvider? _provider;
    private bool _isInitialized;

    public SaturnCore(ILogger<SaturnCore> logger)
    {
        _logger = logger;
    }

    // REQUIRED METHODS TO IMPLEMENT:

    public async Task<bool> InitializeAsync(SaturnConfiguration config)
    {
        // TODO: Implement
        // 1. Create Saturn AgentManager instance
        // 2. Configure LLM provider (OpenRouter/Anthropic)
        // 3. Set up tool registry
        // 4. Configure working directory
        // 5. Load system prompt
        // Return true if successful
    }

    public async Task<string> ProcessMessageAsync(string message, CancellationToken cancellationToken)
    {
        // TODO: Implement
        // 1. Validate initialization
        // 2. Create message context
        // 3. Send to Saturn agent
        // 4. Handle streaming response
        // 5. Return complete response
    }

    public async Task<ToolExecutionResult> ExecuteToolAsync(
        string toolName, 
        Dictionary<string, object> parameters)
    {
        // TODO: Implement
        // 1. Find tool in registry
        // 2. Validate parameters
        // 3. Execute tool
        // 4. Return result
    }

    public void Dispose()
    {
        // TODO: Clean up Saturn resources
    }

    public Task<List<ToolInfo>> GetAvailableToolsAsync()
    {
        // TODO: Return list of available Saturn tools
    }

    public Task<bool> SetWorkingDirectoryAsync(string path)
    {
        // TODO: Update Saturn's working directory
    }
}
```

### 2. Complete Claude Agent Implementation
**File to Update**: `src/OrchestratorChat.Agents/Claude/ClaudeAgent.cs`

Add missing functionality:
```csharp
public partial class ClaudeAgent : AgentBase
{
    // ADD THESE METHODS:

    protected override async Task<AgentResponse> ProcessMessageInternalAsync(
        AgentMessage message, 
        CancellationToken cancellationToken)
    {
        // TODO: Implement
        // 1. Format message for Claude
        // 2. Handle attachments
        // 3. Send to Claude process via stdin/stdout
        // 4. Parse Claude's response
        // 5. Handle tool calls if any
        // 6. Return formatted response
    }

    private async Task<bool> StartClaudeProcessAsync()
    {
        // TODO: Implement
        // 1. Find claude executable
        // 2. Set up process start info
        // 3. Configure stdin/stdout redirection
        // 4. Start process
        // 5. Set up output handlers
    }

    private async Task HandleClaudeOutputAsync(string output)
    {
        // TODO: Implement
        // 1. Parse output format
        // 2. Detect tool calls
        // 3. Handle streaming updates
        // 4. Emit events
    }

    private async Task<ToolExecutionResult> ExecuteClaudeToolAsync(
        ToolCall toolCall)
    {
        // TODO: Implement
        // 1. Map tool to Claude's tool format
        // 2. Send tool execution request
        // 3. Parse result
        // 4. Return result
    }
}
```

### 3. Complete Saturn Agent Implementation
**File to Update**: `src/OrchestratorChat.Agents/Saturn/SaturnAgent.cs`

```csharp
public partial class SaturnAgent : AgentBase
{
    private ISaturnCore _saturnCore;

    // COMPLETE THESE METHODS:

    public override async Task<InitializationResult> InitializeAsync(
        AgentConfiguration configuration)
    {
        // TODO: Implement
        // 1. Create SaturnCore instance
        // 2. Configure Saturn settings
        // 3. Initialize Saturn
        // 4. Set up event handlers
        // 5. Return result
    }

    protected override async Task<AgentResponse> ProcessMessageInternalAsync(
        AgentMessage message, 
        CancellationToken cancellationToken)
    {
        // TODO: Implement
        // 1. Validate Saturn is initialized
        // 2. Convert message to Saturn format
        // 3. Process through SaturnCore
        // 4. Handle response
        // 5. Convert back to AgentResponse
    }

    public override async Task<List<ToolInfo>> GetAvailableToolsAsync()
    {
        // TODO: Get tools from SaturnCore
        return await _saturnCore.GetAvailableToolsAsync();
    }

    protected override async Task<ToolExecutionResult> ExecuteToolInternalAsync(
        string toolName, 
        Dictionary<string, object> parameters)
    {
        // TODO: Execute through SaturnCore
        return await _saturnCore.ExecuteToolAsync(toolName, parameters);
    }
}
```

---

## 🟡 Agent Factory Updates

### Move IAgentFactory to Core
**Current Location**: `src/OrchestratorChat.SignalR/Hubs/IAgentFactory.cs`
**Move To**: `src/OrchestratorChat.Core/Agents/IAgentFactory.cs`

```csharp
namespace OrchestratorChat.Core.Agents;

public interface IAgentFactory
{
    Task<IAgent> CreateAgentAsync(AgentType type, AgentConfiguration configuration);
    Task<List<AgentInfo>> GetConfiguredAgents();
    Task<IAgent?> GetAgentAsync(string agentId);
    void RegisterAgent(string agentId, IAgent agent);
}
```

### Update AgentFactory Implementation
**File**: `src/OrchestratorChat.Agents/AgentFactory.cs`

```csharp
// Update namespace reference
using OrchestratorChat.Core.Agents; // Instead of SignalR

// ADD missing methods:
public async Task<List<AgentInfo>> GetConfiguredAgents()
{
    // TODO: Implement
    // 1. Read from configuration
    // 2. Query registered agents
    // 3. Return agent info list
}

public async Task<IAgent?> GetAgentAsync(string agentId)
{
    // TODO: Implement
    // 1. Look up in registry
    // 2. Return agent or null
}

public void RegisterAgent(string agentId, IAgent agent)
{
    // TODO: Implement
    // 1. Add to internal registry
    // 2. Handle duplicates
}
```

---

## 🟢 Tool Execution Infrastructure

### Create Tool Executor
**File to Create**: `src/OrchestratorChat.Agents/Tools/ToolExecutor.cs`

```csharp
using OrchestratorChat.Core.Agents;

namespace OrchestratorChat.Agents.Tools;

public class ToolExecutor : IToolExecutor
{
    private readonly Dictionary<string, IToolHandler> _handlers;

    // IMPLEMENT:
    // 1. Tool registration
    // 2. Parameter validation
    // 3. Execution with timeout
    // 4. Result formatting
    // 5. Error handling

    public async Task<ToolExecutionResult> ExecuteAsync(
        string toolName,
        Dictionary<string, object> parameters,
        TimeSpan? timeout = null)
    {
        // TODO: Full implementation
    }
}
```

### Create Common Tool Handlers
**Directory**: `src/OrchestratorChat.Agents/Tools/Handlers/`

Create handlers for common tools:
1. `FileReadHandler.cs` - Read file operations
2. `FileWriteHandler.cs` - Write file operations
3. `BashCommandHandler.cs` - Execute shell commands
4. `WebSearchHandler.cs` - Web search operations

---

## 🔧 Configuration Models

### Saturn Configuration
**File to Create**: `src/OrchestratorChat.Agents/Saturn/SaturnConfiguration.cs`

```csharp
public class SaturnConfiguration
{
    public string Provider { get; set; } = "OpenRouter";
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "claude-3-sonnet";
    public string WorkingDirectory { get; set; } = ".";
    public List<string> EnabledTools { get; set; } = new();
    public string? SystemPrompt { get; set; }
    public int MaxTokens { get; set; } = 4096;
    public double Temperature { get; set; } = 0.7;
}
```

---

## 📋 Testing Requirements

1. **Agent Initialization Tests**
   - Test Claude agent startup
   - Test Saturn agent initialization
   - Test configuration validation
   - Test failure scenarios

2. **Message Processing Tests**
   - Test message handling
   - Test streaming responses
   - Test error handling
   - Test cancellation

3. **Tool Execution Tests**
   - Test tool discovery
   - Test parameter validation
   - Test execution timeout
   - Test result formatting

---

## 🚨 Dependencies on Other Tracks

### From Track 1 (Core):
- Need IAgentFactory moved to Core namespace
- Need EventBus for agent events
- Need Session models for context

### From Track 3 (Web UI):
- UI expects specific AgentInfo properties
- UI expects streaming capability flags

### From Track 4 (SignalR):
- SignalR hubs need agent instances
- Need event coordination

---

## 📞 Integration Points

1. **Process Management**
   - Claude: External process via stdin/stdout
   - Saturn: Embedded library calls
   - Handle process lifecycle

2. **Stream Handling**
   - Implement IAsyncEnumerable for streaming
   - Buffer management
   - Error recovery

3. **Tool Integration**
   - MCP tool discovery
   - Parameter marshalling
   - Result transformation

---

## ✅ Definition of Done

- [ ] SaturnCore fully implemented
- [ ] ClaudeAgent process management working
- [ ] SaturnAgent integrated with embedded library
- [ ] Tool execution infrastructure complete
- [ ] IAgentFactory moved to Core
- [ ] All agents can initialize successfully
- [ ] Message processing works end-to-end
- [ ] Tool execution validated
- [ ] Unit tests passing
- [ ] Integration tested with SignalR hubs

---

## 📝 Implementation Notes

### Claude Integration
- Use Process class for claude executable
- Implement stdin/stdout communication
- Parse Claude's response format
- Handle tool calls in Claude's format

### Saturn Integration
- Reference Saturn project as library
- Initialize Saturn's agent system
- Map between Saturn and OrchestratorChat models
- Handle Saturn's tool registry

### Error Handling
- Graceful degradation on agent failure
- Automatic restart capability
- Timeout handling for long operations
- Clear error messages to UI

### Performance Considerations
- Implement connection pooling for agents
- Cache tool definitions
- Optimize message serialization
- Handle concurrent requests properly