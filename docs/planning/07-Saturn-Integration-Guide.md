# Saturn Integration Guide

## Overview
This guide details the process of transforming Saturn from a Terminal.Gui CLI application into an embedded library for OrchestratorChat, enabling web-based multi-agent orchestration via SignalR.

## Transformation Strategy

### Phase 1: Code Analysis and Extraction

#### Files to Extract from SaturnFork
```
SaturnFork/
├── Agents/                     # Core agent logic - EXTRACT
│   ├── Agent.cs               # Main agent class
│   ├── Core/                  # Core functionality
│   │   ├── JsonValidator.cs
│   │   └── Objects/
│   └── MultiAgent/            # Multi-agent support
│       └── Objects/
├── Tools/                      # Tool implementations - EXTRACT
│   ├── Core/
│   │   ├── ITool.cs
│   │   ├── ToolBase.cs
│   │   └── AgentContext.cs
│   ├── MultiAgent/
│   └── [Various tool files]
├── Providers/                  # LLM providers - EXTRACT
│   ├── ILLMProvider.cs
│   ├── OpenRouter/
│   └── Anthropic/
├── Configuration/              # Config system - EXTRACT PARTIALLY
│   └── Objects/
├── Data/                       # Data models - EXTRACT PARTIALLY
│   └── Models/
├── OpenRouter/                 # OpenRouter client - EXTRACT
│   ├── OpenRouterClient.cs
│   └── Models/
└── UI/                        # Terminal UI - REMOVE
    ├── ChatInterface.cs       # Terminal.Gui - Remove
    └── Dialogs/              # Terminal dialogs - Remove
```

### Phase 2: Remove Terminal.Gui Dependencies

#### Current Dependencies to Remove
```csharp
// Before (in Saturn)
using Terminal.Gui;

public class ChatInterface : IDisposable
{
    private TextView chatView;
    private TextView inputField;
    private Button sendButton;
    // ... Terminal.Gui specific code
}

// After (in OrchestratorChat.Saturn)
// No UI code - pure business logic only
public class SaturnCore : ISaturnCore
{
    // Expose only the core functionality
}
```

#### Replacement Strategy
| Saturn Component | Terminal.Gui Usage | Replacement in OrchestratorChat |
|-----------------|-------------------|----------------------------------|
| ChatInterface | Main UI | SignalR + Blazor |
| CommandApprovalDialog | User approval | SignalR callback |
| LoadChatDialog | Session selection | Web UI component |
| ModeSelectionDialog | Mode picker | Web UI dropdown |
| ToolSelectionDialog | Tool picker | Web UI checklist |
| GitRepositoryPrompt | Git check | Automatic validation |
| MarkdownRenderer | Console rendering | Blazor Markdown component |

### Phase 3: Create Saturn Library Project

#### New Project Structure
```
OrchestratorChat.Saturn/
├── OrchestratorChat.Saturn.csproj
├── Core/
│   ├── SaturnCore.cs           # Main entry point
│   ├── ISaturnCore.cs          # Public interface
│   └── SaturnConfiguration.cs
├── Agents/
│   ├── SaturnAgent.cs          # From Saturn's Agent.cs
│   ├── AgentManager.cs
│   └── MultiAgent/
├── Tools/
│   ├── ToolRegistry.cs
│   ├── ToolBase.cs
│   └── Implementations/
├── Providers/
│   ├── ILLMProvider.cs
│   ├── OpenRouterProvider.cs
│   └── AnthropicProvider.cs
└── Models/
    └── [Shared models]
```

### Phase 4: Interface Design

#### Core Saturn Interface
```csharp
namespace OrchestratorChat.Saturn
{
    /// <summary>
    /// Main interface for embedded Saturn functionality
    /// </summary>
    public interface ISaturnCore
    {
        // Agent Management
        Task<ISaturnAgent> CreateAgentAsync(
            ILLMProvider provider,
            SaturnAgentConfiguration configuration);
        
        Task<IAgentManager> GetAgentManagerAsync();
        
        // Provider Management
        Task<ILLMProvider> CreateProviderAsync(
            ProviderType type,
            Dictionary<string, object> settings);
        
        List<ProviderInfo> GetAvailableProviders();
        
        // Tool Management
        IToolRegistry GetToolRegistry();
        void RegisterTool(ITool tool);
        List<ToolInfo> GetAvailableTools();
        
        // Configuration
        Task<SaturnConfiguration> LoadConfigurationAsync();
        Task SaveConfigurationAsync(SaturnConfiguration config);
    }
    
    /// <summary>
    /// Saturn agent interface
    /// </summary>
    public interface ISaturnAgent
    {
        string Id { get; }
        string Name { get; set; }
        AgentStatus Status { get; }
        
        // Core operations
        Task<IAsyncEnumerable<AgentResponse>> ProcessMessageAsync(
            AgentMessage message,
            CancellationToken cancellationToken = default);
        
        Task<ToolExecutionResult> ExecuteToolAsync(
            ToolCall toolCall,
            CancellationToken cancellationToken = default);
        
        Task ShutdownAsync();
        
        // Events
        event EventHandler<ToolCallEventArgs> OnToolCall;
        event EventHandler<StreamingEventArgs> OnStreaming;
        event EventHandler<StatusChangedEventArgs> OnStatusChanged;
    }
}
```

