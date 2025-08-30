# Web UI Components Specification

## Overview
This document specifies the Blazor Server web UI components for OrchestratorChat, leveraging MudBlazor and components from claudecodewrappersharp.

## Project: OrchestratorChat.Web

### Layout Structure

#### Main Layout
```razor
@* Shared/MainLayout.razor *@
@inherits LayoutComponentBase
@inject NavigationManager Navigation
@inject ISessionManager SessionManager

<MudThemeProvider Theme="@_theme" />
<MudDialogProvider />
<MudSnackbarProvider />

<MudLayout>
    <MudAppBar Elevation="1">
        <MudIconButton Icon="@Icons.Material.Filled.Menu" 
                       Color="Color.Inherit" 
                       Edge="Edge.Start" 
                       OnClick="@DrawerToggle" />
        <MudText Typo="Typo.h5" Class="ml-3">OrchestratorChat</MudText>
        <MudSpacer />
        <SessionIndicator />
        <MudIconButton Icon="@Icons.Material.Filled.Settings" 
                       Color="Color.Inherit" 
                       OnClick="@OpenSettings" />
    </MudAppBar>
    
    <MudDrawer @bind-Open="_drawerOpen" ClipMode="DrawerClipMode.Always" Elevation="2">
        <NavMenu />
    </MudDrawer>
    
    <MudMainContent>
        <MudContainer MaxWidth="MaxWidth.False" Class="pa-4">
            @Body
        </MudContainer>
    </MudMainContent>
</MudLayout>

@code {
    private bool _drawerOpen = true;
    private MudTheme _theme = new MudTheme
    {
        Palette = new PaletteLight
        {
            Primary = Colors.Blue.Darken2,
            Secondary = Colors.Green.Accent4,
            AppbarBackground = Colors.Blue.Darken3,
        }
    };
    
    void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }
    
    void OpenSettings()
    {
        Navigation.NavigateTo("/settings");
    }
}
```

### Core Pages

#### Agent Dashboard
```razor
@* Pages/Dashboard.razor *@
@page "/"
@page "/dashboard"
@inject IAgentFactory AgentFactory
@inject ISessionManager SessionManager
@inject NavigationManager Navigation

<PageTitle>Agent Dashboard</PageTitle>

<MudText Typo="Typo.h4" Class="mb-4">Agent Dashboard</MudText>

<MudGrid>
    <MudItem xs="12">
        <MudPaper Class="pa-4">
            <MudButton Variant="Variant.Filled" 
                       Color="Color.Primary" 
                       StartIcon="@Icons.Material.Filled.Add"
                       OnClick="@ShowNewSessionDialog">
                New Session
            </MudButton>
            <MudButton Variant="Variant.Outlined" 
                       Color="Color.Secondary" 
                       StartIcon="@Icons.Material.Filled.GroupAdd"
                       OnClick="@ShowAddAgentDialog"
                       Class="ml-2">
                Add Agent
            </MudButton>
        </MudPaper>
    </MudItem>
    
    @foreach (var agent in _agents)
    {
        <MudItem xs="12" sm="6" md="4" lg="3">
            <AgentCard Agent="@agent" OnClick="@(() => OpenAgentChat(agent))" />
        </MudItem>
    }
</MudGrid>

<MudDialog @ref="_newSessionDialog">
    <DialogContent>
        <NewSessionForm OnSessionCreated="@OnSessionCreated" />
    </DialogContent>
</MudDialog>

@code {
    private List<AgentInfo> _agents = new();
    private MudDialog _newSessionDialog;
    
    protected override async Task OnInitializedAsync()
    {
        await LoadAgents();
    }
    
    private async Task LoadAgents()
    {
        // Load configured agents
        _agents = await AgentFactory.GetConfiguredAgents();
    }
    
    private void OpenAgentChat(AgentInfo agent)
    {
        Navigation.NavigateTo($"/chat/{agent.Id}");
    }
    
    private async Task ShowNewSessionDialog()
    {
        await _newSessionDialog.Show();
    }
    
    private async Task OnSessionCreated(Session session)
    {
        await _newSessionDialog.Close();
        Navigation.NavigateTo($"/session/{session.Id}");
    }
}
```

