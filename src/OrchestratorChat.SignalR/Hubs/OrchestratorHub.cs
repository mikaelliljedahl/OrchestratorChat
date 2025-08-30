using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Core.Orchestration;
using OrchestratorChat.SignalR.Clients;
using OrchestratorChat.SignalR.Contracts.Requests;
using OrchestratorChat.SignalR.Contracts.Responses;
using IAgentFactory = OrchestratorChat.Core.Agents.IAgentFactory;

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

        /// <summary>
        /// Initializes a new instance of OrchestratorHub
        /// </summary>
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

            _logger.LogInformation("Client {ConnectionId} connected to orchestrator hub", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Client disconnects from orchestrator
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (exception != null)
            {
                _logger.LogWarning(exception, "Client {ConnectionId} disconnected with exception", Context.ConnectionId);
            }
            else
            {
                _logger.LogInformation("Client {ConnectionId} disconnected from orchestrator hub", Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Create a new orchestration session
        /// </summary>
        /// <param name="request">Session creation request</param>
        /// <returns>Session creation response</returns>
        public async Task<SessionCreatedResponse> CreateSession(Contracts.Requests.CreateSessionRequest request)
        {
            try
            {
                _logger.LogInformation("Creating session {SessionName} of type {SessionType}", request.Name, request.Type);

                var coreRequest = new Core.Sessions.CreateSessionRequest
                {
                    Name = request.Name,
                    Type = request.Type,
                    AgentIds = request.AgentIds,
                    WorkingDirectory = request.WorkingDirectory
                };

                var session = await _sessionManager.CreateSessionAsync(coreRequest);

                // Add to session group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"session-{session.Id}");

                // Notify session created
                await Clients.Group($"session-{session.Id}").SessionCreated(session);

                _logger.LogInformation("Successfully created session {SessionId}", session.Id);

                return new SessionCreatedResponse
                {
                    Success = true,
                    SessionId = session.Id,
                    Session = session
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create session {SessionName}", request.Name);
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
        /// <param name="request">Orchestration message request</param>
        public async Task SendOrchestrationMessage(OrchestrationMessageRequest request)
        {
            try
            {
                _logger.LogInformation("Processing orchestration message for session {SessionId}", request.SessionId);

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
                    try
                    {
                        _logger.LogInformation("Executing orchestration plan {PlanId} for session {SessionId}", 
                            plan.Id, request.SessionId);

                        var result = await _orchestrator.ExecutePlanAsync(plan, progress);
                        
                        await Clients.Group($"session-{request.SessionId}")
                            .OrchestrationCompleted(result);

                        _logger.LogInformation("Completed orchestration plan {PlanId}", plan.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to execute orchestration plan {PlanId}", plan.Id);
                        
                        await Clients.Group($"session-{request.SessionId}")
                            .ReceiveError(new ErrorResponse
                            {
                                Error = $"Orchestration failed: {ex.Message}",
                                SessionId = request.SessionId
                            });
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process orchestration message for session {SessionId}", request.SessionId);
                
                await Clients.Caller.ReceiveError(new ErrorResponse
                {
                    Error = ex.Message,
                    SessionId = request.SessionId
                });
            }
        }

        /// <summary>
        /// Join an existing session
        /// </summary>
        /// <param name="sessionId">ID of the session to join</param>
        public async Task JoinSession(string sessionId)
        {
            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"session-{sessionId}");
                var session = await _sessionManager.GetSessionAsync(sessionId);
                
                if (session != null)
                {
                    await Clients.Caller.SessionJoined(session);
                    _logger.LogInformation("Client {ConnectionId} joined session {SessionId}", 
                        Context.ConnectionId, sessionId);
                }
                else
                {
                    await Clients.Caller.ReceiveError(new ErrorResponse
                    {
                        Error = $"Session {sessionId} not found",
                        SessionId = sessionId
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to join session {SessionId}", sessionId);
                
                await Clients.Caller.ReceiveError(new ErrorResponse
                {
                    Error = ex.Message,
                    SessionId = sessionId
                });
            }
        }

        /// <summary>
        /// Leave session
        /// </summary>
        /// <param name="sessionId">ID of the session to leave</param>
        public async Task LeaveSession(string sessionId)
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session-{sessionId}");
                _logger.LogInformation("Client {ConnectionId} left session {SessionId}", 
                    Context.ConnectionId, sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to leave session {SessionId}", sessionId);
            }
        }
    }
}