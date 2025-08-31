using Microsoft.AspNetCore.SignalR.Client;
using OrchestratorChat.Web.Models;

namespace OrchestratorChat.Web.Services;

/// <summary>
/// Service for checking system health including providers, connections, and agents
/// </summary>
public interface IHealthCheckService
{
    /// <summary>
    /// Gets comprehensive health status for all system components
    /// </summary>
    /// <returns>Complete health status</returns>
    Task<SystemHealthStatus> GetSystemHealthAsync();
    
    /// <summary>
    /// Gets health status for a specific agent
    /// </summary>
    /// <param name="agentId">The agent ID to check</param>
    /// <returns>Agent health status</returns>
    Task<AgentHealthStatus> GetAgentHealthAsync(string agentId);
    
    /// <summary>
    /// Gets SignalR hub connection status
    /// </summary>
    /// <param name="hubConnection">The hub connection to check</param>
    /// <returns>Hub connection status</returns>
    HubConnectionStatus GetHubConnectionStatus(HubConnection? hubConnection);
    
    /// <summary>
    /// Retries Claude CLI detection
    /// </summary>
    /// <returns>Updated detection result</returns>
    Task<ProviderStatus> RetryClaudeDetectionAsync();
    
    /// <summary>
    /// Validates and saves OpenRouter API key
    /// </summary>
    /// <param name="apiKey">The API key to validate and save</param>
    /// <returns>Validation result</returns>
    Task<ValidationResult> SaveOpenRouterKeyAsync(string apiKey);
    
    /// <summary>
    /// Validates and saves Anthropic API key
    /// </summary>
    /// <param name="apiKey">The API key to validate and save</param>
    /// <returns>Validation result</returns>
    Task<ValidationResult> SaveAnthropicKeyAsync(string apiKey);
}