### Phase 5: Implementation Steps

#### Step 1: Fork and Branch
```bash
# Clone SaturnFork
git clone https://github.com/yourusername/SaturnFork.git
cd SaturnFork

# Create integration branch
git checkout -b orchestrator-integration

# Create new library project
dotnet new classlib -n OrchestratorChat.Saturn
```

#### Step 2: Extract Core Files
```bash
# Copy core files to new project
cp -r Agents/ ../OrchestratorChat.Saturn/Agents/
cp -r Tools/ ../OrchestratorChat.Saturn/Tools/
cp -r Providers/ ../OrchestratorChat.Saturn/Providers/
cp -r OpenRouter/ ../OrchestratorChat.Saturn/OpenRouter/

# Remove UI files
rm -rf ../OrchestratorChat.Saturn/UI/
```

#### Step 3: Remove Terminal.Gui References
```csharp
// Original Saturn Agent.cs
public class Agent
{
    private readonly ChatInterface _ui; // REMOVE
    
    public Agent(ILLMProvider provider, ChatInterface ui)
    {
        _provider = provider;
        _ui = ui; // REMOVE
    }
    
    private void UpdateUI(string message)
    {
        _ui.UpdateChat(message); // REMOVE
    }
}

// Modified for OrchestratorChat.Saturn
public class SaturnAgent : ISaturnAgent
{
    public event EventHandler<StreamingEventArgs> OnStreaming;
    
    public SaturnAgent(ILLMProvider provider)
    {
        _provider = provider;
    }
    
    private void RaiseStreamingEvent(string content)
    {
        OnStreaming?.Invoke(this, new StreamingEventArgs { Content = content });
    }
}
```

#### Step 4: Adapt Tool Approval Flow
```csharp
// Original Saturn with Terminal.Gui
public class BashTool : ToolBase
{
    protected override async Task<ToolExecutionResult> ExecuteAsync(ToolCall call)
    {
        if (RequiresApproval)
        {
            var approved = await CommandApprovalDialog.Show(call.Command);
            if (!approved) return new ToolExecutionResult { Success = false };
        }
        // Execute command
    }
}

// Modified for OrchestratorChat.Saturn
public class BashTool : ToolBase
{
    public delegate Task<bool> ApprovalCallback(ToolCall call);
    public ApprovalCallback OnApprovalRequired { get; set; }
    
    protected override async Task<ToolExecutionResult> ExecuteAsync(ToolCall call)
    {
        if (RequiresApproval && OnApprovalRequired != null)
        {
            var approved = await OnApprovalRequired(call);
            if (!approved) return new ToolExecutionResult { Success = false };
        }
        // Execute command
    }
}
```

