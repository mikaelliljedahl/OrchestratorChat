using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Core.Messages;
using OrchestratorChat.Core.Tools;
using OrchestratorChat.SignalR.Clients;
using OrchestratorChat.SignalR.Contracts.Requests;
using OrchestratorChat.SignalR.Contracts.Responses;
using IAgentFactory = OrchestratorChat.Core.Agents.IAgentFactory;

namespace OrchestratorChat.SignalR.Hubs
{
    /// <summary>
    /// Hub for individual agent communication
    /// </summary>
    public class AgentHub : Hub<IAgentClient>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ISessionManager _sessionManager;
        private readonly ILogger<AgentHub> _logger;
        private readonly IHubContext<AgentHub, IAgentClient> _agentHubContext;
        private static readonly ConcurrentDictionary<string, IAgent> _activeAgents = new();

        /// <summary>
        /// Initializes a new instance of AgentHub
        /// </summary>
        public AgentHub(
            IServiceProvider serviceProvider,
            ISessionManager sessionManager,
            ILogger<AgentHub> logger,
            IHubContext<AgentHub, IAgentClient> agentHubContext)
        {
            _serviceProvider = serviceProvider;
            _sessionManager = sessionManager;
            _logger = logger;
            _agentHubContext = agentHubContext;
        }

        /// <summary>
        /// Client connects to agent hub
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Client {ConnectionId} connected to agent hub", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Client disconnects from agent hub
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (exception != null)
            {
                _logger.LogWarning(exception, "Client {ConnectionId} disconnected with exception", Context.ConnectionId);
            }
            else
            {
                _logger.LogInformation("Client {ConnectionId} disconnected from agent hub", Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Send message directly to a specific agent
        /// </summary>
        /// <param name="request">Agent message request</param>
        public async Task SendAgentMessage(AgentMessageRequest request)
        {
            try
            {
                _logger.LogInformation("Sending message to agent {AgentId} in session {SessionId}", 
                    request.AgentId, request.SessionId);

                // Get or create agent
                var agent = await GetOrCreateAgentAsync(request.AgentId);

                // Create message
                var message = new AgentMessage
                {
                    Content = request.Content,
                    Role = MessageRole.User,
                    SessionId = request.SessionId,
                    AgentId = request.AgentId,
                    Attachments = request.Attachments,
                    Metadata = !string.IsNullOrEmpty(request.CommandId) 
                        ? new Dictionary<string, object> { ["CommandId"] = request.CommandId }
                        : new Dictionary<string, object>()
                };

                // Add to session history
                var session = await _sessionManager.GetSessionAsync(request.SessionId);
                if (session != null)
                {
                    session.Messages.Add(message);
                    await _sessionManager.UpdateSessionAsync(session);
                }

                // Stream responses
                var responseStream = await agent.SendMessageAsync(message);
                await foreach (var response in responseStream)
                {
                    var responseDto = new AgentResponseDto
                    {
                        AgentId = request.AgentId,
                        SessionId = request.SessionId,
                        Response = response
                    };

                    await Clients.Group($"agent-{request.AgentId}")
                        .ReceiveAgentResponse(responseDto);

                    // Also send to session group
                    await Clients.Group($"session-{request.SessionId}")
                        .ReceiveAgentResponse(responseDto);
                }

                _logger.LogInformation("Completed message processing for agent {AgentId}", request.AgentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message to agent {AgentId}", request.AgentId);

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
        /// <param name="request">Tool execution request</param>
        /// <returns>Tool execution response</returns>
        public async Task<ToolExecutionResponse> ExecuteTool(ToolExecutionRequest request)
        {
            try
            {
                _logger.LogInformation("Executing tool {ToolName} on agent {AgentId}", 
                    request.ToolName, request.AgentId);

                var agent = await GetOrCreateAgentAsync(request.AgentId);

                var result = await agent.ExecuteToolAsync(new ToolCall
                {
                    ToolName = request.ToolName,
                    Parameters = request.Parameters,
                    AgentId = request.AgentId,
                    SessionId = request.SessionId
                });

                _logger.LogInformation("Tool {ToolName} executed successfully on agent {AgentId}", 
                    request.ToolName, request.AgentId);

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
                _logger.LogError(ex, "Failed to execute tool {ToolName} on agent {AgentId}", 
                    request.ToolName, request.AgentId);

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
        /// <param name="agentId">Agent ID to subscribe to</param>
        public async Task SubscribeToAgent(string agentId)
        {
            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"agent-{agentId}");
                _logger.LogInformation("Client {ConnectionId} subscribed to agent {AgentId}", 
                    Context.ConnectionId, agentId);

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to subscribe to agent {AgentId}", agentId);
            }
        }

        /// <summary>
        /// Unsubscribe from agent updates
        /// </summary>
        /// <param name="agentId">Agent ID to unsubscribe from</param>
        public async Task UnsubscribeFromAgent(string agentId)
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"agent-{agentId}");
                _logger.LogInformation("Client {ConnectionId} unsubscribed from agent {AgentId}", 
                    Context.ConnectionId, agentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unsubscribe from agent {AgentId}", agentId);
            }
        }

        /// <summary>
        /// Get or create an agent instance
        /// </summary>
        /// <param name="agentId">Agent ID</param>
        /// <returns>Agent instance</returns>
        private async Task<IAgent> GetOrCreateAgentAsync(string agentId)
        {
            if (_activeAgents.TryGetValue(agentId, out var agent))
                return agent;

            // Create agent based on configuration
            // This would typically load from database or configuration
            var factory = _serviceProvider.GetRequiredService<IAgentFactory>();
            agent = await factory.CreateAgentAsync(AgentType.Claude, new AgentConfiguration());

            _activeAgents[agentId] = agent;

            // Hook up events
            agent.StatusChanged += async (s, e) =>
            {
                try
                {
                    await _agentHubContext.Clients.Group($"agent-{agentId}")
                        .AgentStatusUpdate(new AgentStatusDto
                        {
                            AgentId = agentId,
                            Status = e.NewStatus
                        });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send status update for agent {AgentId}", agentId);
                }
            };

            _logger.LogInformation("Created and cached agent {AgentId}", agentId);
            return agent;
        }
    }
}