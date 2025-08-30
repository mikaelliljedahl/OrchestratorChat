namespace OrchestratorChat.Data.Models;

public class AgentConfigurationEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AgentId { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public int MaxTokens { get; set; }
    public string SystemPrompt { get; set; } = string.Empty;
    public bool RequireApproval { get; set; }
    
    // Navigation property
    public virtual AgentEntity Agent { get; set; } = null!;
    
    // JSON serialized data
    public string CustomSettingsJson { get; set; } = string.Empty; // Dictionary<string, object>
    public string EnabledToolsJson { get; set; } = string.Empty; // List<string>
    public string CapabilitiesJson { get; set; } = string.Empty; // AgentCapabilities
}