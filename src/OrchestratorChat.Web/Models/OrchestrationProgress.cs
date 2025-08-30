namespace OrchestratorChat.Web.Models;

public class OrchestrationProgress
{
    public string CurrentStep { get; set; } = string.Empty;
    public int CompletedSteps { get; set; }
    public int TotalSteps { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    
    public double PercentComplete => TotalSteps > 0 ? (double)CompletedSteps / TotalSteps * 100 : 0;
}