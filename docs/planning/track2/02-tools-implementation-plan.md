# Tools System Implementation Plan

## Overview
The tools system provides agents with capabilities to interact with the file system, execute commands, search code, and coordinate with other agents. Currently, OrchestratorChat.Saturn has only 4 basic tools. This document outlines the implementation of a complete tool suite based on SaturnFork.

## Current State vs Required State

### Current State (OrchestratorChat.Saturn)
- Basic tool interface (`ITool.cs`)
- Simple ToolRegistry
- 4 basic tools: ReadFile, WriteFile, Bash, Grep
- No tool validation or error handling
- No multi-agent tools

### Required State (from SaturnFork)
- 12+ core tools for file and system operations
- 6 multi-agent coordination tools
- Tool base class with common functionality
- Command approval service for security
- OpenRouter tool adapter for API integration
- Comprehensive error handling and validation

## Tool Categories

### 1. Core File Operation Tools

#### 1.1 ApplyDiffTool
**Location**: `src/OrchestratorChat.Saturn/Tools/ApplyDiffTool.cs`

**Purpose**: Apply unified diff patches to files

**Implementation**:
```csharp
public class ApplyDiffTool : ToolBase
{
    public override string Name => "apply_diff";
    public override string Description => "Apply a unified diff patch to a file";
    
    public class Parameters
    {
        [JsonPropertyName("file_path")]
        public string FilePath { get; set; }
        
        [JsonPropertyName("diff")]
        public string Diff { get; set; }
        
        [JsonPropertyName("validate")]
        public bool Validate { get; set; } = true;
    }
    
    protected override async Task<ToolResult> ExecuteInternal(string parametersJson)
    {
        // Parse unified diff format
        // Apply changes line by line
        // Validate result if requested
        // Handle conflicts and errors
    }
}
```

#### 1.2 DeleteFileTool
**Location**: `src/OrchestratorChat.Saturn/Tools/DeleteFileTool.cs`

**Purpose**: Safely delete files with confirmation

**Implementation**:
```csharp
public class DeleteFileTool : ToolBase
{
    public override string Name => "delete_file";
    
    public class Parameters
    {
        [JsonPropertyName("file_path")]
        public string FilePath { get; set; }
        
        [JsonPropertyName("recursive")]
        public bool Recursive { get; set; } = false;
    }
    
    protected override async Task<ToolResult> ExecuteInternal(string parametersJson)
    {
        // Validate file exists
        // Check permissions
        // Request approval if configured
        // Perform deletion
        // Log action
    }
}
```

#### 1.3 ExecuteCommandTool
**Location**: `src/OrchestratorChat.Saturn/Tools/ExecuteCommandTool.cs`

**Purpose**: Execute shell commands with approval and timeout

**Implementation**:
```csharp
public class ExecuteCommandTool : ToolBase
{
    private readonly ICommandApprovalService _approvalService;
    
    public override string Name => "execute_command";
    
    public class Parameters
    {
        [JsonPropertyName("command")]
        public string Command { get; set; }
        
        [JsonPropertyName("working_directory")]
        public string WorkingDirectory { get; set; }
        
        [JsonPropertyName("timeout_seconds")]
        public int TimeoutSeconds { get; set; } = 30;
        
        [JsonPropertyName("require_approval")]
        public bool RequireApproval { get; set; } = true;
    }
    
    protected override async Task<ToolResult> ExecuteInternal(string parametersJson)
    {
        // Parse and validate command
        // Check approval requirements
        // Execute with timeout
        // Capture stdout/stderr
        // Return structured result
    }
}
```

#### 1.4 GlobTool
**Location**: `src/OrchestratorChat.Saturn/Tools/GlobTool.cs`

**Purpose**: Find files using glob patterns

**Implementation**:
```csharp
public class GlobTool : ToolBase
{
    public override string Name => "glob";
    
    public class Parameters
    {
        [JsonPropertyName("pattern")]
        public string Pattern { get; set; }
        
        [JsonPropertyName("root_directory")]
        public string RootDirectory { get; set; } = ".";
        
        [JsonPropertyName("ignore_case")]
        public bool IgnoreCase { get; set; } = false;
        
        [JsonPropertyName("include_hidden")]
        public bool IncludeHidden { get; set; } = false;
    }
    
    protected override async Task<ToolResult> ExecuteInternal(string parametersJson)
    {
        // Use Microsoft.Extensions.FileSystemGlobbing
        // Apply pattern matching
        // Filter results
        // Return file list
    }
}
```

