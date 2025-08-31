# SignalR Protocol Specification

## Overview
This document defines the real-time communication protocol using SignalR for OrchestratorChat, enabling bidirectional streaming between the server and web clients.

## Project: OrchestratorChat.SignalR

### Hub Architecture

#### Main Hubs

```csharp
namespace OrchestratorChat.SignalR.Hubs
{
    /// <summary>
    /// Primary hub for orchestration and multi-agent coordination
    /// </summary>
    public class OrchestratorHub : Hub<IOrchestratorClient>
    {
        private readonly IOrchestrator _orchestrator;
        private readonly ISessionManager _sessionManager;
        private readonly IAgentFactory _agentFactory;
        private readonly ILogger<OrchestratorHub> _logger;
        private readonly IHubContext<AgentHub, IAgentClient> _agentHubContext;
        
        public OrchestratorHub(
            IOrchestrator orchestrator,
            ISessionManager sessionManager,
            IAgentFactory agentFactory,
            ILogger<OrchestratorHub> logger,
            IHubContext<AgentHub, IAgentClient> agentHubContext)
        {
            _orchestrator = orchestrator;
            _sessionManager = sessionManager;
            _agentFactory = agentFactory;
            _logger = logger;
            _agentHubContext = agentHubContext;
        }
        
        /// <summary>
        /// Client connects to orchestrator
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.Connected(new ConnectionInfo
            {
                ConnectionId = Context.ConnectionId,
                ConnectedAt = DateTime.UtcNow
            });
            
            await base.OnConnectedAsync();
        }
        
        /// <summary>
        /// Create a new orchestration session
        /// </summary>
        public async Task<SessionCreatedResponse> CreateSession(CreateSessionRequest request)
        {
            try
            {
                var session = await _sessionManager.CreateSessionAsync(new SessionConfiguration
                {
                    Name = request.Name,
                    Type = request.Type,
                    AgentIds = request.AgentIds,
                    WorkingDirectory = request.WorkingDirectory
                });
                
                // Add to session group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"session-{session.Id}");
                
                // Notify session created
                await Clients.Group($"session-{session.Id}").SessionCreated(session);
                
                return new SessionCreatedResponse
                {
                    Success = true,
                    SessionId = session.Id,
                    Session = session
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create session");
                return new SessionCreatedResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }
        
        /// <summary>
        /// Send message to orchestrator for multi-agent coordination
        /// </summary>
        public async Task SendOrchestrationMessage(OrchestrationMessageRequest request)
        {
            var orchestrationRequest = new OrchestrationRequest
            {
                Goal = request.Message,
                AvailableAgentIds = request.AgentIds,
                Strategy = request.Strategy
            };
            
            // Create orchestration plan
            var plan = await _orchestrator.CreatePlanAsync(orchestrationRequest);
            
            // Notify plan created
            await Clients.Group($"session-{request.SessionId}")
                .OrchestrationPlanCreated(plan);
            
            // Execute plan with progress reporting
            var progress = new Progress<OrchestrationProgress>(async p =>
            {
                await Clients.Group($"session-{request.SessionId}")
                    .OrchestrationProgress(p);
            });
            
            // Execute asynchronously
            _ = Task.Run(async () =>
            {
                var result = await _orchestrator.ExecutePlanAsync(plan, progress);
                await Clients.Group($"session-{request.SessionId}")
                    .OrchestrationCompleted(result);
            });
        }
        
        /// <summary>
        /// Join an existing session
        /// </summary>
        public async Task JoinSession(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"session-{sessionId}");
            var session = await _sessionManager.GetSessionAsync(sessionId);
            await Clients.Caller.SessionJoined(session);
        }
        
        /// <summary>
        /// Leave session
        /// </summary>
        public async Task LeaveSession(string sessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session-{sessionId}");
        }
    }
    
    /// <summary>
    /// Hub for individual agent communication
    /// </summary>
    public class AgentHub : Hub<IAgentClient>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ISessionManager _sessionManager;
        private readonly ILogger<AgentHub> _logger;
        private static readonly ConcurrentDictionary<string, IAgent> _activeAgents = new();
        
        /// <summary>
        /// Send message directly to a specific agent
        /// </summary>
        public async Task SendAgentMessage(AgentMessageRequest request)
        {
            try
            {
                // Get or create agent
                var agent = await GetOrCreateAgentAsync(request.AgentId);
                
                // Create message
                var message = new AgentMessage
                {
                    Content = request.Content,
                    Role = MessageRole.User,
                    SessionId = request.SessionId,
                    AgentId = request.AgentId,
                    Attachments = request.Attachments
                };
                
                // Add to session history
                var session = await _sessionManager.GetSessionAsync(request.SessionId);
                session.Messages.Add(message);
                await _sessionManager.UpdateSessionAsync(session);
                
                // Stream responses
                await foreach (var response in agent.SendMessageAsync(message))
                {
                    await Clients.Group($"agent-{request.AgentId}")
                        .ReceiveAgentResponse(new AgentResponseDto
                        {
                            AgentId = request.AgentId,
                            SessionId = request.SessionId,
                            Response = response
                        });
                    
                    // Also send to session group
                    await Clients.Group($"session-{request.SessionId}")
                        .ReceiveAgentResponse(new AgentResponseDto
                        {
                            AgentId = request.AgentId,
                            SessionId = request.SessionId,
                            Response = response
                        });
                }
            }
            catch (Exception ex)
            {
                await Clients.Caller.ReceiveError(new ErrorResponse
                {
                    Error = ex.Message,
                    AgentId = request.AgentId,
                    SessionId = request.SessionId
                });
            }
        }
        
        /// <summary>
        /// Execute tool on agent
        /// </summary>
        public async Task<ToolExecutionResponse> ExecuteTool(ToolExecutionRequest request)
        {
            try
            {
                var agent = await GetOrCreateAgentAsync(request.AgentId);
                
                var result = await agent.ExecuteToolAsync(new ToolCall
                {
                    ToolName = request.ToolName,
                    Parameters = request.Parameters,
                    AgentId = request.AgentId,
                    SessionId = request.SessionId
                });
                
                return new ToolExecutionResponse
                {
                    Success = result.Success,
                    Output = result.Output,
                    Error = result.Error,
                    ExecutionTime = result.ExecutionTime
                };
            }
            catch (Exception ex)
            {
                return new ToolExecutionResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }
        
        /// <summary>
        /// Subscribe to agent updates
        /// </summary>
        public async Task SubscribeToAgent(string agentId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"agent-{agentId}");
            
            if (_activeAgents.TryGetValue(agentId, out var agent))
            {
                await Clients.Caller.AgentStatusUpdate(new AgentStatusDto
                {
                    AgentId = agentId,
                    Status = agent.Status,
                    Capabilities = agent.Capabilities
                });
            }
        }
        
        /// <summary>
        /// Unsubscribe from agent updates
        /// </summary>
        public async Task UnsubscribeFromAgent(string agentId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"agent-{agentId}");
        }
        
        private async Task<IAgent> GetOrCreateAgentAsync(string agentId)
        {
            if (_activeAgents.TryGetValue(agentId, out var agent))
                return agent;
            
            // Create agent based on configuration
            // This would typically load from database
            var factory = _serviceProvider.GetRequiredService<IAgentFactory>();
            agent = await factory.CreateAgentAsync(AgentType.Claude, new AgentConfiguration());
            
            _activeAgents[agentId] = agent;
            
            // Hook up events
            agent.StatusChanged += async (s, e) =>
            {
                await _agentHubContext.Clients.Group($"agent-{agentId}")
                    .AgentStatusUpdate(new AgentStatusDto
                    {
                        AgentId = agentId,
                        Status = e.NewStatus
                    });
            };
            
            return agent;
        }
    }
}
```

