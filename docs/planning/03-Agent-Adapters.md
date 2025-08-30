# Agent Adapters Specification

## Overview
This document specifies the implementation details for agent adapters that bridge the OrchestratorChat core with specific AI agent implementations (Claude Code and embedded Saturn).

## Project: OrchestratorChat.Agents

### Claude Code Adapter

#### ClaudeAgent Implementation
```csharp
namespace OrchestratorChat.Agents.Claude
{
    public class ClaudeAgent : IAgent, IDisposable
    {
        private Process _process;
        private StreamReader _outputReader;
        private StreamWriter _inputWriter;
        private readonly ILogger<ClaudeAgent> _logger;
        private readonly ClaudeConfiguration _configuration;
        private AgentStatus _status = AgentStatus.Uninitialized;
        private readonly SemaphoreSlim _processLock = new(1, 1);
        private CancellationTokenSource _processCts;
        
        public string Id { get; private set; }
        public string Name { get; set; }
        public AgentType Type => AgentType.Claude;
        public AgentStatus Status => _status;
        public AgentCapabilities Capabilities { get; private set; }
        public string WorkingDirectory { get; set; }
        
        public event EventHandler<AgentStatusChangedEventArgs> StatusChanged;
        public event EventHandler<AgentOutputEventArgs> OutputReceived;
        
        public ClaudeAgent(
            ILogger<ClaudeAgent> logger,
            IOptions<ClaudeConfiguration> configuration)
        {
            _logger = logger;
            _configuration = configuration.Value;
            Id = Guid.NewGuid().ToString();
        }
        
        public async Task<AgentInitializationResult> InitializeAsync(
            AgentConfiguration configuration)
        {
            try
            {
                SetStatus(AgentStatus.Initializing);
                
                // Validate Claude CLI is available
                if (!await ValidateClaudeCliAsync())
                {
                    return new AgentInitializationResult
                    {
                        Success = false,
                        ErrorMessage = "Claude CLI not found or not authenticated"
                    };
                }
                
                // Start Claude process
                await StartClaudeProcessAsync(configuration);
                
                // Set capabilities based on model
                Capabilities = GetCapabilitiesForModel(configuration.Model);
                
                SetStatus(AgentStatus.Ready);
                
                return new AgentInitializationResult
                {
                    Success = true,
                    Capabilities = Capabilities,
                    InitializationTime = TimeSpan.FromSeconds(2)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Claude agent");
                SetStatus(AgentStatus.Error);
                return new AgentInitializationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
        
        private async Task<bool> ValidateClaudeCliAsync()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _configuration.ClaudeExecutablePath ?? "claude",
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
        
        private async Task StartClaudeProcessAsync(AgentConfiguration config)
        {
            var arguments = BuildClaudeArguments(config);
            
            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _configuration.ClaudeExecutablePath ?? "claude",
                    Arguments = arguments,
                    WorkingDirectory = WorkingDirectory,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    Environment =
                    {
                        ["CLAUDE_OUTPUT_FORMAT"] = "json-stream"
                    }
                }
            };
            
            _process.Start();
            _outputReader = _process.StandardOutput;
            _inputWriter = _process.StandardInput;
            _processCts = new CancellationTokenSource();
            
            // Start output monitoring
            _ = Task.Run(() => MonitorOutputAsync(_processCts.Token));
        }
        
        private string BuildClaudeArguments(AgentConfiguration config)
        {
            var args = new List<string>();
            
            // Continue session if exists
            if (!string.IsNullOrEmpty(config.SessionId))
            {
                args.Add($"--continue {config.SessionId}");
            }
            
            // Model selection
            args.Add($"--model {config.Model}");
            
            // Output format
            args.Add("--output-format json-stream");
            
            // Temperature
            args.Add($"--temperature {config.Temperature}");
            
            // Max tokens
            args.Add($"--max-tokens {config.MaxTokens}");
            
            // System prompt
            if (!string.IsNullOrEmpty(config.SystemPrompt))
            {
                args.Add($"--system \"{EscapeArgument(config.SystemPrompt)}\"");
            }
            
            // Tools
            if (config.EnabledTools?.Any() == true)
            {
                args.Add($"--tools {string.Join(",", config.EnabledTools)}");
            }
            
            return string.Join(" ", args);
        }
        
        public async Task<IAsyncEnumerable<AgentResponse>> SendMessageAsync(
            AgentMessage message,
            CancellationToken cancellationToken = default)
        {
            await _processLock.WaitAsync(cancellationToken);
            try
            {
                SetStatus(AgentStatus.Busy);
                
                // Send message to Claude process
                await _inputWriter.WriteLineAsync(message.Content);
                await _inputWriter.FlushAsync();
                
                // Return streaming responses
                return StreamResponsesAsync(message.Id, cancellationToken);
            }
            finally
            {
                _processLock.Release();
                SetStatus(AgentStatus.Ready);
            }
        }
        
        private async IAsyncEnumerable<AgentResponse> StreamResponsesAsync(
            string messageId,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var buffer = new StringBuilder();
            var isComplete = false;
            
            while (!isComplete && !cancellationToken.IsCancellationRequested)
            {
                var line = await ReadLineAsync(cancellationToken);
                if (string.IsNullOrEmpty(line))
                    continue;
                
                if (TryParseJsonResponse(line, out var response))
                {
                    response.MessageId = messageId;
                    
                    if (response.Type == ResponseType.Text)
                    {
                        buffer.Append(response.Content);
                    }
                    
                    if (response.IsComplete)
                    {
                        isComplete = true;
                        response.Content = buffer.ToString();
                    }
                    
                    yield return response;
                }
            }
        }
        
        private bool TryParseJsonResponse(string line, out AgentResponse response)
        {
            response = null;
            try
            {
                var json = JsonSerializer.Deserialize<ClaudeJsonResponse>(line);
                response = MapToAgentResponse(json);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        public async Task<ToolExecutionResult> ExecuteToolAsync(
            ToolCall toolCall,
            CancellationToken cancellationToken = default)
        {
            // Claude handles tools internally
            // This is for explicit tool execution requests
            var toolMessage = new AgentMessage
            {
                Content = JsonSerializer.Serialize(toolCall),
                Role = MessageRole.Tool,
                AgentId = Id
            };
            
            var responses = new List<AgentResponse>();
            await foreach (var response in SendMessageAsync(toolMessage, cancellationToken))
            {
                responses.Add(response);
            }
            
            var lastResponse = responses.LastOrDefault();
            return new ToolExecutionResult
            {
                Success = lastResponse?.Type != ResponseType.Error,
                Output = lastResponse?.Content,
                Error = lastResponse?.Type == ResponseType.Error ? lastResponse.Content : null
            };
        }
        
        public async Task ShutdownAsync()
        {
            SetStatus(AgentStatus.Shutdown);
            
            _processCts?.Cancel();
            
            if (_process != null && !_process.HasExited)
            {
                _inputWriter?.WriteLine("exit");
                _inputWriter?.Flush();
                
                if (!_process.WaitForExit(5000))
                {
                    _process.Kill();
                }
            }
            
            Dispose();
        }
        
        public void Dispose()
        {
            _processCts?.Dispose();
            _processLock?.Dispose();
            _inputWriter?.Dispose();
            _outputReader?.Dispose();
            _process?.Dispose();
        }
    }
    
    public class ClaudeConfiguration
    {
        public string ClaudeExecutablePath { get; set; } = "claude";
        public string DefaultModel { get; set; } = "claude-3-sonnet-20240229";
        public bool EnableMcp { get; set; } = true;
        public string McpConfigPath { get; set; }
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
    }
    
    internal class ClaudeJsonResponse
    {
        public string Type { get; set; }
        public string Content { get; set; }
        public bool Done { get; set; }
        public List<ToolCall> ToolCalls { get; set; }
        public Usage Usage { get; set; }
    }
}
```

