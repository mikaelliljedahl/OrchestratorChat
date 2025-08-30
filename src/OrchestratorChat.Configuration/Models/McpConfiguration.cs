namespace OrchestratorChat.Configuration.Models;

/// <summary>
/// Configuration for Model Context Protocol (MCP) tools
/// </summary>
public class McpConfiguration
{
    /// <summary>
    /// Name of the MCP configuration
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of what this MCP configuration provides
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Command to execute the MCP server
    /// </summary>
    public string Command { get; set; } = string.Empty;
    
    /// <summary>
    /// Arguments to pass to the MCP server command
    /// </summary>
    public List<string> Arguments { get; set; } = new();
    
    /// <summary>
    /// Environment variables to set when running the MCP server
    /// </summary>
    public Dictionary<string, string> Environment { get; set; } = new();
    
    /// <summary>
    /// Whether this MCP configuration is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Timeout for MCP server operations in milliseconds
    /// </summary>
    public int TimeoutMs { get; set; } = 30000;
    
    /// <summary>
    /// Maximum number of retries for failed operations
    /// </summary>
    public int MaxRetries { get; set; } = 3;
    
    /// <summary>
    /// Working directory for the MCP server
    /// </summary>
    public string? WorkingDirectory { get; set; }
    
    /// <summary>
    /// Tags for categorizing this MCP configuration
    /// </summary>
    public List<string> Tags { get; set; } = new();
    
    /// <summary>
    /// When this configuration was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When this configuration was last modified
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}