### Client Interfaces

```csharp
namespace OrchestratorChat.SignalR.Clients
{
    /// <summary>
    /// Client methods for orchestrator hub
    /// </summary>
    public interface IOrchestratorClient
    {
        Task Connected(ConnectionInfo info);
        Task SessionCreated(Session session);
        Task SessionJoined(Session session);
        Task OrchestrationPlanCreated(OrchestrationPlan plan);
        Task OrchestrationProgress(OrchestrationProgress progress);
        Task OrchestrationCompleted(OrchestrationResult result);
        Task ReceiveError(ErrorResponse error);
    }
    
    /// <summary>
    /// Client methods for agent hub
    /// </summary>
    public interface IAgentClient
    {
        Task ReceiveAgentResponse(AgentResponseDto response);
        Task AgentStatusUpdate(AgentStatusDto status);
        Task ToolExecutionUpdate(ToolExecutionUpdate update);
        Task ReceiveError(ErrorResponse error);
    }
}
```

### DTOs and Contracts

```csharp
namespace OrchestratorChat.SignalR.Contracts
{
    // Request DTOs
    public class CreateSessionRequest
    {
        public string Name { get; set; }
        public SessionType Type { get; set; }
        public List<string> AgentIds { get; set; }
        public string WorkingDirectory { get; set; }
    }
    
    public class OrchestrationMessageRequest
    {
        public string SessionId { get; set; }
        public string Message { get; set; }
        public List<string> AgentIds { get; set; }
        public OrchestrationStrategy Strategy { get; set; }
    }
    
    public class AgentMessageRequest
    {
        public string AgentId { get; set; }
        public string SessionId { get; set; }
        public string Content { get; set; }
        public List<Attachment> Attachments { get; set; }
    }
    
    public class ToolExecutionRequest
    {
        public string AgentId { get; set; }
        public string SessionId { get; set; }
        public string ToolName { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }
    
    // Response DTOs
    public class SessionCreatedResponse
    {
        public bool Success { get; set; }
        public string SessionId { get; set; }
        public Session Session { get; set; }
        public string Error { get; set; }
    }
    
    public class AgentResponseDto
    {
        public string AgentId { get; set; }
        public string SessionId { get; set; }
        public AgentResponse Response { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
    
    public class AgentStatusDto
    {
        public string AgentId { get; set; }
        public AgentStatus Status { get; set; }
        public AgentCapabilities Capabilities { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
    
    public class ToolExecutionResponse
    {
        public bool Success { get; set; }
        public string Output { get; set; }
        public string Error { get; set; }
        public TimeSpan ExecutionTime { get; set; }
    }
    
    public class ToolExecutionUpdate
    {
        public string ToolName { get; set; }
        public string Status { get; set; }
        public double Progress { get; set; }
        public string Message { get; set; }
    }
    
    public class ErrorResponse
    {
        public string Error { get; set; }
        public string AgentId { get; set; }
        public string SessionId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
    
    public class ConnectionInfo
    {
        public string ConnectionId { get; set; }
        public DateTime ConnectedAt { get; set; }
    }
}
```

