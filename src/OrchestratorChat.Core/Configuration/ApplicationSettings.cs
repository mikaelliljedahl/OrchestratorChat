namespace OrchestratorChat.Core.Configuration;

/// <summary>
/// Main application settings container
/// </summary>
public class ApplicationSettings
{
    /// <summary>
    /// Database configuration settings
    /// </summary>
    public DatabaseSettings Database { get; set; } = new();
    
    /// <summary>
    /// SignalR configuration settings
    /// </summary>
    public SignalRSettings SignalR { get; set; } = new();
    
    /// <summary>
    /// Agent configuration settings
    /// </summary>
    public AgentSettings Agents { get; set; } = new();
    
    /// <summary>
    /// Security configuration settings
    /// </summary>
    public SecuritySettings Security { get; set; } = new();
    
    /// <summary>
    /// Logging configuration settings
    /// </summary>
    public LoggingSettings Logging { get; set; } = new();
}