#### Chat Interface
```razor
@* Pages/ChatInterface.razor *@
@page "/chat/{AgentId}"
@page "/session/{SessionId}"
@using Microsoft.AspNetCore.SignalR.Client
@implements IAsyncDisposable
@inject IJSRuntime JSRuntime

<PageTitle>Chat - @_sessionName</PageTitle>

<MudGrid Class="chat-container">
    <MudItem xs="12" md="3">
        <AgentSidebar Agents="@_agents" 
                     SelectedAgent="@_selectedAgent"
                     OnAgentSelected="@SelectAgent" />
    </MudItem>
    
    <MudItem xs="12" md="9">
        <MudPaper Class="chat-panel">
            <MudToolBar>
                <MudText Typo="Typo.h6">@_selectedAgent?.Name</MudText>
                <MudSpacer />
                <MudIconButton Icon="@Icons.Material.Filled.History" 
                              OnClick="@ShowHistory" />
                <MudIconButton Icon="@Icons.Material.Filled.AttachFile" 
                              OnClick="@AttachFile" />
                <MudIconButton Icon="@Icons.Material.Filled.Settings" 
                              OnClick="@ShowAgentSettings" />
            </MudToolBar>
            
            <div class="messages-container" @ref="_messagesContainer">
                <CascadingValue Value="@_currentSession">
                    @foreach (var message in _messages)
                    {
                        <MessageBubble Message="@message" />
                    }
                    
                    @if (_isTyping)
                    {
                        <TypingIndicator AgentName="@_selectedAgent?.Name" />
                    }
                </CascadingValue>
            </div>
            
            <MessageInput OnSendMessage="@SendMessage" 
                         IsEnabled="@(!_isProcessing)"
                         OnAttach="@AttachFile" />
        </MudPaper>
    </MudItem>
</MudGrid>

@code {
    [Parameter] public string? AgentId { get; set; }
    [Parameter] public string? SessionId { get; set; }
    
    private HubConnection? _agentHub;
    private List<AgentInfo> _agents = new();
    private AgentInfo? _selectedAgent;
    private Session? _currentSession;
    private string _sessionName = "Chat";
    private List<ChatMessage> _messages = new();
    private bool _isProcessing = false;
    private bool _isTyping = false;
    private ElementReference _messagesContainer;
    
    protected override async Task OnInitializedAsync()
    {
        await InitializeHub();
        await LoadSession();
        await LoadAgents();
    }
    
    private async Task InitializeHub()
    {
        _agentHub = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri("/hubs/agent"))
            .Build();
            
        _agentHub.On<AgentResponseDto>("ReceiveAgentResponse", async (response) =>
        {
            _isTyping = false;
            
            if (response.Response.Type == ResponseType.Text)
            {
                var existingMessage = _messages.FirstOrDefault(m => m.Id == response.Response.MessageId);
                if (existingMessage != null)
                {
                    existingMessage.Content += response.Response.Content;
                }
                else
                {
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
        
        await _agentHub.StartAsync();
    }
    
    private async Task SendMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content) || _selectedAgent == null)
            return;
            
        _isProcessing = true;
        _isTyping = true;
        
        // Add user message
        _messages.Add(new ChatMessage
        {
            Content = content,
            Role = MessageRole.User,
            Timestamp = DateTime.UtcNow
        });
        
        // Send to agent
        await _agentHub.InvokeAsync("SendAgentMessage", new AgentMessageRequest
        {
            AgentId = _selectedAgent.Id,
            SessionId = _currentSession?.Id ?? Guid.NewGuid().ToString(),
            Content = content
        });
        
        _isProcessing = false;
        await ScrollToBottom();
    }
    
    private async Task ScrollToBottom()
    {
        await JSRuntime.InvokeVoidAsync("scrollToBottom", _messagesContainer);
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_agentHub is not null)
        {
            await _agentHub.DisposeAsync();
        }
    }
}
```