### Saturn Adapter (Embedded)

#### SaturnAgent Implementation
```csharp
namespace OrchestratorChat.Agents.Saturn
{
    public class SaturnAgent : IAgent
    {
        private readonly ILogger<SaturnAgent> _logger;
        private readonly ISaturnCore _saturnCore;
        private readonly SaturnConfiguration _configuration;
        private AgentStatus _status = AgentStatus.Uninitialized;
        private ILLMProvider _llmProvider;
        private Saturn.Agents.Agent _internalAgent;
        
        public string Id { get; private set; }
        public string Name { get; set; }
        public AgentType Type => AgentType.Saturn;
        public AgentStatus Status => _status;
        public AgentCapabilities Capabilities { get; private set; }
        public string WorkingDirectory { get; set; }
        
        public event EventHandler<AgentStatusChangedEventArgs> StatusChanged;
        public event EventHandler<AgentOutputEventArgs> OutputReceived;
        
        public SaturnAgent(
            ILogger<SaturnAgent> logger,
            ISaturnCore saturnCore,
            IOptions<SaturnConfiguration> configuration)
        {
            _logger = logger;
            _saturnCore = saturnCore;
            _configuration = configuration.Value;
            Id = Guid.NewGuid().ToString();
        }
        
        public async Task<AgentInitializationResult> InitializeAsync(
            AgentConfiguration configuration)
        {
            try
            {
                SetStatus(AgentStatus.Initializing);
                
                // Initialize Saturn LLM provider
                _llmProvider = await CreateLLMProviderAsync(configuration);
                
                // Create Saturn agent instance
                _internalAgent = await _saturnCore.CreateAgentAsync(
                    _llmProvider,
                    configuration);
                
                // Hook up Saturn events
                _internalAgent.OnToolCall += HandleSaturnToolCall;
                _internalAgent.OnStreaming += HandleSaturnStreaming;
                
                // Get capabilities from Saturn
                Capabilities = GetSaturnCapabilities();
                
                SetStatus(AgentStatus.Ready);
                
                return new AgentInitializationResult
                {
                    Success = true,
                    Capabilities = Capabilities,
                    InitializationTime = TimeSpan.FromSeconds(1)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Saturn agent");
                SetStatus(AgentStatus.Error);
                return new AgentInitializationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
        
        private async Task<ILLMProvider> CreateLLMProviderAsync(AgentConfiguration config)
        {
            // Use Saturn's provider factory
            return await _saturnCore.CreateProviderAsync(
                config.CustomSettings.GetValueOrDefault("Provider", "OpenRouter"),
                config.CustomSettings);
        }
        
        public async Task<IAsyncEnumerable<AgentResponse>> SendMessageAsync(
            AgentMessage message,
            CancellationToken cancellationToken = default)
        {
            SetStatus(AgentStatus.Busy);
            try
            {
                // Convert to Saturn message format
                var saturnMessage = ConvertToSaturnMessage(message);
                
                // Send through Saturn agent
                var responseStream = _internalAgent.ProcessMessageAsync(
                    saturnMessage,
                    cancellationToken);
                
                // Stream responses
                return StreamSaturnResponsesAsync(responseStream, message.Id, cancellationToken);
            }
            finally
            {
                SetStatus(AgentStatus.Ready);
            }
        }
        
        private async IAsyncEnumerable<AgentResponse> StreamSaturnResponsesAsync(
            IAsyncEnumerable<Saturn.Models.AgentResponse> saturnStream,
            string messageId,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var saturnResponse in saturnStream.WithCancellation(cancellationToken))
            {
                yield return new AgentResponse
                {
                    MessageId = messageId,
                    Content = saturnResponse.Content,
                    Type = MapResponseType(saturnResponse.Type),
                    IsComplete = saturnResponse.IsComplete,
                    ToolCalls = saturnResponse.ToolCalls?.Select(MapToolCall).ToList(),
                    Usage = new TokenUsage
                    {
                        InputTokens = saturnResponse.Usage?.InputTokens ?? 0,
                        OutputTokens = saturnResponse.Usage?.OutputTokens ?? 0,
                        TotalTokens = saturnResponse.Usage?.TotalTokens ?? 0
                    }
                };
            }
        }
        
        public async Task<ToolExecutionResult> ExecuteToolAsync(
            ToolCall toolCall,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Use Saturn's tool execution
                var saturnToolCall = ConvertToSaturnToolCall(toolCall);
                var result = await _internalAgent.ExecuteToolAsync(saturnToolCall, cancellationToken);
                
                return new ToolExecutionResult
                {
                    Success = result.Success,
                    Output = result.Output,
                    Error = result.Error,
                    ExecutionTime = result.ExecutionTime
                };
            }
            catch (Exception ex)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }
        
        public async Task ShutdownAsync()
        {
            SetStatus(AgentStatus.Shutdown);
            
            if (_internalAgent != null)
            {
                await _internalAgent.ShutdownAsync();
                _internalAgent = null;
            }
            
            _llmProvider?.Dispose();
        }
        
        private AgentCapabilities GetSaturnCapabilities()
        {
            var tools = _saturnCore.GetAvailableTools();
            
            return new AgentCapabilities
            {
                SupportsStreaming = true,
                SupportsTools = true,
                SupportsFileOperations = true,
                SupportsWebSearch = false,
                SupportedModels = _configuration.SupportedModels,
                AvailableTools = tools.Select(t => new ToolDefinition
                {
                    Name = t.Name,
                    Description = t.Description,
                    Schema = ConvertToolSchema(t.Schema)
                }).ToList(),
                MaxTokens = 100000,
                MaxConcurrentRequests = 1
            };
        }
    }
    
    public class SaturnConfiguration
    {
        public List<string> SupportedModels { get; set; } = new()
        {
            "claude-3-opus-20240229",
            "claude-3-sonnet-20240229",
            "gpt-4-turbo-preview",
            "deepseek-coder"
        };
        
        public string DefaultProvider { get; set; } = "OpenRouter";
        public Dictionary<string, string> ProviderSettings { get; set; } = new();
        public bool EnableMultiAgent { get; set; } = true;
        public int MaxSubAgents { get; set; } = 5;
    }
}
```