### Connection Management

```csharp
namespace OrchestratorChat.SignalR.Services
{
    public interface IConnectionManager
    {
        void AddConnection(string connectionId, string userId);
        void RemoveConnection(string connectionId);
        string GetUserId(string connectionId);
        List<string> GetConnectionIds(string userId);
        bool IsUserOnline(string userId);
    }
    
    public class ConnectionManager : IConnectionManager
    {
        private readonly ConcurrentDictionary<string, string> _connectionToUser = new();
        private readonly ConcurrentDictionary<string, HashSet<string>> _userToConnections = new();
        private readonly ILogger<ConnectionManager> _logger;
        
        public void AddConnection(string connectionId, string userId)
        {
            _connectionToUser[connectionId] = userId;
            
            _userToConnections.AddOrUpdate(userId,
                new HashSet<string> { connectionId },
                (key, oldValue) =>
                {
                    oldValue.Add(connectionId);
                    return oldValue;
                });
            
            _logger.LogInformation($"User {userId} connected with {connectionId}");
        }
        
        public void RemoveConnection(string connectionId)
        {
            if (_connectionToUser.TryRemove(connectionId, out var userId))
            {
                if (_userToConnections.TryGetValue(userId, out var connections))
                {
                    connections.Remove(connectionId);
                    if (connections.Count == 0)
                    {
                        _userToConnections.TryRemove(userId, out _);
                    }
                }
                
                _logger.LogInformation($"User {userId} disconnected {connectionId}");
            }
        }
        
        public string GetUserId(string connectionId)
        {
            return _connectionToUser.GetValueOrDefault(connectionId);
        }
        
        public List<string> GetConnectionIds(string userId)
        {
            return _userToConnections.GetValueOrDefault(userId)?.ToList() ?? new List<string>();
        }
        
        public bool IsUserOnline(string userId)
        {
            return _userToConnections.ContainsKey(userId);
        }
    }
}
```

### Stream Management