### Reusable Components (from claudecodewrappersharp)

#### Message Components
```razor
@* Components/MessageBubble.razor *@
<div class="message-bubble @GetMessageClass()">
    <div class="message-header">
        <MudText Typo="Typo.caption">
            @GetSenderName() • @Message.Timestamp.ToString("HH:mm")
        </MudText>
    </div>
    <div class="message-content">
        @if (Message.Role == MessageRole.Assistant)
        {
            <MarkdownRenderer Content="@Message.Content" />
        }
        else
        {
            <MudText>@Message.Content</MudText>
        }
    </div>
    @if (Message.Attachments?.Any() == true)
    {
        <div class="message-attachments">
            @foreach (var attachment in Message.Attachments)
            {
                <AttachmentChip Attachment="@attachment" />
            }
        </div>
    }
</div>

@code {
    [Parameter] public ChatMessage Message { get; set; }
    [CascadingParameter] public Session CurrentSession { get; set; }
    
    private string GetMessageClass()
    {
        return Message.Role == MessageRole.User ? "user-message" : "agent-message";
    }
    
    private string GetSenderName()
    {
        if (Message.Role == MessageRole.User)
            return "You";
            
        var agent = CurrentSession?.ParticipantAgents?.FirstOrDefault(a => a.Id == Message.AgentId);
        return agent?.Name ?? "Agent";
    }
}
```

#### Agent Card Component
```razor
@* Components/AgentCard.razor *@
<MudCard Class="agent-card" @onclick="@OnClick">
    <MudCardContent>
        <div class="d-flex align-center mb-2">
            <MudAvatar Color="@GetStatusColor()" Size="Size.Small" Class="mr-2">
                @Agent.Name.Substring(0, 1).ToUpper()
            </MudAvatar>
            <div>
                <MudText Typo="Typo.h6">@Agent.Name</MudText>
                <MudText Typo="Typo.caption">@Agent.Type</MudText>
            </div>
        </div>
        
        <MudText Typo="Typo.body2" Class="mb-2">
            @Agent.Description
        </MudText>
        
        <MudChip Size="Size.Small" Color="@GetStatusColor()">
            @Agent.Status
        </MudChip>
        
        @if (Agent.Capabilities?.AvailableTools?.Any() == true)
        {
            <div class="mt-2">
                <MudText Typo="Typo.caption">Tools: @Agent.Capabilities.AvailableTools.Count</MudText>
            </div>
        }
    </MudCardContent>
</MudCard>

@code {
    [Parameter] public AgentInfo Agent { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }
    
    private Color GetStatusColor()
    {
        return Agent.Status switch
        {
            AgentStatus.Ready => Color.Success,
            AgentStatus.Busy => Color.Warning,
            AgentStatus.Error => Color.Error,
            _ => Color.Default
        };
    }
}
```

