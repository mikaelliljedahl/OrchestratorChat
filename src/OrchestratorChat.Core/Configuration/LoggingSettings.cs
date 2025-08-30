namespace OrchestratorChat.Core.Configuration;

/// <summary>
/// Logging configuration settings
/// </summary>
public class LoggingSettings
{
    /// <summary>
    /// Minimum log level
    /// </summary>
    public string LogLevel { get; set; } = "Information";
    
    /// <summary>
    /// Whether to enable console logging
    /// </summary>
    public bool EnableConsoleLogging { get; set; } = true;
    
    /// <summary>
    /// Whether to enable file logging
    /// </summary>
    public bool EnableFileLogging { get; set; } = true;
    
    /// <summary>
    /// Path to the log file directory
    /// </summary>
    public string LogFilePath { get; set; } = "logs";
    
    /// <summary>
    /// Whether to enable structured logging
    /// </summary>
    public bool EnableStructuredLogging { get; set; } = true;
    
    /// <summary>
    /// Whether to log to external systems (e.g., Application Insights)
    /// </summary>
    public bool EnableExternalLogging { get; set; } = false;
}