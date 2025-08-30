namespace OrchestratorChat.Core.Configuration;

/// <summary>
/// Database configuration settings
/// </summary>
public class DatabaseSettings
{
    /// <summary>
    /// Database connection string
    /// </summary>
    public string ConnectionString { get; set; }
    
    /// <summary>
    /// Command timeout in seconds
    /// </summary>
    public int CommandTimeout { get; set; } = 30;
    
    /// <summary>
    /// Whether to enable sensitive data logging (should be false in production)
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; } = false;
}