#### 1.5 ListFilesTool
**Location**: `src/OrchestratorChat.Saturn/Tools/ListFilesTool.cs`

**Purpose**: List directory contents with filtering

**Implementation**:
```csharp
public class ListFilesTool : ToolBase
{
    public override string Name => "list_files";
    
    public class Parameters
    {
        [JsonPropertyName("directory")]
        public string Directory { get; set; } = ".";
        
        [JsonPropertyName("recursive")]
        public bool Recursive { get; set; } = false;
        
        [JsonPropertyName("include_hidden")]
        public bool IncludeHidden { get; set; } = false;
        
        [JsonPropertyName("pattern")]
        public string Pattern { get; set; }
        
        [JsonPropertyName("max_depth")]
        public int MaxDepth { get; set; } = -1;
    }
}
```

#### 1.6 SearchAndReplaceTool
**Location**: `src/OrchestratorChat.Saturn/Tools/SearchAndReplaceTool.cs`

**Purpose**: Find and replace text across files

**Implementation**:
```csharp
public class SearchAndReplaceTool : ToolBase
{
    public override string Name => "search_and_replace";
    
    public class Parameters
    {
        [JsonPropertyName("file_path")]
        public string FilePath { get; set; }
        
        [JsonPropertyName("search_pattern")]
        public string SearchPattern { get; set; }
        
        [JsonPropertyName("replacement")]
        public string Replacement { get; set; }
        
        [JsonPropertyName("use_regex")]
        public bool UseRegex { get; set; } = false;
        
        [JsonPropertyName("multiline")]
        public bool Multiline { get; set; } = false;
    }
}
```

#### 1.7 WebFetchTool
**Location**: `src/OrchestratorChat.Saturn/Tools/WebFetchTool.cs`

**Purpose**: Fetch and process web content

**Implementation**:
```csharp
public class WebFetchTool : ToolBase
{
    private readonly HttpClient _httpClient;
    
    public override string Name => "web_fetch";
    
    public class Parameters
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
        
        [JsonPropertyName("method")]
        public string Method { get; set; } = "GET";
        
        [JsonPropertyName("headers")]
        public Dictionary<string, string> Headers { get; set; }
        
        [JsonPropertyName("body")]
        public string Body { get; set; }
        
        [JsonPropertyName("timeout_seconds")]
        public int TimeoutSeconds { get; set; } = 30;
    }
    
    protected override async Task<ToolResult> ExecuteInternal(string parametersJson)
    {
        // Validate URL
        // Build HTTP request
        // Execute with timeout
        // Handle redirects
        // Parse response (HTML to markdown if needed)
        // Return content
    }
}
```

### 2. Multi-Agent Coordination Tools

#### 2.1 CreateAgentTool
**Location**: `src/OrchestratorChat.Saturn/Tools/MultiAgent/CreateAgentTool.cs`

**Purpose**: Spawn new agent instances for parallel tasks

**Implementation**:
```csharp
public class CreateAgentTool : ToolBase
{
    private readonly IAgentManager _agentManager;
    
    public override string Name => "create_agent";
    
    public class Parameters
    {
        [JsonPropertyName("agent_name")]
        public string AgentName { get; set; }
        
        [JsonPropertyName("task")]
        public string Task { get; set; }
        
        [JsonPropertyName("model")]
        public string Model { get; set; }
        
        [JsonPropertyName("tools")]
        public List<string> Tools { get; set; }
        
        [JsonPropertyName("max_iterations")]
        public int MaxIterations { get; set; } = 10;
        
        [JsonPropertyName("context")]
        public Dictionary<string, object> Context { get; set; }
    }
    
    protected override async Task<ToolResult> ExecuteInternal(string parametersJson)
    {
        // Validate agent configuration
        // Check resource limits
        // Create agent instance
        // Initialize with task
        // Start execution
        // Return agent ID
    }
}
```

