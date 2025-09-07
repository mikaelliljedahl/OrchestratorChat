using OrchestratorChat.Core.Events;
using OrchestratorChat.Core.Orchestration;

namespace OrchestratorChat.Core.Timeline;

/// <summary>
/// Timeline event representing orchestration step execution
/// </summary>
public class StepExecutionTimelineEvent : ITimelineEvent
{
    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    public string Id { get; }
    
    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    public DateTime Timestamp { get; }
    
    /// <summary>
    /// Type of timeline event
    /// </summary>
    public TimelineEventType EventType { get; } = TimelineEventType.StepExecution;
    
    /// <summary>
    /// ID of the session this event belongs to
    /// </summary>
    public string SessionId { get; }
    
    /// <summary>
    /// ID of the agent involved in this event
    /// </summary>
    public string? AgentId { get; }
    
    /// <summary>
    /// Title or summary of the event for display purposes
    /// </summary>
    public string Title { get; }
    
    /// <summary>
    /// Detailed description of the event
    /// </summary>
    public string Description { get; }
    
    /// <summary>
    /// Additional metadata about the event
    /// </summary>
    public Dictionary<string, object> Metadata { get; }
    
    /// <summary>
    /// The step ID this event relates to
    /// </summary>
    public string StepId { get; }
    
    /// <summary>
    /// Initializes a new StepExecutionTimelineEvent from a StepCompletedEvent
    /// </summary>
    /// <param name="stepCompletedEvent">The step completed event</param>
    public StepExecutionTimelineEvent(StepCompletedEvent stepCompletedEvent)
    {
        Id = stepCompletedEvent.Id;
        Timestamp = stepCompletedEvent.Timestamp;
        SessionId = string.Empty; // OrchestrationStep doesn't have SessionId
        AgentId = stepCompletedEvent.Step.AssignedAgentId;
        StepId = stepCompletedEvent.ExecutionId + "_" + stepCompletedEvent.Step.Order;
        
        Title = $"Step Completed: {stepCompletedEvent.Step.Name}";
        Description = stepCompletedEvent.Step.Description ?? string.Empty;
        
        Metadata = new Dictionary<string, object>
        {
            ["StepId"] = StepId,
            ["StepName"] = stepCompletedEvent.Step.Name,
            ["ExecutedBy"] = stepCompletedEvent.Step.AssignedAgentId ?? string.Empty,
            ["Success"] = stepCompletedEvent.Result.Success,
            ["Duration"] = stepCompletedEvent.Result.ExecutionTime.ToString(),
            ["ExecutionId"] = stepCompletedEvent.ExecutionId
        };
        
        if (!string.IsNullOrEmpty(stepCompletedEvent.Result.Output))
        {
            Metadata["Output"] = stepCompletedEvent.Result.Output;
        }
        
        if (!string.IsNullOrEmpty(stepCompletedEvent.Result.Error))
        {
            Metadata["ErrorMessage"] = stepCompletedEvent.Result.Error;
        }
    }
}

/// <summary>
/// Timeline event representing tool execution
/// </summary>
public class ToolExecutionTimelineEvent : ITimelineEvent
{
    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    public string Id { get; }
    
    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    public DateTime Timestamp { get; }
    
    /// <summary>
    /// Type of timeline event
    /// </summary>
    public TimelineEventType EventType { get; } = TimelineEventType.ToolExecution;
    
    /// <summary>
    /// ID of the session this event belongs to
    /// </summary>
    public string SessionId { get; }
    
    /// <summary>
    /// ID of the agent involved in this event
    /// </summary>
    public string? AgentId { get; }
    
    /// <summary>
    /// Title or summary of the event for display purposes
    /// </summary>
    public string Title { get; }
    
    /// <summary>
    /// Detailed description of the event
    /// </summary>
    public string Description { get; }
    
    /// <summary>
    /// Additional metadata about the event
    /// </summary>
    public Dictionary<string, object> Metadata { get; }
    
    /// <summary>
    /// The tool name that was executed
    /// </summary>
    public string ToolName { get; }
    
    /// <summary>
    /// Initializes a new ToolExecutionTimelineEvent from a ToolExecutedEvent
    /// </summary>
    /// <param name="toolExecutedEvent">The tool executed event</param>
    public ToolExecutionTimelineEvent(ToolExecutedEvent toolExecutedEvent)
    {
        Id = toolExecutedEvent.Id;
        Timestamp = toolExecutedEvent.Timestamp;
        SessionId = toolExecutedEvent.Call.SessionId ?? string.Empty;
        AgentId = toolExecutedEvent.Call.AgentId;
        ToolName = toolExecutedEvent.Call.ToolName;
        
        Title = $"Tool Executed: {toolExecutedEvent.Call.ToolName}";
        Description = $"Tool {toolExecutedEvent.Call.ToolName} executed by {toolExecutedEvent.Call.AgentId}";
        
        Metadata = new Dictionary<string, object>
        {
            ["ToolName"] = ToolName,
            ["ExecutedBy"] = toolExecutedEvent.Call.AgentId ?? string.Empty,
            ["Success"] = toolExecutedEvent.Result.Success,
            ["Duration"] = toolExecutedEvent.Result.ExecutionTime.ToString()
        };
        
        if (toolExecutedEvent.Call.Parameters?.Any() == true)
        {
            Metadata["Parameters"] = toolExecutedEvent.Call.Parameters;
        }
        
        if (!string.IsNullOrEmpty(toolExecutedEvent.Result.Output))
        {
            Metadata["Result"] = toolExecutedEvent.Result.Output;
        }
        
        if (!string.IsNullOrEmpty(toolExecutedEvent.Result.Error))
        {
            Metadata["ErrorMessage"] = toolExecutedEvent.Result.Error;
        }
    }
}