### Saturn Core Interface (for embedding)

```csharp
namespace OrchestratorChat.Saturn
{
    /// <summary>
    /// Core Saturn functionality exposed as a library
    /// </summary>
    public interface ISaturnCore
    {
        /// <summary>
        /// Create a new Saturn agent instance
        /// </summary>
        Task<Saturn.Agents.Agent> CreateAgentAsync(
            ILLMProvider provider,
            AgentConfiguration configuration);
        
        /// <summary>
        /// Create an LLM provider
        /// </summary>
        Task<ILLMProvider> CreateProviderAsync(
            string providerType,
            Dictionary<string, object> settings);
        
        /// <summary>
        /// Get available tools
        /// </summary>
        List<ToolInfo> GetAvailableTools();
        
        /// <summary>
        /// Register custom tool
        /// </summary>
        void RegisterTool(ITool tool);
        
        /// <summary>
        /// Get or create agent manager for multi-agent scenarios
        /// </summary>
        IAgentManager GetAgentManager();
    }
    
    /// <summary>
    /// Saturn core implementation
    /// </summary>
    public class SaturnCore : ISaturnCore
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ToolRegistry _toolRegistry;
        private readonly AgentManager _agentManager;
        
        public SaturnCore(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _toolRegistry = new ToolRegistry();
            _agentManager = new AgentManager();
            
            // Register default Saturn tools
            RegisterDefaultTools();
        }
        
        public async Task<Saturn.Agents.Agent> CreateAgentAsync(
            ILLMProvider provider,
            AgentConfiguration configuration)
        {
            var agent = new Saturn.Agents.Agent(
                provider,
                _toolRegistry,
                configuration);
            
            await agent.InitializeAsync();
            return agent;
        }
        
        public async Task<ILLMProvider> CreateProviderAsync(
            string providerType,
            Dictionary<string, object> settings)
        {
            return providerType switch
            {
                "OpenRouter" => new OpenRouterProvider(settings),
                "Anthropic" => new AnthropicProvider(settings),
                _ => throw new NotSupportedException($"Provider {providerType} not supported")
            };
        }
        
        public List<ToolInfo> GetAvailableTools()
        {
            return _toolRegistry.GetAllTools()
                .Select(t => new ToolInfo
                {
                    Name = t.Name,
                    Description = t.Description,
                    Schema = t.Schema
                })
                .ToList();
        }
        
        public void RegisterTool(ITool tool)
        {
            _toolRegistry.Register(tool);
        }
        
        public IAgentManager GetAgentManager()
        {
            return _agentManager;
        }
        
        private void RegisterDefaultTools()
        {
            // Register Saturn's built-in tools
            _toolRegistry.Register(new ReadFileTool());
            _toolRegistry.Register(new WriteFileTool());
            _toolRegistry.Register(new GrepTool());
            _toolRegistry.Register(new GlobTool());
            _toolRegistry.Register(new BashTool());
            _toolRegistry.Register(new ApplyDiffTool());
        }
    }
}
```