#### 2.2 HandOffToAgentTool
**Location**: `src/OrchestratorChat.Saturn/Tools/MultiAgent/HandOffToAgentTool.cs`

**Purpose**: Transfer control to another agent

**Implementation**:
```csharp
public class HandOffToAgentTool : ToolBase
{
    public override string Name => "hand_off_to_agent";
    
    public class Parameters
    {
        [JsonPropertyName("agent_id")]
        public string AgentId { get; set; }
        
        [JsonPropertyName("task")]
        public string Task { get; set; }
        
        [JsonPropertyName("context")]
        public Dictionary<string, object> Context { get; set; }
        
        [JsonPropertyName("wait_for_completion")]
        public bool WaitForCompletion { get; set; } = false;
    }
}
```

#### 2.3 GetAgentStatusTool
**Location**: `src/OrchestratorChat.Saturn/Tools/MultiAgent/GetAgentStatusTool.cs`

**Purpose**: Check status of running agents

**Implementation**:
```csharp
public class GetAgentStatusTool : ToolBase
{
    public override string Name => "get_agent_status";
    
    public class Parameters
    {
        [JsonPropertyName("agent_id")]
        public string AgentId { get; set; }
        
        [JsonPropertyName("include_output")]
        public bool IncludeOutput { get; set; } = false;
    }
    
    public class StatusResult
    {
        public string AgentId { get; set; }
        public string Status { get; set; } // Running, Completed, Failed, Terminated
        public int Progress { get; set; }
        public string CurrentTask { get; set; }
        public List<string> CompletedTasks { get; set; }
        public string Output { get; set; }
        public string Error { get; set; }
    }
}
```

#### 2.4 WaitForAgentTool
**Location**: `src/OrchestratorChat.Saturn/Tools/MultiAgent/WaitForAgentTool.cs`

**Purpose**: Wait for agent completion with timeout

**Implementation**:
```csharp
public class WaitForAgentTool : ToolBase
{
    public override string Name => "wait_for_agent";
    
    public class Parameters
    {
        [JsonPropertyName("agent_id")]
        public string AgentId { get; set; }
        
        [JsonPropertyName("timeout_seconds")]
        public int TimeoutSeconds { get; set; } = 300;
        
        [JsonPropertyName("poll_interval_seconds")]
        public int PollIntervalSeconds { get; set; } = 5;
    }
}
```

#### 2.5 TerminateAgentTool
**Location**: `src/OrchestratorChat.Saturn/Tools/MultiAgent/TerminateAgentTool.cs`

**Purpose**: Gracefully terminate agent execution

**Implementation**:
```csharp
public class TerminateAgentTool : ToolBase
{
    public override string Name => "terminate_agent";
    
    public class Parameters
    {
        [JsonPropertyName("agent_id")]
        public string AgentId { get; set; }
        
        [JsonPropertyName("force")]
        public bool Force { get; set; } = false;
        
        [JsonPropertyName("reason")]
        public string Reason { get; set; }
    }
}
```

#### 2.6 GetTaskResultTool
**Location**: `src/OrchestratorChat.Saturn/Tools/MultiAgent/GetTaskResultTool.cs`

**Purpose**: Retrieve results from completed agent tasks

**Implementation**:
```csharp
public class GetTaskResultTool : ToolBase
{
    public override string Name => "get_task_result";
    
    public class Parameters
    {
        [JsonPropertyName("task_id")]
        public string TaskId { get; set; }
        
        [JsonPropertyName("wait_if_running")]
        public bool WaitIfRunning { get; set; } = true;
    }
}
```

### 3. Tool Infrastructure

#### 3.1 ToolBase Abstract Class
**Location**: `src/OrchestratorChat.Saturn/Tools/Core/ToolBase.cs`

**Purpose**: Common functionality for all tools

