namespace OrchestratorChat.Core.Agents;

public class AgentInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AgentType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public AgentStatus Status { get; set; }
    public AgentCapabilities? Capabilities { get; set; }
    public DateTime LastActive { get; set; }
    public string WorkingDirectory { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
}