```csharp
namespace OrchestratorChat.SignalR.Streaming
{
    public interface IStreamManager
    {
        Task<string> CreateStream(string sessionId, string agentId);
        Task WriteToStream(string streamId, object data);
        Task CloseStream(string streamId);
        IAsyncEnumerable<T> GetStream<T>(string streamId);
    }
    
    public class StreamManager : IStreamManager
    {
        private readonly ConcurrentDictionary<string, Channel<object>> _streams = new();
        private readonly ILogger<StreamManager> _logger;
        
        public Task<string> CreateStream(string sessionId, string agentId)
        {
            var streamId = $"{sessionId}-{agentId}-{Guid.NewGuid()}";
            var channel = Channel.CreateUnbounded<object>(new UnboundedChannelOptions
            {
                SingleWriter = true,
                SingleReader = false
            });
            
            _streams[streamId] = channel;
            return Task.FromResult(streamId);
        }
        
        public async Task WriteToStream(string streamId, object data)
        {
            if (_streams.TryGetValue(streamId, out var channel))
            {
                await channel.Writer.WriteAsync(data);
            }
        }
        
        public Task CloseStream(string streamId)
        {
            if (_streams.TryRemove(streamId, out var channel))
            {
                channel.Writer.TryComplete();
            }
            return Task.CompletedTask;
        }
        
        public async IAsyncEnumerable<T> GetStream<T>(string streamId)
        {
            if (_streams.TryGetValue(streamId, out var channel))
            {
                await foreach (var item in channel.Reader.ReadAllAsync())
                {
                    if (item is T typedItem)
                    {
                        yield return typedItem;
                    }
                }
            }
        }
    }
}
```

### SignalR Configuration

```csharp
namespace OrchestratorChat.SignalR
{
    public static class SignalRConfiguration
    {
        public static IServiceCollection AddOrchestratorSignalR(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Add SignalR
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = configuration.GetValue<bool>("SignalR:EnableDetailedErrors");
                options.MaximumReceiveMessageSize = configuration.GetValue<long>("SignalR:MaximumReceiveMessageSize", 102400);
                options.StreamBufferCapacity = configuration.GetValue<int>("SignalR:StreamBufferCapacity", 10);
                options.KeepAliveInterval = TimeSpan.FromSeconds(configuration.GetValue<int>("SignalR:KeepAliveInterval", 15));
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(configuration.GetValue<int>("SignalR:ClientTimeoutInterval", 30));
            })
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.PayloadSerializerOptions.WriteIndented = false;
                options.PayloadSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });
            
            // Add connection management
            services.AddSingleton<IConnectionManager, ConnectionManager>();
            services.AddSingleton<IStreamManager, StreamManager>();
            
            // Add hosted service for cleanup
            services.AddHostedService<SignalRCleanupService>();
            
            return services;
        }
        
        public static IApplicationBuilder UseOrchestratorSignalR(
            this IApplicationBuilder app)
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<OrchestratorHub>("/hubs/orchestrator");
                endpoints.MapHub<AgentHub>("/hubs/agent");
            });
            
            return app;
        }
    }
    
    public class SignalRCleanupService : BackgroundService
    {
        private readonly IConnectionManager _connectionManager;
        private readonly IStreamManager _streamManager;
        private readonly ILogger<SignalRCleanupService> _logger;
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Cleanup orphaned streams every 5 minutes
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    
                    // Perform cleanup logic
                    _logger.LogInformation("Running SignalR cleanup");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in SignalR cleanup");
                }
            }
        }
    }
}
```

## JavaScript/TypeScript Client

