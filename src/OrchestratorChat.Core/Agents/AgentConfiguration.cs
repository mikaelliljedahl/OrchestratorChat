namespace OrchestratorChat.Core.Agents;

/// <summary>
/// Configuration settings for an agent
/// </summary>
public class AgentConfiguration
{
    /// <summary>
    /// Name of the agent
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Type of the agent
    /// </summary>
    public AgentType Type { get; set; }
    
    /// <summary>
    /// Working directory for the agent
    /// </summary>
    public string? WorkingDirectory { get; set; }
    
    /// <summary>
    /// The model to use for this agent
    /// </summary>
    public string Model { get; set; } = "claude-3-sonnet";
    
    /// <summary>
    /// Temperature setting for response generation
    /// </summary>
    public double Temperature { get; set; } = 0.7;
    
    /// <summary>
    /// Maximum number of tokens for responses
    /// </summary>
    public int MaxTokens { get; set; } = 4096;
    
    /// <summary>
    /// System prompt for the agent
    /// </summary>
    public string SystemPrompt { get; set; }
    
    /// <summary>
    /// Custom settings specific to the agent type
    /// </summary>
    public Dictionary<string, object> CustomSettings { get; set; } = new();
    
    /// <summary>
    /// List of tools enabled for this agent
    /// </summary>
    public List<string> EnabledTools { get; set; } = new();
    
    /// <summary>
    /// Whether tool execution requires user approval
    /// </summary>
    public bool RequireApproval { get; set; } = false;
}