### Agent Factory

```csharp
namespace OrchestratorChat.Agents
{
    public interface IAgentFactory
    {
        Task<IAgent> CreateAgentAsync(AgentType type, AgentConfiguration configuration);
        List<AgentType> GetSupportedTypes();
    }
    
    public class AgentFactory : IAgentFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AgentFactory> _logger;
        
        public AgentFactory(
            IServiceProvider serviceProvider,
            ILogger<AgentFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }
        
        public async Task<IAgent> CreateAgentAsync(
            AgentType type,
            AgentConfiguration configuration)
        {
            IAgent agent = type switch
            {
                AgentType.Claude => _serviceProvider.GetRequiredService<ClaudeAgent>(),
                AgentType.Saturn => _serviceProvider.GetRequiredService<SaturnAgent>(),
                _ => throw new NotSupportedException($"Agent type {type} not supported")
            };
            
            agent.Name = configuration.Name ?? $"{type} Agent";
            agent.WorkingDirectory = configuration.WorkingDirectory ?? Directory.GetCurrentDirectory();
            
            var result = await agent.InitializeAsync(configuration);
            if (!result.Success)
            {
                throw new AgentException($"Failed to initialize {type} agent: {result.ErrorMessage}", agent.Id);
            }
            
            return agent;
        }
        
        public List<AgentType> GetSupportedTypes()
        {
            return Enum.GetValues<AgentType>()
                .Where(t => t != AgentType.Custom)
                .ToList();
        }
    }
}
```

