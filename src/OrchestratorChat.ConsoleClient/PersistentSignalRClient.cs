using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Core.Orchestration;
using OrchestratorChat.SignalR.Clients;
using OrchestratorChat.SignalR.Contracts.Responses;
using OrchestratorChat.ConsoleClient.Models;
using OrchestratorChat.ConsoleClient.Services;

namespace OrchestratorChat.ConsoleClient
{
    /// <summary>
    /// Persistent SignalR client that maintains connections to OrchestratorHub and AgentHub
    /// </summary>
    public class PersistentSignalRClient : IHostedService, IOrchestratorClient, IAgentClient
    {
        private readonly ILogger<PersistentSignalRClient> _logger;
        private readonly MessageHandler _messageHandler;
        private readonly string _serverUrl;
        private readonly string _defaultAgentId;
        private readonly string _sessionName;

        private HubConnection? _orchestratorConnection;
        private HubConnection? _agentConnection;
        private string? _currentSessionId;
        private CancellationTokenSource? _cancellationTokenSource;

        public PersistentSignalRClient(
            ILogger<PersistentSignalRClient> logger, 
            MessageHandler messageHandler,
            string serverUrl,
            string defaultAgentId,
            string sessionName)
        {
            _logger = logger;
            _messageHandler = messageHandler;
            _serverUrl = serverUrl;
            _defaultAgentId = defaultAgentId;
            _sessionName = sessionName;
        }

        /// <summary>
        /// Gets the current session ID
        /// </summary>
        public string? CurrentSessionId => _currentSessionId;

        /// <summary>
        /// Gets connection status
        /// </summary>
        public bool IsConnected => 
            _orchestratorConnection?.State == HubConnectionState.Connected &&
            _agentConnection?.State == HubConnectionState.Connected;

        /// <summary>
        /// Start the hosted service and establish connections
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _logger.LogInformation("Starting SignalR client connections to {ServerUrl}", _serverUrl);

            try
            {
                await ConnectToHubsAsync();
                await CreateOrJoinSessionAsync();
                
                // Start background reconnection monitoring
                _ = Task.Run(MonitorConnectionsAsync, _cancellationTokenSource.Token);
                
                _logger.LogInformation("SignalR client started successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start SignalR client");
                throw;
            }
        }

        /// <summary>
        /// Stop the hosted service and close connections
        /// </summary>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping SignalR client");

            _cancellationTokenSource?.Cancel();

            try
            {
                if (_orchestratorConnection != null)
                {
                    await _orchestratorConnection.StopAsync(cancellationToken);
                    await _orchestratorConnection.DisposeAsync();
                }

                if (_agentConnection != null)
                {
                    await _agentConnection.StopAsync(cancellationToken);
                    await _agentConnection.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error stopping SignalR connections");
            }

            _cancellationTokenSource?.Dispose();
            _logger.LogInformation("SignalR client stopped");
        }

