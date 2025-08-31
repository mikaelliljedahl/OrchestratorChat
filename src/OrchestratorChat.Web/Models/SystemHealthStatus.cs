namespace OrchestratorChat.Web.Models;

/// <summary>
/// Overall system health status
/// </summary>
public class SystemHealthStatus
{
    /// <summary>
    /// Claude CLI detection status
    /// </summary>
    public ProviderStatus ClaudeCli { get; set; } = ProviderStatus.NotFound;
    
    /// <summary>
    /// OpenRouter API key status
    /// </summary>
    public ProviderStatus OpenRouterKey { get; set; } = ProviderStatus.Missing;
    
    /// <summary>
    /// Anthropic API key status
    /// </summary>
    public ProviderStatus AnthropicKey { get; set; } = ProviderStatus.Missing;
    
    /// <summary>
    /// OrchestratorHub connection status
    /// </summary>
    public HubConnectionStatus OrchestratorHub { get; set; } = HubConnectionStatus.Disconnected;
    
    /// <summary>
    /// AgentHub connection status
    /// </summary>
    public HubConnectionStatus AgentHub { get; set; } = HubConnectionStatus.Disconnected;
    
    /// <summary>
    /// Overall severity based on all status checks
    /// </summary>
    public HealthSeverity OverallSeverity { get; set; } = HealthSeverity.Error;
    
    /// <summary>
    /// Primary blocking issue if any
    /// </summary>
    public string? PrimaryIssue { get; set; }
    
    /// <summary>
    /// Primary fix action for the most critical issue
    /// </summary>
    public string? PrimaryFixAction { get; set; }
}

/// <summary>
/// Health status for a specific agent
/// </summary>
public class AgentHealthStatus
{
    /// <summary>
    /// Agent ID
    /// </summary>
    public string AgentId { get; set; } = string.Empty;
    
    /// <summary>
    /// Agent initialization status
    /// </summary>
    public AgentInitializationStatus Status { get; set; } = AgentInitializationStatus.Uninitialized;
    
    /// <summary>
    /// Error message if initialization failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Health severity for this agent
    /// </summary>
    public HealthSeverity Severity { get; set; } = HealthSeverity.Error;
}

/// <summary>
/// SignalR hub connection status
/// </summary>
public enum HubConnectionStatus
{
    /// <summary>
    /// Hub is connected and ready
    /// </summary>
    Connected,
    
    /// <summary>
    /// Hub is attempting to reconnect
    /// </summary>
    Reconnecting,
    
    /// <summary>
    /// Hub is disconnected
    /// </summary>
    Disconnected
}

/// <summary>
/// Agent initialization status
/// </summary>
public enum AgentInitializationStatus
{
    /// <summary>
    /// Agent is uninitialized
    /// </summary>
    Uninitialized,
    
    /// <summary>
    /// Agent is initialized and ready
    /// </summary>
    Initialized,
    
    /// <summary>
    /// Agent initialization failed
    /// </summary>
    Error
}

/// <summary>
/// Health severity levels for UI display
/// </summary>
public enum HealthSeverity
{
    /// <summary>
    /// Everything is working correctly
    /// </summary>
    OK,
    
    /// <summary>
    /// Degraded but not blocking functionality
    /// </summary>
    Warning,
    
    /// <summary>
    /// Blocking issue that prevents normal operation
    /// </summary>
    Error
}