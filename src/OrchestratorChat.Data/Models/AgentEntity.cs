using OrchestratorChat.Core.Agents;

namespace OrchestratorChat.Data.Models;

public class AgentEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public AgentType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<SessionAgentEntity> SessionAgents { get; set; } = new List<SessionAgentEntity>();
    public virtual ICollection<MessageEntity> Messages { get; set; } = new List<MessageEntity>();
    public virtual AgentConfigurationEntity? Configuration { get; set; }
    
    // Statistics
    public int TotalSessions { get; set; }
    public int TotalMessages { get; set; }
    public long TotalTokensUsed { get; set; }
}