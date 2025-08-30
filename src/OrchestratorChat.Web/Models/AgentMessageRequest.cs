namespace OrchestratorChat.Web.Models;

public class AgentMessageRequest
{
    public string AgentId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string>? AttachmentPaths { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}