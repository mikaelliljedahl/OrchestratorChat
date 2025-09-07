namespace OrchestratorChat.Core.Timeline;

/// <summary>
/// Represents the different types of events that can appear in a session timeline
/// </summary>
public enum TimelineEventType
{
    /// <summary>
    /// A message event (agent communication)
    /// </summary>
    Message,
    
    /// <summary>
    /// A decision event (proposal, voting, commitment)
    /// </summary>
    Decision,
    
    /// <summary>
    /// A handoff event (agent-to-agent transfer)
    /// </summary>
    Handoff,
    
    /// <summary>
    /// An approval event (request, grant, reject)
    /// </summary>
    Approval,
    
    /// <summary>
    /// An orchestration step execution event
    /// </summary>
    StepExecution,
    
    /// <summary>
    /// A tool execution event
    /// </summary>
    ToolExecution
}