#### Step 5: Create Adapter Layer
```csharp
namespace OrchestratorChat.Saturn
{
    public class SaturnCore : ISaturnCore
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ToolRegistry _toolRegistry;
        private readonly Dictionary<string, ILLMProvider> _providers;
        
        public SaturnCore(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _toolRegistry = new ToolRegistry();
            _providers = new Dictionary<string, ILLMProvider>();
            
            InitializeDefaultTools();
        }
        
        public async Task<ISaturnAgent> CreateAgentAsync(
            ILLMProvider provider,
            SaturnAgentConfiguration configuration)
        {
            var agent = new SaturnAgent(provider, _toolRegistry);
            
            // Configure agent
            agent.Configuration = new AgentConfiguration
            {
                Model = configuration.Model,
                Temperature = configuration.Temperature,
                MaxTokens = configuration.MaxTokens,
                SystemPrompt = configuration.SystemPrompt,
                EnableTools = configuration.EnableTools,
                ToolNames = configuration.ToolNames
            };
            
            // Hook up tool approval if needed
            if (configuration.RequireApproval)
            {
                foreach (var tool in _toolRegistry.GetTools())
                {
                    if (tool is BashTool bashTool)
                    {
                        bashTool.OnApprovalRequired = async (call) =>
                        {
                            // This will be handled via SignalR callback
                            return await RequestApprovalViaSignalR(call);
                        };
                    }
                }
            }
            
            await agent.InitializeAsync();
            return agent;
        }
        
        public async Task<ILLMProvider> CreateProviderAsync(
            ProviderType type,
            Dictionary<string, object> settings)
        {
            ILLMProvider provider = type switch
            {
                ProviderType.OpenRouter => new OpenRouterProvider(
                    new OpenRouterClient(new OpenRouterOptions
                    {
                        ApiKey = settings["ApiKey"]?.ToString()
                    })),
                ProviderType.Anthropic => new AnthropicProvider(settings),
                _ => throw new NotSupportedException($"Provider {type} not supported")
            };
            
            await provider.InitializeAsync();
            _providers[provider.Id] = provider;
            
            return provider;
        }
        
        private void InitializeDefaultTools()
        {
            _toolRegistry.Register(new ReadFileTool());
            _toolRegistry.Register(new WriteFileTool());
            _toolRegistry.Register(new GrepTool());
            _toolRegistry.Register(new GlobTool());
            _toolRegistry.Register(new BashTool());
            _toolRegistry.Register(new ApplyDiffTool());
            _toolRegistry.Register(new WebFetchTool());
            
            // Multi-agent tools
            _toolRegistry.Register(new HandOffToAgentTool());
            _toolRegistry.Register(new WaitForAgentTool());
            _toolRegistry.Register(new GetTaskResultTool());
            _toolRegistry.Register(new GetAgentStatusTool());
        }
        
        private async Task<bool> RequestApprovalViaSignalR(ToolCall call)
        {
            // This will be implemented to request approval via SignalR
            // For now, auto-approve in development
            return true;
        }
    }
}
```

### Phase 6: SignalR Integration

#### Saturn SignalR Service
```csharp
namespace OrchestratorChat.Agents.Saturn
{
    public class SaturnSignalRService : ISaturnSignalRService
    {
        private readonly ISaturnCore _saturnCore;
        private readonly IHubContext<AgentHub, IAgentClient> _hubContext;
        private readonly Dictionary<string, ISaturnAgent> _activeAgents;
        
        public SaturnSignalRService(
            ISaturnCore saturnCore,
            IHubContext<AgentHub, IAgentClient> hubContext)
        {
            _saturnCore = saturnCore;
            _hubContext = hubContext;
            _activeAgents = new Dictionary<string, ISaturnAgent>();
        }
        
        public async Task<string> CreateSaturnAgentAsync(
            string connectionId,
            SaturnAgentConfiguration config)
        {
            // Create provider
            var provider = await _saturnCore.CreateProviderAsync(
                config.ProviderType,
                config.ProviderSettings);
            
            // Create agent
            var agent = await _saturnCore.CreateAgentAsync(provider, config);
            
            // Hook up streaming events to SignalR
            agent.OnStreaming += async (sender, args) =>
            {
                await _hubContext.Clients.Client(connectionId)
                    .ReceiveAgentResponse(new AgentResponseDto
                    {
                        AgentId = agent.Id,
                        Response = new AgentResponse
                        {
                            Content = args.Content,
                            Type = ResponseType.Text,
                            IsComplete = args.IsComplete
                        }
                    });
            };
            
            agent.OnToolCall += async (sender, args) =>
            {
                await _hubContext.Clients.Client(connectionId)
                    .ToolExecutionUpdate(new ToolExecutionUpdate
                    {
                        ToolName = args.ToolName,
                        Status = "Executing",
                        Message = args.Parameters.ToString()
                    });
            };
            
            _activeAgents[agent.Id] = agent;
            return agent.Id;
        }
        
        public async Task SendMessageToSaturnAsync(
            string agentId,
            string message,
            string sessionId)
        {
            if (!_activeAgents.TryGetValue(agentId, out var agent))
                throw new InvalidOperationException($"Agent {agentId} not found");
            
            var agentMessage = new AgentMessage
            {
                Content = message,
                Role = MessageRole.User,
                SessionId = sessionId
            };
            
            await foreach (var response in agent.ProcessMessageAsync(agentMessage))
            {
                // Responses are already being streamed via events
                // This is just for awaiting completion
            }
        }
    }
}
```

### Phase 7: Testing Strategy

