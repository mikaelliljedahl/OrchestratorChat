using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Core.Messages;
using OrchestratorChat.Core.Tools;
using OrchestratorChat.SignalR.Clients;
using OrchestratorChat.SignalR.Contracts.Requests;
using OrchestratorChat.SignalR.Contracts.Responses;
using OrchestratorChat.Data.Repositories;
using OrchestratorChat.Data.Models;
using IAgentFactory = OrchestratorChat.Core.Agents.IAgentFactory;

namespace OrchestratorChat.SignalR.Hubs
{
    /// <summary>
    /// Hub for individual agent communication
    /// </summary>
    public class AgentHub : Hub<IAgentClient>
    {
        private readonly IAgentRegistry _agentRegistry;
        private readonly IAgentRepository _agentRepository;
        private readonly ISessionManager _sessionManager;
        private readonly ILogger<AgentHub> _logger;
        private readonly IHubContext<AgentHub, IAgentClient> _agentHubContext;

        /// <summary>
        /// Initializes a new instance of AgentHub
        /// </summary>
        public AgentHub(
            IAgentRegistry agentRegistry,
            IAgentRepository agentRepository,
            ISessionManager sessionManager,
            ILogger<AgentHub> logger,
            IHubContext<AgentHub, IAgentClient> agentHubContext)
        {
            _agentRegistry = agentRegistry ?? throw new ArgumentNullException(nameof(agentRegistry));
            _agentRepository = agentRepository ?? throw new ArgumentNullException(nameof(agentRepository));
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

                // Get or create agent using registry
                var agent = await _agentRegistry.GetOrCreateAsync(request.AgentId, async () =>
                {
                    var stored = await _agentRepository.GetWithConfigurationAsync(request.AgentId);
                    if (stored == null)
                    {
                        throw new InvalidOperationException($"Agent {request.AgentId} not found. Please create the agent first through the UI.");
                    }

                    var cfg = MapToCoreConfiguration(stored);
                    return (stored.Type, cfg);
                });

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
                var responseStream = await agent.SendMessageStreamAsync(message);
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
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                _logger.LogWarning("Agent {AgentId} not found in repository", request.AgentId);

                await Clients.Caller.ReceiveError(new ErrorResponse
                {
                    Error = ex.Message,
                    AgentId = request.AgentId,
                    SessionId = request.SessionId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message to agent {AgentId}", request.AgentId);

                var friendlyError = GetFriendlyErrorMessage(ex);
                await Clients.Caller.ReceiveError(new ErrorResponse
                {
                    Error = friendlyError,
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

                var agent = await _agentRegistry.GetOrCreateAsync(request.AgentId, async () =>
                {
                    var stored = await _agentRepository.GetWithConfigurationAsync(request.AgentId);
                    if (stored == null)
                    {
                        throw new InvalidOperationException($"Agent {request.AgentId} not found. Please create the agent first through the UI.");
                    }

                    var cfg = MapToCoreConfiguration(stored);
                    return (stored.Type, cfg);
                });

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

                var agent = await _agentRegistry.FindAsync(agentId);
                if (agent != null)
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
        /// Maps repository entity to Core configuration
        /// </summary>
        /// <param name="stored">Stored agent entity with configuration</param>
        /// <returns>Core agent configuration</returns>
        private static AgentConfiguration MapToCoreConfiguration(AgentEntity stored)
        {
            var config = stored.Configuration;
            if (config == null)
            {
                throw new InvalidOperationException($"Agent {stored.Id} has no configuration");
            }

            var coreConfig = new AgentConfiguration
            {
                Name = stored.Name,
                Type = stored.Type,
                WorkingDirectory = stored.WorkingDirectory,
                Model = config.Model,
                Temperature = config.Temperature,
                MaxTokens = config.MaxTokens,
                SystemPrompt = config.SystemPrompt,
                RequireApproval = config.RequireApproval
            };

            // Deserialize custom settings
            if (!string.IsNullOrEmpty(config.CustomSettingsJson))
            {
                try
                {
                    var customSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(config.CustomSettingsJson);
                    if (customSettings != null)
                    {
                        coreConfig.CustomSettings = customSettings;
                    }
                }
                catch (JsonException ex)
                {
                    throw new InvalidOperationException($"Invalid custom settings JSON for agent {stored.Id}", ex);
                }
            }

            // Deserialize enabled tools
            if (!string.IsNullOrEmpty(config.EnabledToolsJson))
            {
                try
                {
                    var enabledTools = JsonSerializer.Deserialize<List<string>>(config.EnabledToolsJson);
                    if (enabledTools != null)
                    {
                        coreConfig.EnabledTools = enabledTools;
                    }
                }
                catch (JsonException ex)
                {
                    throw new InvalidOperationException($"Invalid enabled tools JSON for agent {stored.Id}", ex);
                }
            }

            return coreConfig;
        }

        /// <summary>
        /// Provides user-friendly error messages for common issues
        /// </summary>
        /// <param name="exception">The original exception</param>
        /// <returns>Friendly error message</returns>
        private static string GetFriendlyErrorMessage(Exception exception)
        {
            return exception.Message switch
            {
                var msg when msg.Contains("claude") && msg.Contains("not found") =>
                    "Claude CLI is not installed or not found in PATH. Please install Claude CLI and ensure it's authenticated.",
                
                var msg when msg.Contains("API key") || msg.Contains("authentication") =>
                    "API key is missing or invalid. Please check your OpenRouter API key or Claude authentication.",
                
                var msg when msg.Contains("timeout") =>
                    "The agent took too long to respond. Please try again.",
                
                var msg when msg.Contains("connection") =>
                    "Unable to connect to the agent service. Please check your internet connection and try again.",
                
                _ => exception.Message
            };
        }
    }
}