### Agent Health Monitoring

```csharp
namespace OrchestratorChat.Agents.Monitoring
{
    public interface IAgentHealthMonitor
    {
        Task<AgentHealth> CheckHealthAsync(IAgent agent);
        void StartMonitoring(IAgent agent, TimeSpan interval);
        void StopMonitoring(string agentId);
        event EventHandler<AgentHealthChangedEventArgs> HealthChanged;
    }
    
    public class AgentHealthMonitor : IAgentHealthMonitor, IDisposable
    {
        private readonly Dictionary<string, Timer> _monitors = new();
        private readonly ILogger<AgentHealthMonitor> _logger;
        
        public event EventHandler<AgentHealthChangedEventArgs> HealthChanged;
        
        public async Task<AgentHealth> CheckHealthAsync(IAgent agent)
        {
            try
            {
                // Send health check message
                var healthMessage = new AgentMessage
                {
                    Content = "ping",
                    Role = MessageRole.System
                };
                
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var responses = new List<AgentResponse>();
                
                await foreach (var response in agent.SendMessageAsync(healthMessage, cts.Token))
                {
                    responses.Add(response);
                }
                
                return new AgentHealth
                {
                    AgentId = agent.Id,
                    Status = HealthStatus.Healthy,
                    LastCheckTime = DateTime.UtcNow,
                    ResponseTime = TimeSpan.FromMilliseconds(100)
                };
            }
            catch (Exception ex)
            {
                return new AgentHealth
                {
                    AgentId = agent.Id,
                    Status = HealthStatus.Unhealthy,
                    LastCheckTime = DateTime.UtcNow,
                    Error = ex.Message
                };
            }
        }
        
        public void StartMonitoring(IAgent agent, TimeSpan interval)
        {
            if (_monitors.ContainsKey(agent.Id))
                return;
            
            var timer = new Timer(
                async _ => await CheckAndReportHealthAsync(agent),
                null,
                TimeSpan.Zero,
                interval);
            
            _monitors[agent.Id] = timer;
        }
        
        public void StopMonitoring(string agentId)
        {
            if (_monitors.TryGetValue(agentId, out var timer))
            {
                timer?.Dispose();
                _monitors.Remove(agentId);
            }
        }
        
        private async Task CheckAndReportHealthAsync(IAgent agent)
        {
            var health = await CheckHealthAsync(agent);
            HealthChanged?.Invoke(this, new AgentHealthChangedEventArgs
            {
                AgentId = agent.Id,
                Health = health
            });
        }
        
        public void Dispose()
        {
            foreach (var timer in _monitors.Values)
            {
                timer?.Dispose();
            }
            _monitors.Clear();
        }
    }
    
    public class AgentHealth
    {
        public string AgentId { get; set; }
        public HealthStatus Status { get; set; }
        public DateTime LastCheckTime { get; set; }
        public TimeSpan? ResponseTime { get; set; }
        public string Error { get; set; }
        public Dictionary<string, object> Metrics { get; set; } = new();
    }
    
    public enum HealthStatus
    {
        Unknown,
        Healthy,
        Degraded,
        Unhealthy
    }
}
```

