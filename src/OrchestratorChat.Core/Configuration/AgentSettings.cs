using OrchestratorChat.Core.Agents;

namespace OrchestratorChat.Core.Configuration;

/// <summary>
/// Agent configuration settings
/// </summary>
public class AgentSettings
{
    /// <summary>
    /// Path to the Claude executable
    /// </summary>
    public string ClaudeExecutablePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Path to the Saturn library
    /// </summary>
    public string SaturnLibraryPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Maximum number of concurrent agents allowed
    /// </summary>
    public int MaxConcurrentAgents { get; set; } = 10;
    
    /// <summary>
    /// Default timeout for agent operations
    /// </summary>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(5);
    
    /// <summary>
    /// Default configurations for different agent types
    /// </summary>
    public Dictionary<string, AgentConfiguration> DefaultConfigurations { get; set; } = new();
}