#### MCP Configuration Component (from claudecodewrappersharp)
```razor
@* Components/McpConfiguration.razor *@
<MudCard>
    <MudCardContent>
        <MudText Typo="Typo.h6" Class="mb-3">MCP Configuration</MudText>
        
        <MudTabs Elevation="0" Rounded="true" ApplyEffectsToContainer="true">
            <MudTabPanel Text="Global Tools">
                <MudList>
                    @foreach (var tool in _globalTools)
                    {
                        <MudListItem>
                            <div class="d-flex align-center">
                                <MudCheckBox @bind-Checked="@tool.Enabled" />
                                <MudText Class="ml-2">@tool.Name</MudText>
                                <MudSpacer />
                                <MudIconButton Icon="@Icons.Material.Filled.Settings" 
                                             Size="Size.Small"
                                             OnClick="@(() => ConfigureTool(tool))" />
                            </div>
                        </MudListItem>
                    }
                </MudList>
            </MudTabPanel>
            
            <MudTabPanel Text="Project Tools">
                <MudTextField @bind-Value="_projectPath" 
                            Label="Project Path" 
                            Variant="Variant.Outlined"
                            Adornment="Adornment.End"
                            AdornmentIcon="@Icons.Material.Filled.FolderOpen"
                            OnAdornmentClick="@SelectProjectPath" />
                            
                <MudButton Variant="Variant.Filled" 
                         Color="Color.Primary"
                         OnClick="@LoadProjectConfig"
                         Class="mt-2">
                    Load Project Config
                </MudButton>
                
                @if (_projectTools?.Any() == true)
                {
                    <MudList Class="mt-3">
                        @foreach (var tool in _projectTools)
                        {
                            <MudListItem>
                                <MudText>@tool.Name - @tool.Command</MudText>
                            </MudListItem>
                        }
                    </MudList>
                }
            </MudTabPanel>
            
            <MudTabPanel Text="Import">
                <MudText Typo="Typo.body2" Class="mb-3">
                    Import MCP configuration from Claude Desktop
                </MudText>
                <MudButton Variant="Variant.Outlined" 
                         Color="Color.Secondary"
                         OnClick="@ImportFromClaudeDesktop">
                    Import from Claude Desktop
                </MudButton>
            </MudTabPanel>
        </MudTabs>
    </MudCardContent>
</MudCard>

@code {
    private List<McpTool> _globalTools = new();
    private List<McpTool> _projectTools = new();
    private string _projectPath = "";
    
    protected override async Task OnInitializedAsync()
    {
        await LoadGlobalTools();
    }
    
    private async Task LoadGlobalTools()
    {
        _globalTools = await McpService.GetGlobalTools();
    }
    
    private async Task LoadProjectConfig()
    {
        if (!string.IsNullOrEmpty(_projectPath))
        {
            _projectTools = await McpService.GetProjectTools(_projectPath);
        }
    }
    
    private async Task ImportFromClaudeDesktop()
    {
        var imported = await McpService.ImportFromClaudeDesktop();
        if (imported)
        {
            await LoadGlobalTools();
            Snackbar.Add("Configuration imported successfully", Severity.Success);
        }
    }
}
```

#### Session History Component
```razor
@* Components/SessionHistory.razor *@
<MudDrawer @bind-Open="@IsOpen" Anchor="Anchor.Right" Width="400px" Elevation="8">
    <MudDrawerHeader>
        <MudText Typo="Typo.h6">Session History</MudText>
        <MudSpacer />
        <MudIconButton Icon="@Icons.Material.Filled.Close" OnClick="@Close" />
    </MudDrawerHeader>
    
    <MudList Clickable="true">
        @foreach (var session in _sessions)
        {
            <MudListItem OnClick="@(() => LoadSession(session))">
                <div>
                    <MudText Typo="Typo.subtitle1">@session.Name</MudText>
                    <MudText Typo="Typo.caption">
                        @session.CreatedAt.ToString("MMM dd, HH:mm") • 
                        @session.Messages.Count messages
                    </MudText>
                    @if (!string.IsNullOrEmpty(session.LastMessage))
                    {
                        <MudText Typo="Typo.body2" Class="text-truncate">
                            @session.LastMessage
                        </MudText>
                    }
                </div>
            </MudListItem>
            <MudDivider />
        }
    </MudList>
</MudDrawer>

@code {
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
    [Parameter] public EventCallback<Session> OnSessionSelected { get; set; }
    
    private List<SessionSummary> _sessions = new();
    
    protected override async Task OnInitializedAsync()
    {
        await LoadSessions();
    }
    
    private async Task LoadSessions()
    {
        _sessions = await SessionManager.GetRecentSessions(20);
    }
    
    private async Task LoadSession(SessionSummary session)
    {
        await OnSessionSelected.InvokeAsync(session);
        await Close();
    }
    
    private async Task Close()
    {
        IsOpen = false;
        await IsOpenChanged.InvokeAsync(false);
    }
}
```

