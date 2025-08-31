using Microsoft.AspNetCore.SignalR.Client;
using OrchestratorChat.Web.Models;

namespace OrchestratorChat.Web.Services;

/// <summary>
/// Service for checking system health including providers, connections, and agents
/// </summary>
public class HealthCheckService : IHealthCheckService
{
    private readonly IProviderVerificationService _providerVerificationService;
    private readonly IAgentService _agentService;
    private readonly ILogger<HealthCheckService> _logger;
    
    public HealthCheckService(
        IProviderVerificationService providerVerificationService,
        IAgentService agentService,
        ILogger<HealthCheckService> logger)
    {
        _providerVerificationService = providerVerificationService;
        _agentService = agentService;
        _logger = logger;
    }
    
    public async Task<SystemHealthStatus> GetSystemHealthAsync()
    {
        try
        {
            var providerStatus = await _providerVerificationService.GetProviderStatusAsync();
            
            var healthStatus = new SystemHealthStatus
            {
                ClaudeCli = providerStatus.ClaudeCli,
                OpenRouterKey = providerStatus.OpenRouterKey,
                AnthropicKey = providerStatus.AnthropicKey,
                // Hub connections will be updated by caller as they have access to hub instances
                OrchestratorHub = HubConnectionStatus.Disconnected,
                AgentHub = HubConnectionStatus.Disconnected
            };
            
            // Determine overall severity and primary issue
            DetermineOverallHealth(healthStatus);
            
            return healthStatus;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system health status");
            return new SystemHealthStatus
            {
                OverallSeverity = HealthSeverity.Error,
                PrimaryIssue = "Failed to check system health",
                PrimaryFixAction = "Check logs for details"
            };
        }
    }
    
    public async Task<AgentHealthStatus> GetAgentHealthAsync(string agentId)
    {
        try
        {
            var agents = await _agentService.GetConfiguredAgentsAsync();
            var agent = agents.FirstOrDefault(a => a.Id == agentId);
            
            if (agent == null)
            {
                return new AgentHealthStatus
                {
                    AgentId = agentId,
                    Status = AgentInitializationStatus.Error,
                    ErrorMessage = "Agent not found",
                    Severity = HealthSeverity.Error
                };
            }
            
            var status = agent.Status switch
            {
                Core.Agents.AgentStatus.Ready => AgentInitializationStatus.Initialized,
                Core.Agents.AgentStatus.Error => AgentInitializationStatus.Error,
                Core.Agents.AgentStatus.Uninitialized => AgentInitializationStatus.Uninitialized,
                _ => AgentInitializationStatus.Uninitialized
            };
            
            var severity = status switch
            {
                AgentInitializationStatus.Initialized => HealthSeverity.OK,
                AgentInitializationStatus.Error => HealthSeverity.Error,
                _ => HealthSeverity.Warning
            };
            
            return new AgentHealthStatus
            {
                AgentId = agentId,
                Status = status,
                ErrorMessage = agent.Status == Core.Agents.AgentStatus.Error ? "Agent initialization failed" : null,
                Severity = severity
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get agent health for {AgentId}", agentId);
            return new AgentHealthStatus
            {
                AgentId = agentId,
                Status = AgentInitializationStatus.Error,
                ErrorMessage = ex.Message,
                Severity = HealthSeverity.Error
            };
        }
    }
    
    public HubConnectionStatus GetHubConnectionStatus(HubConnection? hubConnection)
    {
        if (hubConnection == null)
            return HubConnectionStatus.Disconnected;
            
        return hubConnection.State switch
        {
            HubConnectionState.Connected => HubConnectionStatus.Connected,
            HubConnectionState.Reconnecting => HubConnectionStatus.Reconnecting,
            _ => HubConnectionStatus.Disconnected
        };
    }
    
    public async Task<ProviderStatus> RetryClaudeDetectionAsync()
    {
        return await _providerVerificationService.DetectClaudeCliAsync();
    }
    
    public async Task<ValidationResult> SaveOpenRouterKeyAsync(string apiKey)
    {
        return await _providerVerificationService.ValidateOpenRouterKeyAsync(apiKey);
    }
    
    public async Task<ValidationResult> SaveAnthropicKeyAsync(string apiKey)
    {
        return await _providerVerificationService.ValidateAnthropicKeyAsync(apiKey);
    }
    
    private void DetermineOverallHealth(SystemHealthStatus health)
    {
        // Check for blocking issues first
        if (health.ClaudeCli == ProviderStatus.NotFound)
        {
            health.OverallSeverity = HealthSeverity.Error;
            health.PrimaryIssue = "Claude CLI not found";
            health.PrimaryFixAction = "Install Claude CLI";
            return;
        }
        
        if (health.OpenRouterKey == ProviderStatus.Missing && health.AnthropicKey == ProviderStatus.Missing)
        {
            health.OverallSeverity = HealthSeverity.Error;
            health.PrimaryIssue = "API key missing";
            health.PrimaryFixAction = "Add API key";
            return;
        }
        
        if (health.OrchestratorHub == HubConnectionStatus.Disconnected || health.AgentHub == HubConnectionStatus.Disconnected)
        {
            health.OverallSeverity = HealthSeverity.Error;
            health.PrimaryIssue = "Hub disconnected";
            health.PrimaryFixAction = "Reconnect";
            return;
        }
        
        // Check for warning conditions
        if (health.OrchestratorHub == HubConnectionStatus.Reconnecting || health.AgentHub == HubConnectionStatus.Reconnecting)
        {
            health.OverallSeverity = HealthSeverity.Warning;
            health.PrimaryIssue = "Reconnecting to hubs";
            health.PrimaryFixAction = null; // No action needed for reconnecting
            return;
        }
        
        // All good
        health.OverallSeverity = HealthSeverity.OK;
        health.PrimaryIssue = null;
        health.PrimaryFixAction = null;
    }
}