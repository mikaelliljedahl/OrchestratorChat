namespace OrchestratorChat.Core.Sessions;

/// <summary>
/// Request to create a new session
/// </summary>
public class CreateSessionRequest
{
    /// <summary>
    /// Name of the session
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of session to create
    /// </summary>
    public SessionType Type { get; set; }

    /// <summary>
    /// List of agent IDs to participate in the session
    /// </summary>
    public List<string> AgentIds { get; set; } = new();

    /// <summary>
    /// Working directory for the session
    /// </summary>
    public string WorkingDirectory { get; set; } = string.Empty;
}