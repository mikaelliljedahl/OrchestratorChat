using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using OrchestratorChat.Web.Models;
using OrchestratorChat.Web.Services;

namespace OrchestratorChat.Web.Components;

public partial class HealthPanel : ComponentBase, IAsyncDisposable
{
    [Parameter] public HubConnection? OrchestratorHub { get; set; }
    [Parameter] public HubConnection? AgentHub { get; set; }
    [Parameter] public string? SelectedAgentId { get; set; }
    [Parameter] public bool ShowDiagnostics { get; set; } = true;
    [Parameter] public bool ShowOpenRouterStatus { get; set; } = true;
    [Parameter] public EventCallback OnReconnectRequested { get; set; }
    [Parameter] public EventCallback<string> OnAgentRetryRequested { get; set; }
    [Parameter] public EventCallback OnHealthUpdated { get; set; }
    
    [Inject] private IHealthCheckService HealthCheckService { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    
    private SystemHealthStatus? _systemHealth;
    private AgentHealthStatus? _agentHealth;
    private bool _showOpenRouterKeyInput = false;
    private bool _showAnthropicKeyInput = false;
    private string _openRouterKeyInput = string.Empty;
    private string _anthropicKeyInput = string.Empty;
    private Timer? _healthCheckTimer;
    
    protected override async Task OnInitializedAsync()
    {
        await RefreshHealthStatus();
        
        // Start periodic health checks (every 10 seconds to avoid excessive API calls)
        _healthCheckTimer = new Timer(async _ => await RefreshHealthStatus(), null, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(10));
    }
    
    protected override async Task OnParametersSetAsync()
    {
        await RefreshHealthStatus();
    }
    
    private async Task RefreshHealthStatus()
    {
        try
        {
            _systemHealth = await HealthCheckService.GetSystemHealthAsync();
            
            // Update hub connection status
            if (_systemHealth != null)
            {
                _systemHealth.OrchestratorHub = HealthCheckService.GetHubConnectionStatus(OrchestratorHub);
                _systemHealth.AgentHub = HealthCheckService.GetHubConnectionStatus(AgentHub);
            }
            
            // Get agent health if agent is selected
            if (!string.IsNullOrEmpty(SelectedAgentId))
            {
                _agentHealth = await HealthCheckService.GetAgentHealthAsync(SelectedAgentId);
            }
            
            await InvokeAsync(StateHasChanged);
            await OnHealthUpdated.InvokeAsync();
        }
        catch (Exception ex)
        {
            // Log error but don't throw - health panel should be resilient
            Console.WriteLine($"Health check failed: {ex.Message}");
        }
    }
    
    private async Task InstallClaudeCli()
    {
        await JSRuntime.InvokeVoidAsync("open", "https://docs.anthropic.com/claude/reference/cli", "_blank");
    }
    
    private async Task RetryClaudeDetection()
    {
        var result = await HealthCheckService.RetryClaudeDetectionAsync();
        if (_systemHealth != null)
        {
            _systemHealth.ClaudeCli = result;
            await InvokeAsync(StateHasChanged);
        }
    }
    
    private async Task SaveOpenRouterKey()
    {
        if (string.IsNullOrWhiteSpace(_openRouterKeyInput))
            return;
            
        var result = await HealthCheckService.SaveOpenRouterKeyAsync(_openRouterKeyInput);
        if (result.IsValid && _systemHealth != null)
        {
            _systemHealth.OpenRouterKey = result.Status;
            _showOpenRouterKeyInput = false;
            _openRouterKeyInput = string.Empty;
            await InvokeAsync(StateHasChanged);
        }
    }
    
    private async Task SaveAnthropicKey()
    {
        if (string.IsNullOrWhiteSpace(_anthropicKeyInput))
            return;
            
        var result = await HealthCheckService.SaveAnthropicKeyAsync(_anthropicKeyInput);
        if (result.IsValid && _systemHealth != null)
        {
            _systemHealth.AnthropicKey = result.Status;
            _showAnthropicKeyInput = false;
            _anthropicKeyInput = string.Empty;
            await InvokeAsync(StateHasChanged);
        }
    }
    
    private async Task RequestReconnect()
    {
        await OnReconnectRequested.InvokeAsync();
    }
    
    private async Task RetryAgentInitialization()
    {
        if (!string.IsNullOrEmpty(SelectedAgentId))
        {
            await OnAgentRetryRequested.InvokeAsync(SelectedAgentId);
        }
    }
    
    private Task ShowAgentDetails()
    {
        // TODO: Show agent error details in a dialog
        return Task.CompletedTask;
    }
    
    // Status text methods
    private string GetClaudeStatusText() => _systemHealth?.ClaudeCli == ProviderStatus.Detected ? "Detected" : "Not found";
    private string GetOpenRouterStatusText() => _systemHealth?.OpenRouterKey == ProviderStatus.Present ? "Present" : "Missing";
    private string GetAnthropicStatusText() => _systemHealth?.AnthropicKey == ProviderStatus.Present ? "Present" : "Missing";
    private string GetAgentStatusText() => _agentHealth?.Status switch
    {
        AgentInitializationStatus.Initialized => "Ready",
        AgentInitializationStatus.Error => "Error initializing",
        _ => "Initializing"
    };
    
    private string GetHubsStatusText()
    {
        if (_systemHealth?.OrchestratorHub == HubConnectionStatus.Connected && _systemHealth?.AgentHub == HubConnectionStatus.Connected)
            return "Connected";
        if (_systemHealth?.OrchestratorHub == HubConnectionStatus.Reconnecting || _systemHealth?.AgentHub == HubConnectionStatus.Reconnecting)
            return "Reconnecting";
        return "Disconnected";
    }
    
    // Tooltip methods
    private string GetClaudeTooltip() => $"Claude CLI: {GetClaudeStatusText()}";
    private string GetOpenRouterTooltip() => $"OpenRouter key: {GetOpenRouterStatusText()}";
    private string GetAnthropicTooltip() => $"Anthropic key: {GetAnthropicStatusText()}";
    private string GetHubsTooltip() => $"OrchestratorHub & AgentHub: {GetHubsStatusText()}";
    private string GetAgentTooltip() => $"Agent status: {GetAgentStatusText()}";
    
    // Color methods
    private Color GetClaudeColor() => _systemHealth?.ClaudeCli == ProviderStatus.Detected ? Color.Success : Color.Error;
    private Color GetOpenRouterColor() => _systemHealth?.OpenRouterKey == ProviderStatus.Present ? Color.Success : Color.Error;
    private Color GetAnthropicColor() => _systemHealth?.AnthropicKey == ProviderStatus.Present ? Color.Success : Color.Warning;
    private Color GetAgentColor() => _agentHealth?.Severity switch
    {
        HealthSeverity.OK => Color.Success,
        HealthSeverity.Warning => Color.Warning,
        HealthSeverity.Error => Color.Error,
        _ => Color.Default
    };
    
    private Color GetHubsColor()
    {
        if (_systemHealth?.OrchestratorHub == HubConnectionStatus.Connected && _systemHealth?.AgentHub == HubConnectionStatus.Connected)
            return Color.Success;
        if (_systemHealth?.OrchestratorHub == HubConnectionStatus.Reconnecting || _systemHealth?.AgentHub == HubConnectionStatus.Reconnecting)
            return Color.Warning;
        return Color.Error;
    }
    
    // Icon methods
    private string GetClaudeIcon() => _systemHealth?.ClaudeCli == ProviderStatus.Detected ? Icons.Material.Filled.CheckCircle : Icons.Material.Filled.Error;
    private string GetOpenRouterIcon() => _systemHealth?.OpenRouterKey == ProviderStatus.Present ? Icons.Material.Filled.CheckCircle : Icons.Material.Filled.Error;
    private string GetAnthropicIcon() => _systemHealth?.AnthropicKey == ProviderStatus.Present ? Icons.Material.Filled.CheckCircle : Icons.Material.Filled.Warning;
    private string GetAgentIcon() => _agentHealth?.Severity switch
    {
        HealthSeverity.OK => Icons.Material.Filled.CheckCircle,
        HealthSeverity.Warning => Icons.Material.Filled.Warning,
        HealthSeverity.Error => Icons.Material.Filled.Error,
        _ => Icons.Material.Filled.Help
    };
    
    private string GetHubsIcon()
    {
        if (_systemHealth?.OrchestratorHub == HubConnectionStatus.Connected && _systemHealth?.AgentHub == HubConnectionStatus.Connected)
            return Icons.Material.Filled.CheckCircle;
        if (_systemHealth?.OrchestratorHub == HubConnectionStatus.Reconnecting || _systemHealth?.AgentHub == HubConnectionStatus.Reconnecting)
            return Icons.Material.Filled.Sync;
        return Icons.Material.Filled.Error;
    }
    
    public ValueTask DisposeAsync()
    {
        _healthCheckTimer?.Dispose();
        return ValueTask.CompletedTask;
    }
}