**Implementation**:
```csharp
public abstract class ToolBase : ITool
{
    protected ILogger<ToolBase> Logger { get; }
    protected AgentContext Context { get; private set; }
    
    public abstract string Name { get; }
    public abstract string Description { get; }
    public virtual bool RequiresApproval { get; } = false;
    
    public async Task<ToolResult> ExecuteAsync(string parametersJson, AgentContext context)
    {
        Context = context;
        
        try
        {
            // Validate parameters
            ValidateParameters(parametersJson);
            
            // Check approval if required
            if (RequiresApproval && !await GetApprovalAsync(parametersJson))
            {
                return new ToolResult
                {
                    Success = false,
                    Error = "Tool execution denied by user"
                };
            }
            
            // Execute tool logic
            var result = await ExecuteInternal(parametersJson);
            
            // Log execution
            LogExecution(parametersJson, result);
            
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Tool execution failed: {ToolName}", Name);
            return new ToolResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }
    
    protected abstract Task<ToolResult> ExecuteInternal(string parametersJson);
    protected virtual void ValidateParameters(string parametersJson) { }
    protected virtual Task<bool> GetApprovalAsync(string parametersJson) => Task.FromResult(true);
    protected virtual void LogExecution(string parameters, ToolResult result) { }
}
```

#### 3.2 ToolRegistry
**Location**: `src/OrchestratorChat.Saturn/Tools/ToolRegistry.cs`

**Purpose**: Auto-discover and manage available tools

**Implementation**:
```csharp
public class ToolRegistry
{
    private readonly Dictionary<string, ITool> _tools = new();
    private readonly IServiceProvider _serviceProvider;
    
    public ToolRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        DiscoverTools();
    }
    
    private void DiscoverTools()
    {
        // Use reflection to find all ITool implementations
        var toolTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => !t.IsAbstract && typeof(ITool).IsAssignableFrom(t));
        
        foreach (var toolType in toolTypes)
        {
            var tool = (ITool)ActivatorUtilities.CreateInstance(_serviceProvider, toolType);
            _tools[tool.Name] = tool;
        }
    }
    
    public ITool GetTool(string name) => _tools.GetValueOrDefault(name);
    
    public IEnumerable<ITool> GetAllTools() => _tools.Values;
    
    public List<ToolDefinition> GetToolDefinitions()
    {
        return _tools.Values.Select(tool => new ToolDefinition
        {
            Name = tool.Name,
            Description = tool.Description,
            Parameters = tool.GetParameterSchema()
        }).ToList();
    }
}
```

#### 3.3 Command Approval Service
**Location**: `src/OrchestratorChat.Saturn/Tools/Core/CommandApprovalService.cs`

**Purpose**: Handle user approval for dangerous operations

**Implementation**:
```csharp
public interface ICommandApprovalService
{
    Task<bool> RequestApprovalAsync(string toolName, string command, string reason);
    void SetApprovalMode(ApprovalMode mode);
}

public class CommandApprovalService : ICommandApprovalService
{
    private ApprovalMode _mode = ApprovalMode.Ask;
    private readonly HashSet<string> _approvedCommands = new();
    
    public enum ApprovalMode
    {
        Always,  // Always approve
        Never,   // Never approve
        Ask,     // Ask for each command
        Once     // Ask once per unique command
    }
    
    public async Task<bool> RequestApprovalAsync(string toolName, string command, string reason)
    {
        switch (_mode)
        {
            case ApprovalMode.Always:
                return true;
            
            case ApprovalMode.Never:
                return false;
            
            case ApprovalMode.Once:
                if (_approvedCommands.Contains(command))
                    return true;
                // Fall through to ask
                
            case ApprovalMode.Ask:
                // In web context, send approval request to UI
                var approved = await SendApprovalRequestToUI(toolName, command, reason);
                if (approved && _mode == ApprovalMode.Once)
                    _approvedCommands.Add(command);
                return approved;
        }
    }
}
```

#### 3.4 OpenRouter Tool Adapter
**Location**: `src/OrchestratorChat.Saturn/Tools/Core/OpenRouterToolAdapter.cs`

**Purpose**: Convert tools to OpenRouter API format

