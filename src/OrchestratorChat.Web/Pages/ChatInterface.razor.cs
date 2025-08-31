using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Messages;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Web.Models;
using OrchestratorChat.Web.Services;
using SignalRResponses = OrchestratorChat.SignalR.Contracts.Responses;
using SignalRRequests = OrchestratorChat.SignalR.Contracts.Requests;
using System.Threading;

namespace OrchestratorChat.Web.Pages;

public partial class ChatInterface : ComponentBase, IAsyncDisposable
{
    [Parameter] public string? AgentId { get; set; }
    [Parameter] public string? SessionId { get; set; }
    
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] private IAgentService AgentService { get; set; } = null!;
    [Inject] private ISessionManager SessionManager { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private ILogger<ChatInterface> Logger { get; set; } = null!;
    
    private HubConnection? _agentHub;
    private HubConnection? _orchestratorHub;
    private List<AgentInfo> _agents = new();
    private AgentInfo? _selectedAgent;
    private Session? _currentSession;
    private string _sessionName = "Chat";
    private List<ChatMessage> _messages = new();
    private bool _isProcessing = false;
    private bool _isTyping = false;
    private bool _showHistory = false;
    private bool _showCreateAgentDialog = false;
    private ElementReference _messagesContainer;
    private string? _errorMessage;
    private bool _showHealthDiagnostics = false;
    
    // Subscription tracking for proper cleanup
    private readonly HashSet<string> _subscribedAgentIds = new();
    private string? _joinedSessionId;
    
    protected override async Task OnInitializedAsync()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        Logger.LogInformation("ChatInterface initialization started with AgentId='{AgentId}', SessionId='{SessionId}'", AgentId, SessionId);
        
        try
        {
            Logger.LogDebug("Step 1: Initializing SignalR hubs");
            await InitializeHubs();
            Logger.LogDebug("Step 1: Hubs initialized successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            
            Logger.LogDebug("Step 2: Loading agents");
            await LoadAgents();
            Logger.LogDebug("Step 2: Loaded {AgentCount} agents in {ElapsedMs}ms", _agents.Count, stopwatch.ElapsedMilliseconds);
            
            Logger.LogDebug("Step 3: Handling route parameters");
            await HandleRouteParameters();
            Logger.LogDebug("Step 3: Route parameters handled, selected agent: '{SelectedAgent}'", _selectedAgent?.Name ?? "none");
            
            Logger.LogDebug("Step 4: Loading or creating session");
            await LoadOrCreateSession();
            Logger.LogInformation("ChatInterface initialization completed successfully in {ElapsedMs}ms. Session: '{SessionName}' ({SessionId}), Agent: '{AgentName}'", 
                stopwatch.ElapsedMilliseconds, _sessionName, _currentSession?.Id, _selectedAgent?.Name);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during chat interface initialization after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            _errorMessage = $"Failed to initialize chat interface: {ex.Message}";
            _showHealthDiagnostics = true;
            StateHasChanged();
        }
    }
    
    protected override async Task OnParametersSetAsync()
    {
        // Handle parameter changes during navigation
        await HandleRouteParameters();
    }
    
    private async Task HandleRouteParameters()
    {
        Logger.LogDebug("HandleRouteParameters: AgentId='{AgentId}', SessionId='{SessionId}', current selected agent: '{CurrentAgent}'", 
            AgentId, SessionId, _selectedAgent?.Id);
            
        if (!string.IsNullOrEmpty(AgentId))
        {
            var targetAgent = _agents.FirstOrDefault(a => a.Id == AgentId);
            if (targetAgent != null && _selectedAgent?.Id != AgentId)
            {
                Logger.LogInformation("HandleRouteParameters: Selecting agent '{AgentName}' ({AgentId}) from route parameter", 
                    targetAgent.Name, targetAgent.Id);
                await SelectAgentInternal(targetAgent);
                
                // Ensure proper SignalR subscriptions according to spec
                await EnsureSignalRSubscriptions();
                StateHasChanged();
            }
            else if (targetAgent == null)
            {
                Logger.LogWarning("HandleRouteParameters: Agent with ID '{AgentId}' not found in loaded agents", AgentId);
            }
            else
            {
                Logger.LogDebug("HandleRouteParameters: Agent '{AgentId}' is already selected", AgentId);
            }
        }
    }
    
    private async Task InitializeHubs()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        Logger.LogInformation("InitializeHubs: Starting SignalR hub initialization");
        
        try
        {
            Logger.LogDebug("InitializeHubs: Creating agent hub connection to '/hubs/agent'");
            // Initialize Agent Hub
            _agentHub = new HubConnectionBuilder()
                .WithUrl(Navigation.ToAbsoluteUri("/hubs/agent"))
                .Build();
            
            // Add connection state change logging
            _agentHub.Closed += error =>
            {
                Logger.LogWarning("Agent hub connection closed. Error: {Error}", error?.Message ?? "No error");
                return Task.CompletedTask;
            };
            
            _agentHub.Reconnecting += error =>
            {
                Logger.LogInformation("Agent hub reconnecting. Error: {Error}", error?.Message ?? "No error");
                return Task.CompletedTask;
            };
            
            _agentHub.Reconnected += connectionId =>
            {
                Logger.LogInformation("Agent hub reconnected with connection ID: {ConnectionId}", connectionId);
                return Task.CompletedTask;
            };
                
            _agentHub.On<SignalRResponses.AgentResponseDto>("ReceiveAgentResponse", async (response) =>
            {
                Logger.LogDebug("ReceiveAgentResponse: Received response from agent {AgentId}, message ID {MessageId}, type {ResponseType}", 
                    response.AgentId, response.Response.MessageId, response.Response.Type);
                    
                _isTyping = false;
                _errorMessage = null;
                _showHealthDiagnostics = false; // Hide diagnostics on successful response
                
                if (response.Response.Type == ResponseType.Text)
                {
                    var existingMessage = _messages.FirstOrDefault(m => m.Id == response.Response.MessageId);
                    if (existingMessage != null)
                    {
                        Logger.LogDebug("ReceiveAgentResponse: Appending content to existing message {MessageId}", response.Response.MessageId);
                        existingMessage.Content += response.Response.Content;
                    }
                    else
                    {
                        Logger.LogDebug("ReceiveAgentResponse: Creating new message {MessageId} from agent {AgentId}", 
                            response.Response.MessageId, response.AgentId);
                        _messages.Add(new ChatMessage
                        {
                            Id = response.Response.MessageId,
                            Content = response.Response.Content,
                            Role = MessageRole.Assistant,
                            AgentId = response.AgentId,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                    
                    await InvokeAsync(StateHasChanged);
                    await ScrollToBottom();
                }
            });
            
            _agentHub.On<SignalRResponses.ErrorResponse>("ReceiveError", async (error) =>
            {
                Logger.LogError("ReceiveError: Agent error received: {Error}", error.Error);
                _isTyping = false;
                _isProcessing = false;
                _errorMessage = error.Error;
                _showHealthDiagnostics = true; // Show diagnostics when errors occur
                await InvokeAsync(StateHasChanged);
            });
            
            Logger.LogDebug("InitializeHubs: Creating orchestrator hub connection to '/hubs/orchestrator'");
            // Initialize Orchestrator Hub
            _orchestratorHub = new HubConnectionBuilder()
                .WithUrl(Navigation.ToAbsoluteUri("/hubs/orchestrator"))
                .Build();
            
            // Add connection state change logging
            _orchestratorHub.Closed += error =>
            {
                Logger.LogWarning("Orchestrator hub connection closed. Error: {Error}", error?.Message ?? "No error");
                return Task.CompletedTask;
            };
            
            _orchestratorHub.Reconnecting += error =>
            {
                Logger.LogInformation("Orchestrator hub reconnecting. Error: {Error}", error?.Message ?? "No error");
                return Task.CompletedTask;
            };
            
            _orchestratorHub.Reconnected += connectionId =>
            {
                Logger.LogInformation("Orchestrator hub reconnected with connection ID: {ConnectionId}", connectionId);
                return Task.CompletedTask;
            };
                
            _orchestratorHub.On<Session>("SessionCreated", async (session) =>
            {
                Logger.LogInformation("SessionCreated: Received session created event for session {SessionId} ({SessionName})", 
                    session.Id, session.Name);
                _currentSession = session;
                _sessionName = session.Name;
                await InvokeAsync(StateHasChanged);
            });
            
            // Start hubs with timeout
            Logger.LogDebug("InitializeHubs: Starting hub connections with 10 second timeout");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            
            var agentTask = _agentHub.StartAsync(cts.Token);
            var orchestratorTask = _orchestratorHub.StartAsync(cts.Token);
            
            await Task.WhenAll(agentTask, orchestratorTask);
            
            Logger.LogInformation("InitializeHubs: SignalR hubs initialized successfully in {ElapsedMs}ms. Agent hub state: {AgentHubState}, Orchestrator hub state: {OrchestratorHubState}", 
                stopwatch.ElapsedMilliseconds, _agentHub.State, _orchestratorHub.State);
        }
        catch (OperationCanceledException)
        {
            Logger.LogError("InitializeHubs: SignalR hub initialization timed out after 10 seconds");
            throw new TimeoutException("Failed to connect to chat services - connection timed out");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "InitializeHubs: Failed to initialize SignalR hubs after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            throw new InvalidOperationException($"Failed to initialize chat services: {ex.Message}", ex);
        }
    }
    
    private async Task LoadOrCreateSession()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        Logger.LogInformation("LoadOrCreateSession: Starting session loading/creation. SessionId='{SessionId}', SelectedAgent='{SelectedAgent}'", 
            SessionId, _selectedAgent?.Id);
        
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            
            if (!string.IsNullOrEmpty(SessionId))
            {
                Logger.LogDebug("LoadOrCreateSession: Attempting to load specific session {SessionId}", SessionId);
                // Load specific session
                _currentSession = await SessionManager.GetSessionAsync(SessionId);
                if (_currentSession != null)
                {
                    Logger.LogInformation("LoadOrCreateSession: Loaded specific session '{SessionName}' ({SessionId}) with {MessageCount} messages and {ParticipantCount} participants", 
                        _currentSession.Name, _currentSession.Id, _currentSession.Messages?.Count ?? 0, _currentSession.ParticipantAgentIds?.Count ?? 0);
                    _sessionName = _currentSession.Name;
                    await LoadMessages();
                    await JoinSessionGroups();
                    return;
                }
                else
                {
                    Logger.LogWarning("LoadOrCreateSession: Specific session {SessionId} not found", SessionId);
                }
            }
            
            Logger.LogDebug("LoadOrCreateSession: Checking for current session");
            // Check for current session
            _currentSession = await SessionManager.GetCurrentSessionAsync();
            if (_currentSession != null)
            {
                Logger.LogInformation("LoadOrCreateSession: Found current session '{SessionName}' ({SessionId}) with {MessageCount} messages", 
                    _currentSession.Name, _currentSession.Id, _currentSession.Messages?.Count ?? 0);
                _sessionName = _currentSession.Name;
                await LoadMessages();
                await JoinSessionGroups();
                return;
            }
            else
            {
                Logger.LogDebug("LoadOrCreateSession: No current session found");
            }
            
            // No session exists and we have a selected agent - create implicit session
            if (_selectedAgent != null)
            {
                Logger.LogInformation("LoadOrCreateSession: No existing session found, creating implicit session for agent '{AgentName}'", _selectedAgent.Name);
                await CreateImplicitSession();
            }
            else
            {
                Logger.LogInformation("LoadOrCreateSession: No session created - no agent selected");
            }
            
            Logger.LogInformation("LoadOrCreateSession: Completed in {ElapsedMs}ms. Final session: '{SessionName}' ({SessionId})", 
                stopwatch.ElapsedMilliseconds, _sessionName, _currentSession?.Id);
        }
        catch (OperationCanceledException)
        {
            Logger.LogError("LoadOrCreateSession: Loading or creating session timed out after 8 seconds");
            _errorMessage = "Database service is unavailable - please try refreshing the page";
            _showHealthDiagnostics = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "LoadOrCreateSession: Error loading or creating session after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            _errorMessage = $"Error setting up chat session: {ex.Message}";
            _showHealthDiagnostics = true;
        }
    }
    
    private async Task CreateImplicitSession()
    {
        if (_selectedAgent == null || _orchestratorHub == null) return;
        
        // Check connection state before attempting operation
        if (_orchestratorHub.State != HubConnectionState.Connected)
        {
            Logger.LogWarning("Cannot create implicit session - SignalR orchestrator connection not ready (state: {State})", 
                _orchestratorHub.State);
            _errorMessage = "Chat service is not connected - please try refreshing the page";
            _showHealthDiagnostics = true;
            return;
        }
        
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            
            var sessionName = $"Chat with {_selectedAgent.Name}";
            var createRequest = new CreateSessionRequest
            {
                Name = sessionName,
                Type = SessionType.SingleAgent,
                AgentIds = new List<string> { _selectedAgent.Id },
                WorkingDirectory = Directory.GetCurrentDirectory()
            };
            
            var response = await _orchestratorHub.InvokeAsync<SignalRResponses.SessionCreatedResponse>(
                "CreateSession", createRequest, cts.Token);
                
            if (response.Success && response.Session != null)
            {
                _currentSession = response.Session;
                _sessionName = response.Session.Name;
                
                // Ensure proper SignalR subscriptions according to spec
                await EnsureSignalRSubscriptions();
                
                Logger.LogInformation("Implicit session created: {SessionId}", _currentSession.Id);
            }
            else
            {
                _errorMessage = response.Error ?? "Failed to create session";
                Logger.LogError("Failed to create implicit session: {Error}", _errorMessage);
                _showHealthDiagnostics = true;
            }
        }
        catch (OperationCanceledException)
        {
            Logger.LogError("Creating implicit session timed out after 10 seconds");
            _errorMessage = "Creating chat session timed out - please try again";
            _showHealthDiagnostics = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating implicit session");
            _errorMessage = $"Failed to create chat session: {ex.Message}";
            _showHealthDiagnostics = true;
        }
    }
    
    private async Task JoinSessionGroups()
    {
        if (_currentSession == null || _orchestratorHub == null || _agentHub == null) return;
        
        // Check connection states before attempting operations
        if (_orchestratorHub.State != HubConnectionState.Connected)
        {
            Logger.LogWarning("Cannot join session groups - orchestrator hub not connected (state: {State})", 
                _orchestratorHub.State);
            return;
        }
        
        if (_agentHub.State != HubConnectionState.Connected)
        {
            Logger.LogWarning("Cannot subscribe to agents - agent hub not connected (state: {State})", 
                _agentHub.State);
            return;
        }
        
        try
        {
            // Join session group first
            await JoinSessionGroup();
            
            // Subscribe to agent updates for each agent in session
            foreach (var agentId in _currentSession.ParticipantAgentIds ?? new List<string>())
            {
                try
                {
                    await SubscribeToAgent(agentId);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to subscribe to session agent {AgentId}, continuing with other agents", agentId);
                }
            }
            
            // Also ensure we're subscribed to the currently selected agent (if not already in session)
            await SubscribeToSelectedAgent();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error joining session groups for session {SessionId}", _currentSession.Id);
            _errorMessage = $"Failed to join chat session: {ex.Message}";
            _showHealthDiagnostics = true;
        }
    }
    
    private async Task JoinSessionGroup()
    {
        if (_currentSession == null || _orchestratorHub == null) return;
        
        if (_orchestratorHub.State != HubConnectionState.Connected)
        {
            Logger.LogWarning("Cannot join session group - orchestrator hub not connected (state: {State})", 
                _orchestratorHub.State);
            return;
        }
        
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            
            // Join session group in orchestrator hub
            await _orchestratorHub.InvokeAsync("JoinSession", _currentSession.Id, cts.Token);
            _joinedSessionId = _currentSession.Id;
            Logger.LogInformation("Joined session group {SessionId}", _currentSession.Id);
        }
        catch (OperationCanceledException)
        {
            Logger.LogError("Joining session group timed out after 8 seconds for session {SessionId}", _currentSession.Id);
            _errorMessage = "Failed to connect to chat session - please try refreshing";
            _showHealthDiagnostics = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error joining session group for session {SessionId}", _currentSession.Id);
            _errorMessage = $"Failed to join chat session: {ex.Message}";
            _showHealthDiagnostics = true;
        }
    }
    
    private async Task SubscribeToSelectedAgent()
    {
        if (_selectedAgent != null && !(_currentSession?.ParticipantAgentIds?.Contains(_selectedAgent.Id) ?? false))
        {
            // Check connection state before attempting to subscribe
            if (_agentHub?.State != HubConnectionState.Connected)
            {
                Logger.LogWarning("Cannot subscribe to selected agent {AgentId} - SignalR connection not ready (state: {State})", 
                    _selectedAgent.Id, _agentHub?.State);
                return;
            }
            
            try
            {
                await SubscribeToAgent(_selectedAgent.Id);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to subscribe to selected agent {AgentId}", _selectedAgent.Id);
            }
        }
    }
    
    /// <summary>
    /// Ensures proper SignalR subscriptions according to the specification:
    /// - When _selectedAgent changes or is initialized, call SubscribeToAgent
    /// - If _currentSession?.Id exists and OrchestratorHub connection exists, call JoinSession
    /// </summary>
    private async Task EnsureSignalRSubscriptions()
    {
        Logger.LogDebug("EnsureSignalRSubscriptions: Ensuring proper SignalR subscriptions. SelectedAgent: {AgentId}, SessionId: {SessionId}", 
            _selectedAgent?.Id, _currentSession?.Id);
            
        // Subscribe to selected agent if we have one
        if (_selectedAgent != null)
        {
            try
            {
                await SubscribeToAgent(_selectedAgent.Id);
                Logger.LogDebug("EnsureSignalRSubscriptions: Successfully subscribed to agent {AgentId}", _selectedAgent.Id);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "EnsureSignalRSubscriptions: Failed to subscribe to selected agent {AgentId}", _selectedAgent.Id);
            }
        }
        
        // Join session group if we have a session and orchestrator hub connection
        if (_currentSession?.Id != null && _orchestratorHub != null && _orchestratorHub.State == HubConnectionState.Connected)
        {
            try
            {
                // Only join if we haven't already joined this session
                if (_joinedSessionId != _currentSession.Id)
                {
                    await JoinSessionGroup();
                    Logger.LogDebug("EnsureSignalRSubscriptions: Successfully joined session group {SessionId}", _currentSession.Id);
                }
                else
                {
                    Logger.LogDebug("EnsureSignalRSubscriptions: Already joined session group {SessionId}", _currentSession.Id);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "EnsureSignalRSubscriptions: Failed to join session group {SessionId}", _currentSession.Id);
            }
        }
        else if (_currentSession?.Id != null)
        {
            Logger.LogWarning("EnsureSignalRSubscriptions: Session exists but cannot join - orchestrator hub state: {HubState}", 
                _orchestratorHub?.State ?? HubConnectionState.Disconnected);
        }
    }
    
    private async Task LoadAgents()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        Logger.LogInformation("LoadAgents: Starting to load configured agents");
        
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            _agents = await AgentService.GetConfiguredAgentsAsync();
            
            Logger.LogInformation("LoadAgents: Loaded {AgentCount} agents: [{AgentList}]", 
                _agents.Count, string.Join(", ", _agents.Select(a => $"{a.Name} ({a.Id}, {a.Status})").Take(10)));
            
            // Auto-select agent if none is currently selected
            if (_selectedAgent == null && _agents.Any())
            {
                Logger.LogDebug("LoadAgents: No agent selected, attempting to select default agent");
                
                // Try to get default agent first
                try
                {
                    var defaultAgent = await AgentService.GetDefaultAgentAsync();
                    if (defaultAgent != null && _agents.Any(a => a.Id == defaultAgent.Id))
                    {
                        Logger.LogInformation("LoadAgents: Selected default agent '{AgentName}' ({AgentId})", defaultAgent.Name, defaultAgent.Id);
                        await SelectAgentInternal(defaultAgent);
                    }
                    else
                    {
                        Logger.LogDebug("LoadAgents: Default agent not available, selecting first agent");
                        // Fall back to first available agent
                        var firstAgent = _agents.FirstOrDefault();
                        if (firstAgent != null)
                        {
                            Logger.LogInformation("LoadAgents: Selected first available agent '{AgentName}' ({AgentId})", 
                                firstAgent.Name, firstAgent.Id);
                            await SelectAgentInternal(firstAgent);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "LoadAgents: Failed to get default agent, using first available");
                    var firstAgent = _agents.FirstOrDefault();
                    if (firstAgent != null)
                    {
                        Logger.LogInformation("LoadAgents: Selected fallback agent '{AgentName}' ({AgentId})", 
                            firstAgent.Name, firstAgent.Id);
                        await SelectAgentInternal(firstAgent);
                    }
                }
                
                StateHasChanged();
            }
            else if (_selectedAgent != null)
            {
                Logger.LogDebug("LoadAgents: Agent '{AgentName}' ({AgentId}) already selected", _selectedAgent.Name, _selectedAgent.Id);
                // Ensure we're subscribed to the selected agent after hub initialization
                if (_agentHub != null && _agentHub.State == HubConnectionState.Connected)
                {
                    try
                    {
                        await SubscribeToSelectedAgent();
                        Logger.LogDebug("LoadAgents: Successfully ensured subscription to selected agent {AgentId}", _selectedAgent.Id);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "LoadAgents: Failed to subscribe to already selected agent {AgentId}", _selectedAgent.Id);
                    }
                }
                else
                {
                    Logger.LogWarning("LoadAgents: Cannot subscribe to selected agent - agent hub state: {HubState}", 
                        _agentHub?.State ?? HubConnectionState.Disconnected);
                }
            }
            else
            {
                Logger.LogWarning("LoadAgents: No agents available for selection");
            }
            
            Logger.LogInformation("LoadAgents: Completed successfully in {ElapsedMs}ms. Selected agent: '{SelectedAgent}'", 
                stopwatch.ElapsedMilliseconds, _selectedAgent?.Name ?? "none");
        }
        catch (OperationCanceledException)
        {
            Logger.LogError("LoadAgents: Loading agents timed out after 5 seconds");
            _errorMessage = "Agent service is unavailable - please try refreshing the page";
            _showHealthDiagnostics = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "LoadAgents: Error loading agents after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            _errorMessage = $"Failed to load agents: {ex.Message}";
            _showHealthDiagnostics = true;
        }
    }
    
    
    private Task LoadMessages()
    {
        if (_currentSession == null)
        {
            Logger.LogDebug("LoadMessages: No current session, skipping message loading");
            return Task.CompletedTask;
        }
        
        Logger.LogDebug("LoadMessages: Loading messages for session {SessionId}", _currentSession.Id);
        
        try
        {
            // Convert from session messages to chat messages
            _messages = _currentSession.Messages?.Select(m => new ChatMessage
            {
                Id = m.Id,
                Content = m.Content,
                Role = m.Role,
                AgentId = m.SenderId,
                Timestamp = m.Timestamp,
                Attachments = m.Attachments?.ToList()
            }).ToList() ?? new List<ChatMessage>();
            
            Logger.LogInformation("LoadMessages: Loaded {MessageCount} messages for session {SessionId}. Message summary: [{MessageSummary}]", 
                _messages.Count, _currentSession.Id, 
                string.Join(", ", _messages.Take(3).Select(m => $"{m.Role}:{m.Content?.Substring(0, Math.Min(50, m.Content?.Length ?? 0))}...")));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "LoadMessages: Error loading messages for session {SessionId}", _currentSession?.Id);
            _messages = new List<ChatMessage>();
            _errorMessage = $"Failed to load message history: {ex.Message}";
        }
        
        return Task.CompletedTask;
    }
    
    private async Task SelectAgent(AgentInfo agent)
    {
        Logger.LogInformation("SelectAgent: Attempting to select agent '{AgentName}' ({AgentId}), current agent: '{CurrentAgent}'", 
            agent.Name, agent.Id, _selectedAgent?.Id);
            
        if (_selectedAgent?.Id == agent.Id)
        {
            Logger.LogDebug("SelectAgent: Agent {AgentId} is already selected, no change needed", agent.Id);
            return; // Already selected
        }
        
        await SelectAgentInternal(agent);
        await InvokeAsync(StateHasChanged);
    }
    
    private async Task SelectAgentInternal(AgentInfo agent)
    {
        var previousAgentId = _selectedAgent?.Id;
        var previousAgentName = _selectedAgent?.Name;
        _selectedAgent = agent;
        
        Logger.LogInformation("SelectAgentInternal: Changing selection from '{PreviousAgent}' to '{NewAgent}' ({AgentId})", 
            previousAgentName, agent.Name, agent.Id);
        
        if (_agentHub != null && _agentHub.State == HubConnectionState.Connected)
        {
            try
            {
                // Unsubscribe from previous agent
                if (!string.IsNullOrEmpty(previousAgentId))
                {
                    Logger.LogDebug("SelectAgentInternal: Unsubscribing from previous agent {PreviousAgentId}", previousAgentId);
                    await UnsubscribeFromAgent(previousAgentId);
                }
                
                // Use the new helper method to ensure proper subscriptions according to spec
                await EnsureSignalRSubscriptions();
                
                Logger.LogInformation("SelectAgentInternal: Successfully updated subscriptions for agent change");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SelectAgentInternal: Error updating agent subscription from {PreviousAgent} to {NewAgent}", 
                    previousAgentId, agent.Id);
                _errorMessage = $"Failed to connect to agent: {ex.Message}";
            }
        }
        else
        {
            Logger.LogWarning("SelectAgentInternal: Agent hub is null or not connected, cannot update subscriptions. State: {HubState}", 
                _agentHub?.State ?? HubConnectionState.Disconnected);
        }
    }
    
    private async Task SendMessage(string content)
    {
        Logger.LogInformation("SendMessage: User attempting to send message of length {ContentLength} to agent '{AgentName}' ({AgentId})", 
            content?.Length ?? 0, _selectedAgent?.Name, _selectedAgent?.Id);
            
        if (string.IsNullOrWhiteSpace(content) || _selectedAgent == null || _agentHub == null)
        {
            Logger.LogWarning("SendMessage: Invalid request - content empty: {ContentEmpty}, agent null: {AgentNull}, hub null: {HubNull}", 
                string.IsNullOrWhiteSpace(content), _selectedAgent == null, _agentHub == null);
            return;
        }
        
        // Check connection state before sending
        if (_agentHub.State != HubConnectionState.Connected)
        {
            Logger.LogWarning("SendMessage: Cannot send message - agent hub not connected (state: {HubState})", _agentHub.State);
            _errorMessage = "Chat service is not connected - please try reconnecting";
            _showHealthDiagnostics = true;
            await InvokeAsync(StateHasChanged);
            return;
        }
            
        _isProcessing = true;
        _isTyping = true;
        _errorMessage = null;
        
        Logger.LogDebug("SendMessage: Processing state set, checking session");
        
        // Ensure we have a session before sending
        if (_currentSession == null)
        {
            Logger.LogInformation("SendMessage: No current session, creating implicit session");
            await CreateImplicitSession();
            if (_currentSession == null)
            {
                Logger.LogError("SendMessage: Failed to create implicit session");
                _isProcessing = false;
                _isTyping = false;
                _errorMessage = "Unable to create chat session. Please try again.";
                _showHealthDiagnostics = true;
                await InvokeAsync(StateHasChanged);
                return;
            }
        }
        
        // Add user message
        var userMessage = new ChatMessage
        {
            Id = Guid.NewGuid().ToString(),
            Content = content,
            Role = MessageRole.User,
            Timestamp = DateTime.UtcNow
        };
        _messages.Add(userMessage);
        
        Logger.LogDebug("SendMessage: Added user message {MessageId} to UI, total messages: {MessageCount}", 
            userMessage.Id, _messages.Count);
        
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            
            // Send to agent via SignalR
            var request = new SignalRRequests.AgentMessageRequest
            {
                AgentId = _selectedAgent.Id,
                SessionId = _currentSession.Id,
                Content = content
            };
            
            Logger.LogDebug("SendMessage: Sending message to agent via SignalR - AgentId: {AgentId}, SessionId: {SessionId}", 
                request.AgentId, request.SessionId);
            
            await _agentHub.InvokeAsync("SendAgentMessage", request, cts.Token);
            
            Logger.LogInformation("SendMessage: Message sent successfully to agent {AgentId} in session {SessionId}", 
                _selectedAgent.Id, _currentSession.Id);
        }
        catch (OperationCanceledException)
        {
            Logger.LogError("SendMessage: Sending message to agent {AgentId} timed out after 30 seconds", _selectedAgent.Id);
            _errorMessage = "Message send timed out - the agent may be unresponsive";
            _showHealthDiagnostics = true;
            _isTyping = false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "SendMessage: Error sending message to agent {AgentId} in session {SessionId}", 
                _selectedAgent.Id, _currentSession?.Id);
            _errorMessage = $"Failed to send message: {ex.Message}";
            _showHealthDiagnostics = true;
            _isTyping = false;
        }
        
        _isProcessing = false;
        await ScrollToBottom();
    }
    
    private async Task ScrollToBottom()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("scrollToBottom", _messagesContainer);
        }
        catch
        {
            // Ignore JS errors
        }
    }
    
    private void ShowHistory()
    {
        _showHistory = true;
    }
    
    private Task AttachFile()
    {
        // TODO: Implement file attachment
        return Task.CompletedTask;
    }
    
    private void ShowAgentSettings()
    {
        // TODO: Implement agent settings
    }
    
    private void ShowCreateAgentDialog()
    {
        Console.WriteLine("ChatInterface.ShowCreateAgentDialog: Method called, setting _showCreateAgentDialog = true");
        _showCreateAgentDialog = true;
        StateHasChanged();
        Console.WriteLine($"ChatInterface.ShowCreateAgentDialog: Dialog visibility set to: {_showCreateAgentDialog}");
    }
    
    private async Task CreateNewAgent(AgentConfiguration configuration)
    {
        Console.WriteLine($"ChatInterface.CreateNewAgent: Method called with configuration for agent '{configuration.Name}'");
        try
        {
            Console.WriteLine($"ChatInterface.CreateNewAgent: Calling AgentService.CreateAgentAsync");
            var newAgent = await AgentService.CreateAgentAsync(configuration.Type, configuration);
            Console.WriteLine($"ChatInterface.CreateNewAgent: Agent created successfully with ID '{newAgent.Id}'");
            
            Console.WriteLine($"ChatInterface.CreateNewAgent: Refreshing agents list");
            await LoadAgents(); // Refresh the agents list
            
            // Select the newly created agent
            var createdAgent = _agents.FirstOrDefault(a => a.Id == newAgent.Id);
            if (createdAgent != null)
            {
                Console.WriteLine($"ChatInterface.CreateNewAgent: Selecting newly created agent '{createdAgent.Name}'");
                await SelectAgentInternal(createdAgent);
            }
            
            // Set as default if no default agent exists
            try
            {
                var existingDefault = await AgentService.GetDefaultAgentAsync();
                if (existingDefault == null)
                {
                    await AgentService.SetDefaultAgentAsync(newAgent.Id);
                    Console.WriteLine($"ChatInterface.CreateNewAgent: Set agent '{newAgent.Name}' as default");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to set agent as default: {ex.Message}");
            }
            
            StateHasChanged();
            Console.WriteLine($"ChatInterface.CreateNewAgent: Method completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ChatInterface.CreateNewAgent: Error creating agent: {ex.Message}");
            Console.WriteLine($"ChatInterface.CreateNewAgent: Exception details: {ex}");
            // TODO: Show error message to user
        }
    }
    
    private Task LoadSession(Session session)
    {
        Navigation.NavigateTo($"/session/{session.Id}");
        return Task.CompletedTask;
    }
    
    private Color GetAgentStatusColor(AgentStatus status)
    {
        return status switch
        {
            AgentStatus.Ready => Color.Success,
            AgentStatus.Busy => Color.Warning,
            AgentStatus.Processing => Color.Warning,
            AgentStatus.Error => Color.Error,
            AgentStatus.Initializing => Color.Info,
            AgentStatus.Shutdown => Color.Default,
            AgentStatus.Uninitialized => Color.Secondary,
            AgentStatus.Unknown => Color.Default,
            _ => Color.Default
        };
    }
    
    // Health panel methods
    private async Task ReconnectHubs()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        Logger.LogInformation("ReconnectHubs: Starting hub reconnection. Agent hub state: {AgentHubState}, Orchestrator hub state: {OrchestratorHubState}", 
            _agentHub?.State, _orchestratorHub?.State);
        
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            
            var reconnectTasks = new List<Task>();
            
            if (_agentHub?.State != HubConnectionState.Connected && _agentHub != null)
            {
                Logger.LogInformation("ReconnectHubs: Reconnecting agent hub (current state: {State})", _agentHub.State);
                reconnectTasks.Add(_agentHub.StartAsync(cts.Token));
            }
            
            if (_orchestratorHub?.State != HubConnectionState.Connected && _orchestratorHub != null)
            {
                Logger.LogInformation("ReconnectHubs: Reconnecting orchestrator hub (current state: {State})", _orchestratorHub.State);
                reconnectTasks.Add(_orchestratorHub.StartAsync(cts.Token));
            }
            
            if (reconnectTasks.Any())
            {
                Logger.LogDebug("ReconnectHubs: Executing {TaskCount} reconnection tasks", reconnectTasks.Count);
                await Task.WhenAll(reconnectTasks);
                Logger.LogInformation("ReconnectHubs: Hub reconnection completed successfully in {ElapsedMs}ms. Final states - Agent: {AgentState}, Orchestrator: {OrchestratorState}", 
                    stopwatch.ElapsedMilliseconds, _agentHub?.State, _orchestratorHub?.State);
            }
            else
            {
                Logger.LogInformation("ReconnectHubs: No reconnection needed - all hubs already connected");
            }
            
            _errorMessage = null;
            _showHealthDiagnostics = false;
            await InvokeAsync(StateHasChanged);
        }
        catch (OperationCanceledException)
        {
            Logger.LogError("ReconnectHubs: Hub reconnection timed out after 10 seconds");
            _errorMessage = "Reconnection timed out - please try again";
            _showHealthDiagnostics = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "ReconnectHubs: Error reconnecting hubs after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            _errorMessage = $"Failed to reconnect: {ex.Message}";
            _showHealthDiagnostics = true;
        }
    }
    
    private async Task RetryAgentInitialization(string agentId)
    {
        Logger.LogInformation("RetryAgentInitialization: Retrying initialization for agent {AgentId}", agentId);
        
        try
        {
            Logger.LogDebug("RetryAgentInitialization: Reloading agents to refresh status");
            // Reload agents to refresh status
            await LoadAgents();
            
            var refreshedAgent = _agents.FirstOrDefault(a => a.Id == agentId);
            if (refreshedAgent != null)
            {
                Logger.LogInformation("RetryAgentInitialization: Agent {AgentId} found after reload with status {Status}", 
                    agentId, refreshedAgent.Status);
                _selectedAgent = refreshedAgent;
            }
            else
            {
                Logger.LogWarning("RetryAgentInitialization: Agent {AgentId} not found after reload", agentId);
            }
            
            await InvokeAsync(StateHasChanged);
            Logger.LogInformation("RetryAgentInitialization: Completed retry for agent {AgentId}", agentId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "RetryAgentInitialization: Error retrying agent initialization for {AgentId}", agentId);
            _errorMessage = $"Failed to retry agent initialization: {ex.Message}";
        }
    }
    
    private async Task OnHealthUpdated()
    {
        // Auto-show diagnostics panel when there are blocking errors
        // Hide when everything is OK or user manually dismissed
        if (!string.IsNullOrEmpty(_errorMessage))
        {
            _showHealthDiagnostics = true;
        }
        
        await InvokeAsync(StateHasChanged);
    }
    
    private async Task SubscribeToAgent(string agentId)
    {
        Logger.LogDebug("SubscribeToAgent: Attempting to subscribe to agent {AgentId}. Hub null: {HubNull}, Already subscribed: {AlreadySubscribed}, Hub state: {HubState}", 
            agentId, _agentHub == null, _subscribedAgentIds.Contains(agentId), _agentHub?.State);
            
        if (_agentHub == null || string.IsNullOrEmpty(agentId) || _subscribedAgentIds.Contains(agentId))
        {
            Logger.LogDebug("SubscribeToAgent: Skipping subscription - hub null: {HubNull}, agent ID empty: {IdEmpty}, already subscribed: {AlreadySubscribed}", 
                _agentHub == null, string.IsNullOrEmpty(agentId), _subscribedAgentIds.Contains(agentId));
            return;
        }
        
        // Check connection state before attempting operation
        if (_agentHub.State != HubConnectionState.Connected)
        {
            Logger.LogWarning("SubscribeToAgent: Cannot subscribe to agent {AgentId} - SignalR connection not ready (state: {State})", 
                agentId, _agentHub.State);
            return;
        }
            
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            
            Logger.LogDebug("SubscribeToAgent: Invoking SubscribeToAgent on SignalR hub for agent {AgentId}", agentId);
            await _agentHub.InvokeAsync("SubscribeToAgent", agentId, cts.Token);
            
            _subscribedAgentIds.Add(agentId);
            Logger.LogInformation("SubscribeToAgent: Successfully subscribed to agent {AgentId}. Total subscriptions: {SubscriptionCount}", 
                agentId, _subscribedAgentIds.Count);
        }
        catch (OperationCanceledException)
        {
            Logger.LogError("SubscribeToAgent: Subscribing to agent {AgentId} timed out after 5 seconds", agentId);
            throw new TimeoutException($"Failed to subscribe to agent {agentId} - operation timed out");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "SubscribeToAgent: Failed to subscribe to agent {AgentId}", agentId);
            throw;
        }
    }
    
    private async Task UnsubscribeFromAgent(string agentId)
    {
        Logger.LogDebug("UnsubscribeFromAgent: Attempting to unsubscribe from agent {AgentId}. Hub null: {HubNull}, Subscribed: {IsSubscribed}, Hub state: {HubState}", 
            agentId, _agentHub == null, _subscribedAgentIds.Contains(agentId), _agentHub?.State);
            
        if (_agentHub == null || string.IsNullOrEmpty(agentId) || !_subscribedAgentIds.Contains(agentId))
        {
            Logger.LogDebug("UnsubscribeFromAgent: Skipping unsubscription - hub null: {HubNull}, agent ID empty: {IdEmpty}, not subscribed: {NotSubscribed}", 
                _agentHub == null, string.IsNullOrEmpty(agentId), !_subscribedAgentIds.Contains(agentId));
            return;
        }
            
        try
        {
            Logger.LogDebug("UnsubscribeFromAgent: Invoking UnsubscribeFromAgent on SignalR hub for agent {AgentId}", agentId);
            await _agentHub.InvokeAsync("UnsubscribeFromAgent", agentId);
            
            _subscribedAgentIds.Remove(agentId);
            Logger.LogInformation("UnsubscribeFromAgent: Successfully unsubscribed from agent {AgentId}. Remaining subscriptions: {SubscriptionCount}", 
                agentId, _subscribedAgentIds.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UnsubscribeFromAgent: Failed to unsubscribe from agent {AgentId}", agentId);
            // Don't throw on unsubscribe errors as they're non-critical
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        Logger.LogInformation("DisposeAsync: Starting ChatInterface disposal. Subscribed agents: {SubscriptionCount}, Joined session: '{SessionId}'", 
            _subscribedAgentIds.Count, _joinedSessionId);
        
        try
        {
            // Clean up subscriptions before disposing hubs
            if (_agentHub is not null && _agentHub.State == HubConnectionState.Connected)
            {
                Logger.LogDebug("DisposeAsync: Cleaning up {SubscriptionCount} agent subscriptions", _subscribedAgentIds.Count);
                // Unsubscribe from all subscribed agents
                foreach (var agentId in _subscribedAgentIds.ToList())
                {
                    await UnsubscribeFromAgent(agentId);
                }
            }
            else
            {
                Logger.LogDebug("DisposeAsync: Skipping agent subscription cleanup - hub null: {HubNull}, state: {HubState}", 
                    _agentHub == null, _agentHub?.State);
            }
            
            if (_orchestratorHub is not null && _orchestratorHub.State == HubConnectionState.Connected)
            {
                // Leave session group if joined
                if (!string.IsNullOrEmpty(_joinedSessionId))
                {
                    try
                    {
                        Logger.LogDebug("DisposeAsync: Leaving session group {SessionId}", _joinedSessionId);
                        await _orchestratorHub.InvokeAsync("LeaveSession", _joinedSessionId);
                        Logger.LogInformation("DisposeAsync: Left session group {SessionId} during disposal", _joinedSessionId);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "DisposeAsync: Failed to leave session group {SessionId} during disposal", _joinedSessionId);
                    }
                }
            }
            else
            {
                Logger.LogDebug("DisposeAsync: Skipping session cleanup - orchestrator hub null: {HubNull}, state: {HubState}", 
                    _orchestratorHub == null, _orchestratorHub?.State);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "DisposeAsync: Error during subscription cleanup in dispose");
        }
        finally
        {
            // Always dispose the hubs
            if (_agentHub is not null)
            {
                Logger.LogDebug("DisposeAsync: Disposing agent hub");
                await _agentHub.DisposeAsync();
            }
            
            if (_orchestratorHub is not null)
            {
                Logger.LogDebug("DisposeAsync: Disposing orchestrator hub");
                await _orchestratorHub.DisposeAsync();
            }
            
            Logger.LogInformation("DisposeAsync: ChatInterface disposal completed");
        }
    }
}