### Orchestration Components

#### Orchestration View
```razor
@* Pages/Orchestrator.razor *@
@page "/orchestrator"
@inject IOrchestrator Orchestrator

<PageTitle>Multi-Agent Orchestrator</PageTitle>

<MudGrid>
    <MudItem xs="12" md="8">
        <MudCard>
            <MudCardContent>
                <MudText Typo="Typo.h5" Class="mb-3">Orchestration Planning</MudText>
                
                <MudTextField @bind-Value="_goal" 
                            Label="Goal" 
                            Variant="Variant.Outlined"
                            Lines="3"
                            HelperText="Describe what you want to accomplish" />
                
                <MudSelect @bind-Value="_strategy" 
                         Label="Strategy" 
                         Variant="Variant.Outlined"
                         Class="mt-3">
                    <MudSelectItem Value="OrchestrationStrategy.Sequential">Sequential</MudSelectItem>
                    <MudSelectItem Value="OrchestrationStrategy.Parallel">Parallel</MudSelectItem>
                    <MudSelectItem Value="OrchestrationStrategy.Adaptive">Adaptive</MudSelectItem>
                </MudSelect>
                
                <MudText Typo="Typo.subtitle1" Class="mt-3 mb-2">Select Agents</MudText>
                <MudChipSet @bind-SelectedChips="_selectedAgents" MultiSelection="true">
                    @foreach (var agent in _availableAgents)
                    {
                        <MudChip Value="@agent.Id" Color="Color.Primary" Variant="Variant.Outlined">
                            @agent.Name
                        </MudChip>
                    }
                </MudChipSet>
                
                <MudButton Variant="Variant.Filled" 
                         Color="Color.Primary"
                         OnClick="@CreatePlan"
                         Disabled="@_isCreatingPlan"
                         Class="mt-3">
                    @if (_isCreatingPlan)
                    {
                        <MudProgressCircular Size="Size.Small" Indeterminate="true" />
                        <span class="ml-2">Creating Plan...</span>
                    }
                    else
                    {
                        <span>Create Plan</span>
                    }
                </MudButton>
            </MudCardContent>
        </MudCard>
        
        @if (_currentPlan != null)
        {
            <OrchestrationPlan Plan="@_currentPlan" 
                              OnExecute="@ExecutePlan"
                              Progress="@_progress" />
        }
    </MudItem>
    
    <MudItem xs="12" md="4">
        <OrchestrationTimeline Steps="@_executedSteps" />
    </MudItem>
</MudGrid>

@code {
    private string _goal = "";
    private OrchestrationStrategy _strategy = OrchestrationStrategy.Adaptive;
    private List<AgentInfo> _availableAgents = new();
    private MudChip[] _selectedAgents = Array.Empty<MudChip>();
    private OrchestrationPlan? _currentPlan;
    private OrchestrationProgress? _progress;
    private List<ExecutedStep> _executedSteps = new();
    private bool _isCreatingPlan = false;
    
    protected override async Task OnInitializedAsync()
    {
        _availableAgents = await AgentFactory.GetConfiguredAgents();
    }
    
    private async Task CreatePlan()
    {
        _isCreatingPlan = true;
        
        var request = new OrchestrationRequest
        {
            Goal = _goal,
            Strategy = _strategy,
            AvailableAgentIds = _selectedAgents.Select(c => c.Value.ToString()).ToList()
        };
        
        _currentPlan = await Orchestrator.CreatePlanAsync(request);
        _isCreatingPlan = false;
    }
    
    private async Task ExecutePlan()
    {
        var progress = new Progress<OrchestrationProgress>(p =>
        {
            _progress = p;
            InvokeAsync(StateHasChanged);
        });
        
        var result = await Orchestrator.ExecutePlanAsync(_currentPlan, progress);
        
        // Update timeline with results
        _executedSteps = result.StepResults.Select(r => new ExecutedStep
        {
            Name = r.StepName,
            Status = r.Success ? "Success" : "Failed",
            Duration = r.Duration,
            Output = r.Output
        }).ToList();
    }
}
```