**Implementation**:
```csharp
public class OpenRouterToolAdapter
{
    public static OpenRouter.Models.Api.Chat.ToolDefinition ConvertToOpenRouterTool(ITool tool)
    {
        return new OpenRouter.Models.Api.Chat.ToolDefinition
        {
            Type = "function",
            Function = new OpenRouter.Models.Api.Chat.ToolFunction
            {
                Name = tool.Name,
                Description = tool.Description,
                Parameters = tool.GetParameterSchema()
            }
        };
    }
    
    public static async Task<string> ExecuteToolCall(
        OpenRouter.Models.Api.Chat.ToolCall toolCall,
        ToolRegistry registry,
        AgentContext context)
    {
        var tool = registry.GetTool(toolCall.Function.Name);
        if (tool == null)
            return JsonSerializer.Serialize(new { error = "Tool not found" });
        
        var result = await tool.ExecuteAsync(toolCall.Function.Arguments, context);
        return JsonSerializer.Serialize(result);
    }
}
```

### 4. Tool Objects and Models

#### 4.1 Core Tool Models
**Location**: `src/OrchestratorChat.Saturn/Tools/Objects/`

```csharp
public class ToolResult
{
    public bool Success { get; set; }
    public string Output { get; set; }
    public string Error { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}

public class AgentContext
{
    public string AgentId { get; set; }
    public string SessionId { get; set; }
    public string WorkingDirectory { get; set; }
    public Dictionary<string, object> Variables { get; set; }
    public ILogger Logger { get; set; }
}

public class FileOperation
{
    public string Path { get; set; }
    public string Operation { get; set; } // Read, Write, Delete, Modify
    public DateTime Timestamp { get; set; }
    public string Content { get; set; }
}
```

## Implementation Priority

### Phase 1: Core Tools (Week 1)
1. Implement ToolBase abstract class
2. Update ToolRegistry with auto-discovery
3. Implement core file tools (Read, Write, Delete)
4. Add ExecuteCommand with approval

### Phase 2: Advanced Tools (Week 2)
1. Implement ApplyDiff tool
2. Add SearchAndReplace tool
3. Implement Glob and ListFiles tools
4. Add WebFetch tool

### Phase 3: Multi-Agent Tools (Week 3)
1. Implement CreateAgent tool
2. Add agent status and control tools
3. Implement task handoff mechanism
4. Add result retrieval

### Phase 4: Integration (Week 4)
1. Integrate with provider system
2. Add OpenRouter adapter
3. Implement approval service
4. Create comprehensive tests

## Testing Requirements

### Unit Tests
- Parameter validation for each tool
- Error handling scenarios
- Mock file system operations
- Command approval flow

### Integration Tests
- Tool execution with real files
- Multi-agent coordination
- Web fetch with mock server
- Command execution timeout

## Security Considerations

1. **Command Execution**:
   - Always validate and sanitize commands
   - Implement timeout mechanisms
   - Log all executions
   - Require approval for dangerous operations

2. **File Operations**:
   - Validate paths (no directory traversal)
   - Check permissions before operations
   - Implement safe deletion with confirmation
   - Log all file modifications

3. **Web Requests**:
   - Validate URLs
   - Implement request timeout
   - Limit response size
   - Block internal network access

4. **Multi-Agent**:
   - Resource limits per agent
   - Prevent infinite loops
   - Isolate agent contexts
   - Monitor resource usage

## Dependencies to Add

```xml
<PackageReference Include="Microsoft.Extensions.FileSystemGlobbing" Version="8.0.0" />
<PackageReference Include="DiffPlex" Version="1.7.1" />
<PackageReference Include="HtmlAgilityPack" Version="1.11.54" />
<PackageReference Include="ReverseMarkdown" Version="3.25.0" />
```

## Migration Notes

When porting from SaturnFork:
1. Replace console-based approval with web UI integration
2. Adapt tools for concurrent execution
3. Add async/await throughout
4. Implement proper cancellation tokens
5. Add telemetry and logging

## Validation Checklist

- [ ] All core tools implemented
- [ ] Multi-agent tools functional
- [ ] Tool registry with auto-discovery
- [ ] Command approval service integrated
- [ ] OpenRouter adapter working
- [ ] Comprehensive error handling
- [ ] Security measures in place
- [ ] Unit tests passing
- [ ] Integration tests passing