#### Unit Tests
```csharp
[TestClass]
public class SaturnCoreTests
{
    [TestMethod]
    public async Task CreateAgent_WithValidConfig_ReturnsAgent()
    {
        // Arrange
        var serviceProvider = new ServiceCollection()
            .BuildServiceProvider();
        
        var saturnCore = new SaturnCore(serviceProvider);
        var provider = await saturnCore.CreateProviderAsync(
            ProviderType.OpenRouter,
            new Dictionary<string, object> { ["ApiKey"] = "test-key" });
        
        // Act
        var agent = await saturnCore.CreateAgentAsync(provider, new SaturnAgentConfiguration
        {
            Model = "claude-3-sonnet",
            Temperature = 0.7
        });
        
        // Assert
        Assert.IsNotNull(agent);
        Assert.AreEqual("claude-3-sonnet", agent.Configuration.Model);
    }
    
    [TestMethod]
    public async Task ToolRegistry_RegisterCustomTool_Success()
    {
        // Arrange
        var saturnCore = new SaturnCore(null);
        var customTool = new Mock<ITool>();
        customTool.Setup(t => t.Name).Returns("CustomTool");
        
        // Act
        saturnCore.RegisterTool(customTool.Object);
        var tools = saturnCore.GetAvailableTools();
        
        // Assert
        Assert.IsTrue(tools.Any(t => t.Name == "CustomTool"));
    }
}
```

#### Integration Tests
```csharp
[TestClass]
[TestCategory("Integration")]
public class SaturnIntegrationTests
{
    [TestMethod]
    public async Task SaturnAgent_ProcessMessage_ReturnsResponse()
    {
        // Arrange
        var saturnCore = new SaturnCore(CreateServiceProvider());
        var provider = CreateMockProvider();
        var agent = await saturnCore.CreateAgentAsync(provider, new SaturnAgentConfiguration());
        
        // Act
        var responses = new List<AgentResponse>();
        await foreach (var response in agent.ProcessMessageAsync(new AgentMessage
        {
            Content = "Hello",
            Role = MessageRole.User
        }))
        {
            responses.Add(response);
        }
        
        // Assert
        Assert.IsTrue(responses.Any());
    }
}
```

### Phase 8: Migration Checklist

#### Code Changes
- [ ] Remove all Terminal.Gui references
- [ ] Remove UI folder and classes
- [ ] Extract core agent logic
- [ ] Extract tool implementations
- [ ] Extract provider implementations
- [ ] Create ISaturnCore interface
- [ ] Implement SaturnCore class
- [ ] Create event-based streaming
- [ ] Implement approval callbacks
- [ ] Add SignalR integration layer

#### Project Setup
- [ ] Create OrchestratorChat.Saturn project
- [ ] Add necessary NuGet packages
- [ ] Configure project references
- [ ] Set up build pipeline
- [ ] Create unit test project

#### Testing
- [ ] Unit tests for SaturnCore
- [ ] Unit tests for agents
- [ ] Unit tests for tools
- [ ] Integration tests
- [ ] SignalR integration tests

### Phase 9: Configuration Migration

#### Original Saturn Configuration
```json
{
  "model": "claude-3-sonnet",
  "temperature": 0.7,
  "provider": "OpenRouter",
  "openRouterApiKey": "sk-...",
  "tools": ["read", "write", "bash", "grep"]
}
```

#### OrchestratorChat Saturn Configuration
```json
{
  "saturn": {
    "providers": {
      "openRouter": {
        "apiKey": "sk-...",
        "defaultModel": "claude-3-sonnet"
      },
      "anthropic": {
        "apiKey": "sk-ant-..."
      }
    },
    "defaultConfiguration": {
      "model": "claude-3-sonnet",
      "temperature": 0.7,
      "maxTokens": 4096,
      "enableTools": true,
      "requireApproval": true
    },
    "tools": {
      "enabled": ["read", "write", "bash", "grep", "glob", "applyDiff"],
      "requireApproval": ["bash", "write", "applyDiff"]
    },
    "multiAgent": {
      "enabled": true,
      "maxConcurrentAgents": 5
    }
  }
}
```

## Deployment Considerations

### Package Structure
```
OrchestratorChat.Saturn.nupkg
├── lib/
│   └── net8.0/
│       ├── OrchestratorChat.Saturn.dll
│       └── OrchestratorChat.Saturn.pdb
├── contentFiles/
│   └── tools/
│       └── [Default tool implementations]
└── OrchestratorChat.Saturn.nuspec
```

### Dependencies
```xml
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
<PackageReference Include="System.Text.Json" Version="8.0.0" />
<PackageReference Include="Polly" Version="8.0.0" />
```

## Troubleshooting Guide

### Common Issues

#### Issue: Tool execution fails
**Solution**: Ensure tool has proper permissions and approval callback is set

#### Issue: Provider initialization fails
**Solution**: Check API keys and network connectivity

#### Issue: Streaming not working
**Solution**: Verify event handlers are properly connected

## Next Steps
1. Fork SaturnFork repository
2. Create integration branch
3. Begin Terminal.Gui removal
4. Extract core components
5. Create Saturn library project
6. Implement ISaturnCore
7. Add SignalR integration
8. Write comprehensive tests
9. Document API surface

## Version History
- v1.0 - Initial integration guide
- Date: 2024-01-30
- Status: Ready for implementation