### Styling

```css
/* wwwroot/css/app.css */
.chat-container {
    height: calc(100vh - 120px);
}

.messages-container {
    height: calc(100% - 120px);
    overflow-y: auto;
    padding: 1rem;
}

.message-bubble {
    margin-bottom: 1rem;
    animation: slideIn 0.3s ease-out;
}

.user-message {
    margin-left: auto;
    max-width: 70%;
}

.user-message .message-content {
    background-color: var(--mud-palette-primary);
    color: white;
    padding: 0.75rem;
    border-radius: 12px 12px 4px 12px;
}

.agent-message {
    margin-right: auto;
    max-width: 70%;
}

.agent-message .message-content {
    background-color: var(--mud-palette-grey-lighten4);
    padding: 0.75rem;
    border-radius: 12px 12px 12px 4px;
}

.agent-card {
    cursor: pointer;
    transition: transform 0.2s, box-shadow 0.2s;
}

.agent-card:hover {
    transform: translateY(-2px);
    box-shadow: 0 4px 12px rgba(0,0,0,0.15);
}

@keyframes slideIn {
    from {
        opacity: 0;
        transform: translateY(10px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}

.typing-indicator {
    display: flex;
    align-items: center;
    padding: 0.75rem;
}

.typing-indicator span {
    height: 8px;
    width: 8px;
    background-color: var(--mud-palette-grey);
    border-radius: 50%;
    display: inline-block;
    margin-right: 4px;
    animation: typing 1.4s infinite;
}

.typing-indicator span:nth-child(2) {
    animation-delay: 0.2s;
}

.typing-indicator span:nth-child(3) {
    animation-delay: 0.4s;
}

@keyframes typing {
    0%, 60%, 100% {
        transform: translateY(0);
    }
    30% {
        transform: translateY(-10px);
    }
}
```

### JavaScript Interop

```javascript
// wwwroot/js/app.js
window.scrollToBottom = (element) => {
    element.scrollTop = element.scrollHeight;
};

window.copyToClipboard = (text) => {
    navigator.clipboard.writeText(text);
};

window.downloadFile = (filename, content) => {
    const blob = new Blob([content], { type: 'text/plain' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    window.URL.revokeObjectURL(url);
};
```

## Component Library Summary

### Reused from claudecodewrappersharp
- MudBlazor components and theme
- MCP configuration UI
- Session management components
- Toast notifications
- File management dialogs
- Markdown renderer

### New Components
- Agent dashboard
- Multi-agent chat interface
- Orchestration planner
- Real-time message streaming
- Agent status indicators
- Session timeline

## Testing

```csharp
[TestClass]
public class ChatInterfaceTests : TestContext
{
    [TestMethod]
    public void MessageBubble_RendersUserMessage()
    {
        // Arrange
        var message = new ChatMessage
        {
            Content = "Test message",
            Role = MessageRole.User
        };
        
        // Act
        var component = RenderComponent<MessageBubble>(parameters => parameters
            .Add(p => p.Message, message));
        
        // Assert
        Assert.IsTrue(component.Find(".user-message").Exists());
    }
}
```

## Next Steps
1. Set up Blazor Server project
2. Install MudBlazor package
3. Port components from claudecodewrappersharp
4. Implement SignalR integration
5. Create responsive layouts
6. Add accessibility features

## Version History
- v1.0 - Initial specification
- Date: 2024-01-30
- Status: Ready for implementation