```typescript
// signalr-client.ts
import * as signalR from "@microsoft/signalr";

export class OrchestratorClient {
    private orchestratorConnection: signalR.HubConnection;
    private agentConnection: signalR.HubConnection;
    
    constructor(private baseUrl: string) {
        this.orchestratorConnection = new signalR.HubConnectionBuilder()
            .withUrl(`${baseUrl}/hubs/orchestrator`)
            .withAutomaticReconnect()
            .build();
            
        this.agentConnection = new signalR.HubConnectionBuilder()
            .withUrl(`${baseUrl}/hubs/agent`)
            .withAutomaticReconnect()
            .build();
    }
    
    async connect(): Promise<void> {
        await this.orchestratorConnection.start();
        await this.agentConnection.start();
        
        this.setupEventHandlers();
    }
    
    private setupEventHandlers(): void {
        // Orchestrator events
        this.orchestratorConnection.on("Connected", (info) => {
            console.log("Connected to orchestrator", info);
        });
        
        this.orchestratorConnection.on("SessionCreated", (session) => {
            console.log("Session created", session);
        });
        
        this.orchestratorConnection.on("OrchestrationProgress", (progress) => {
            console.log("Orchestration progress", progress);
        });
        
        // Agent events
        this.agentConnection.on("ReceiveAgentResponse", (response) => {
            console.log("Agent response", response);
        });
        
        this.agentConnection.on("AgentStatusUpdate", (status) => {
            console.log("Agent status", status);
        });
    }
    
    async createSession(request: CreateSessionRequest): Promise<SessionCreatedResponse> {
        return await this.orchestratorConnection.invoke("CreateSession", request);
    }
    
    async sendAgentMessage(request: AgentMessageRequest): Promise<void> {
        await this.agentConnection.invoke("SendAgentMessage", request);
    }
    
    async subscribeToAgent(agentId: string): Promise<void> {
        await this.agentConnection.invoke("SubscribeToAgent", agentId);
    }
}
```

## Blazor Client Integration

```csharp
@page "/chat/{SessionId}"
@using Microsoft.AspNetCore.SignalR.Client
@implements IAsyncDisposable

<div class="chat-container">
    @foreach (var message in messages)
    {
        <MessageComponent Message="@message" />
    }
</div>

<InputArea OnSendMessage="SendMessage" />

@code {
    [Parameter] public string SessionId { get; set; }
    
    private HubConnection orchestratorHub;
    private HubConnection agentHub;
    private List<ChatMessage> messages = new();
    
    protected override async Task OnInitializedAsync()
    {
        orchestratorHub = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri("/hubs/orchestrator"))
            .Build();
            
        agentHub = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri("/hubs/agent"))
            .Build();
            
        // Set up event handlers
        agentHub.On<AgentResponseDto>("ReceiveAgentResponse", (response) =>
        {
            messages.Add(new ChatMessage
            {
                Content = response.Response.Content,
                AgentId = response.AgentId,
                Timestamp = response.Timestamp
            });
            InvokeAsync(StateHasChanged);
        });
        
        await orchestratorHub.StartAsync();
        await agentHub.StartAsync();
        
        // Join session
        await orchestratorHub.InvokeAsync("JoinSession", SessionId);
    }
    
    private async Task SendMessage(string content)
    {
        await agentHub.InvokeAsync("SendAgentMessage", new AgentMessageRequest
        {
            SessionId = SessionId,
            Content = content,
            AgentId = "default-agent"
        });
    }
    
    public async ValueTask DisposeAsync()
    {
        if (orchestratorHub is not null)
        {
            await orchestratorHub.DisposeAsync();
        }
        if (agentHub is not null)
        {
            await agentHub.DisposeAsync();
        }
    }
}
```

## Testing

```csharp
[TestClass]
public class SignalRHubTests
{
    [TestMethod]
    public async Task OrchestratorHub_CreateSession_Success()
    {
        // Arrange
        var mockOrchestrator = new Mock<IOrchestrator>();
        var mockSessionManager = new Mock<ISessionManager>();
        var hub = new OrchestratorHub(
            mockOrchestrator.Object,
            mockSessionManager.Object,
            /* other dependencies */);
        
        // Act
        var result = await hub.CreateSession(new CreateSessionRequest
        {
            Name = "Test Session",
            Type = SessionType.MultiAgent
        });
        
        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.SessionId);
    }
}
```

## Performance Considerations

### Scalability
- Use Redis backplane for multi-server deployment
- Implement connection throttling
- Use streaming for large data transfers
- Batch small messages when possible

### Monitoring
- Track connection count
- Monitor message throughput
- Log slow operations
- Alert on connection failures

## Security

### Authentication
- Require JWT tokens for connection
- Validate user permissions per hub method
- Implement rate limiting

### Data Protection
- Sanitize all user inputs
- Encrypt sensitive data in transit
- Validate message size limits

## Next Steps
1. Implement hub classes
2. Set up client libraries
3. Create integration tests
4. Add monitoring and metrics
5. Document client usage patterns

## Version History
- v1.0 - Initial specification
- Date: 2024-01-30
- Status: Ready for implementation