## Dependency Injection Setup

```csharp
// In Program.cs
services.AddSingleton<ISaturnCore, SaturnCore>();
services.AddTransient<ClaudeAgent>();
services.AddTransient<SaturnAgent>();
services.AddSingleton<IAgentFactory, AgentFactory>();
services.AddSingleton<IAgentHealthMonitor, AgentHealthMonitor>();

services.Configure<ClaudeConfiguration>(configuration.GetSection("Claude"));
services.Configure<SaturnConfiguration>(configuration.GetSection("Saturn"));
```

## Testing Guidelines

### Unit Tests
```csharp
[TestClass]
public class ClaudeAgentTests
{
    [TestMethod]
    public async Task InitializeAsync_ValidConfiguration_ReturnsSuccess()
    {
        // Arrange
        var logger = new Mock<ILogger<ClaudeAgent>>();
        var config = Options.Create(new ClaudeConfiguration());
        var agent = new ClaudeAgent(logger.Object, config);
        
        // Act
        var result = await agent.InitializeAsync(new AgentConfiguration());
        
        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(AgentStatus.Ready, agent.Status);
    }
}
```

### Integration Tests
```csharp
[TestClass]
[TestCategory("Integration")]
public class AgentIntegrationTests
{
    [TestMethod]
    public async Task ClaudeAgent_SendMessage_ReturnsResponse()
    {
        // This test requires Claude CLI to be installed
        // Arrange
        var services = new ServiceCollection();
        // ... configure services
        var provider = services.BuildServiceProvider();
        
        var factory = provider.GetRequiredService<IAgentFactory>();
        var agent = await factory.CreateAgentAsync(AgentType.Claude, new AgentConfiguration());
        
        // Act
        var message = new AgentMessage { Content = "Hello" };
        var responses = new List<AgentResponse>();
        await foreach (var response in agent.SendMessageAsync(message))
        {
            responses.Add(response);
        }
        
        // Assert
        Assert.IsTrue(responses.Any());
    }
}
```

## Implementation Checklist

### Claude Agent
- [ ] Process management
- [ ] Stream parsing
- [ ] Session continuity
- [ ] Error handling
- [ ] Tool execution
- [ ] MCP support

### Saturn Agent
- [ ] Remove Terminal.Gui dependencies
- [ ] Create ISaturnCore interface
- [ ] Implement provider abstraction
- [ ] Tool registry integration
- [ ] Multi-agent support
- [ ] Event mapping

### Common
- [ ] Agent factory
- [ ] Health monitoring
- [ ] Logging
- [ ] Metrics collection
- [ ] Unit tests
- [ ] Integration tests

## Next Steps
1. Implement ClaudeAgent with process management
2. Transform Saturn codebase for embedding
3. Create comprehensive test suite
4. Document agent-specific configuration

## Version History
- v1.0 - Initial specification
- Date: 2024-01-30
- Status: Ready for implementation