        /// <summary>
        /// Send a message to an agent in the current session
        /// </summary>
        public async Task<bool> SendMessageToAgentAsync(ClientCommand command)
        {
            if (!IsConnected || _currentSessionId == null)
            {
                _logger.LogWarning("Cannot send message - not connected or no active session");
                return false;
            }

            try
            {
                var request = new AgentMessageRequest
                {
                    AgentId = command.AgentId,
                    SessionId = _currentSessionId,
                    Content = command.Message,
                    Attachments = new(),
                    CommandId = command.CommandId
                };

                await _agentConnection!.InvokeAsync("SendAgentMessage", request);
                _logger.LogInformation("Sent message to agent {AgentId}", command.AgentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message to agent {AgentId}", command.AgentId);
                return false;
            }
        }

        /// <summary>
        /// Connect to both SignalR hubs
        /// </summary>
        private async Task ConnectToHubsAsync()
        {
            // Build orchestrator connection
            _orchestratorConnection = new HubConnectionBuilder()
                .WithUrl($"{_serverUrl}/orchestratorhub")
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
                .Build();

            // Build agent connection
            _agentConnection = new HubConnectionBuilder()
                .WithUrl($"{_serverUrl}/agenthub")
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
                .Build();

            // Register client methods for orchestrator hub
            _orchestratorConnection.On<ConnectionInfo>("Connected", Connected);
            _orchestratorConnection.On<Session>("SessionCreated", SessionCreated);
            _orchestratorConnection.On<Session>("SessionJoined", SessionJoined);
            _orchestratorConnection.On<OrchestrationPlan>("OrchestrationPlanCreated", OrchestrationPlanCreated);
            _orchestratorConnection.On<OrchestrationProgress>("OrchestrationProgress", OrchestrationProgress);
            _orchestratorConnection.On<OrchestrationResult>("OrchestrationCompleted", OrchestrationCompleted);
            _orchestratorConnection.On<ErrorResponse>("ReceiveError", ReceiveError);

            // Register client methods for agent hub
            _agentConnection.On<AgentResponseDto>("ReceiveAgentResponse", ReceiveAgentResponse);
            _agentConnection.On<AgentStatusDto>("AgentStatusUpdate", AgentStatusUpdate);
            _agentConnection.On<ToolExecutionUpdate>("ToolExecutionUpdate", ToolExecutionUpdate);
            _agentConnection.On<ErrorResponse>("ReceiveError", ReceiveError);

            // Start connections
            await _orchestratorConnection.StartAsync();
            await _agentConnection.StartAsync();

            _logger.LogInformation("Connected to SignalR hubs");
        }

        /// <summary>
        /// Create or join the designated session
        /// </summary>
        private async Task CreateOrJoinSessionAsync()
        {
            try
            {
                var createRequest = new CreateSessionRequest
                {
                    Name = _sessionName,
                    Type = SessionType.MultiAgent,
                    AgentIds = new List<string> { _defaultAgentId },
                    WorkingDirectory = Environment.CurrentDirectory
                };

                var response = await _orchestratorConnection!.InvokeAsync<SessionCreatedResponse>("CreateSession", createRequest);
                
                if (response.Success && response.SessionId != null)
                {
                    _currentSessionId = response.SessionId;
                    _logger.LogInformation("Created session {SessionId} with name '{SessionName}'", _currentSessionId, _sessionName);
                }
                else
                {
                    _logger.LogError("Failed to create session: {Error}", response.Error);
                    throw new InvalidOperationException($"Failed to create session: {response.Error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create or join session");
                throw;
            }
        }

        /// <summary>
        /// Monitor connections and attempt reconnection with exponential backoff
        /// </summary>
        private async Task MonitorConnectionsAsync()
        {
            var backoffDelay = TimeSpan.FromSeconds(1);
            const int maxBackoffSeconds = 60;

            while (!_cancellationTokenSource!.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), _cancellationTokenSource.Token);

                    if (!IsConnected)
                    {
                        _logger.LogWarning("Connection lost, attempting to reconnect...");
                        
                        await ConnectToHubsAsync();
                        
                        if (_currentSessionId == null)
                        {
                            await CreateOrJoinSessionAsync();
                        }

                        backoffDelay = TimeSpan.FromSeconds(1); // Reset backoff on successful reconnection
                        _logger.LogInformation("Reconnection successful");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Reconnection attempt failed, will retry in {Delay}", backoffDelay);
                    
                    await Task.Delay(backoffDelay, _cancellationTokenSource.Token);
                    
                    // Exponential backoff with jitter
                    var nextDelay = TimeSpan.FromSeconds(Math.Min(backoffDelay.TotalSeconds * 2, maxBackoffSeconds));
                    backoffDelay = nextDelay;
                }
            }
        }

        #region IOrchestratorClient Implementation

        public Task Connected(ConnectionInfo info)
        {
            _logger.LogInformation("Connected to orchestrator hub with connection ID: {ConnectionId}", info.ConnectionId);
            return Task.CompletedTask;
        }

        public Task SessionCreated(Session session)
        {
            _logger.LogInformation("Session created: {SessionId}", session.Id);
            _currentSessionId = session.Id;
            return Task.CompletedTask;
        }

        public Task SessionJoined(Session session)
        {
            _logger.LogInformation("Joined session: {SessionId}", session.Id);
            _currentSessionId = session.Id;
            return Task.CompletedTask;
        }

        public Task OrchestrationPlanCreated(OrchestrationPlan plan)
        {
            _logger.LogInformation("Orchestration plan created: {PlanId}", plan.Id);
            return Task.CompletedTask;
        }

        public Task OrchestrationProgress(OrchestrationProgress progress)
        {
            _logger.LogInformation("Orchestration progress: {CurrentTask} ({CurrentStep}/{TotalSteps})", 
                progress.CurrentTask, progress.CurrentStep, progress.TotalSteps);
            return Task.CompletedTask;
        }

        public Task OrchestrationCompleted(OrchestrationResult result)
        {
            _logger.LogInformation("Orchestration completed successfully: {Success}", result.Success);
            return Task.CompletedTask;
        }

        public Task ReceiveError(ErrorResponse error)
        {
            _logger.LogError("Received error: {Error} for session {SessionId}", error.Error, error.SessionId);
            return Task.CompletedTask;
        }

        #endregion

        #region IAgentClient Implementation

        public Task ReceiveAgentResponse(AgentResponseDto response)
        {
            _logger.LogInformation("Received response from agent {AgentId}", response.AgentId);
            return _messageHandler.HandleAgentResponseAsync(response);
        }

        public Task AgentStatusUpdate(AgentStatusDto status)
        {
            _logger.LogInformation("Agent {AgentId} status update: {Status}", status.AgentId, status.Status);
            return Task.CompletedTask;
        }

        public Task ToolExecutionUpdate(ToolExecutionUpdate update)
        {
            _logger.LogInformation("Tool execution update: {Status}", update.Status);
            return Task.CompletedTask;
        }

        #endregion
    }
}