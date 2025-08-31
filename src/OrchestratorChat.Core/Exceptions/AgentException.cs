using OrchestratorChat.Core.Agents;

namespace OrchestratorChat.Core.Exceptions;

/// <summary>
/// Exception thrown when an agent-related error occurs
/// </summary>
public class AgentException : OrchestratorException
{
    /// <summary>
    /// ID of the agent that caused the exception
    /// </summary>
    public string AgentId { get; set; } = string.Empty;
    
    /// <summary>
    /// Current status of the agent when the exception occurred
    /// </summary>
    public AgentStatus AgentStatus { get; set; }
    
    /// <summary>
    /// Initializes a new instance of the AgentException class
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="agentId">ID of the agent that caused the exception</param>
    public AgentException(string message, string agentId) 
        : base(message, "AGENT_ERROR")
    {
        AgentId = agentId ?? string.Empty;
    }
    
    /// <summary>
    /// Initializes a new instance of the AgentException class with an inner exception
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="agentId">ID of the agent that caused the exception</param>
    /// <param name="innerException">Inner exception that caused this exception</param>
    public AgentException(string message, string agentId, Exception innerException) 
        : base(message, innerException, "AGENT_ERROR")
    {
        AgentId = agentId ?? string.Empty;
    }
    
    /// <summary>
    /// Initializes a new instance of the AgentException class with agent status
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="agentId">ID of the agent that caused the exception</param>
    /// <param name="agentStatus">Current status of the agent</param>
    public AgentException(string message, string agentId, AgentStatus agentStatus) 
        : base(message, "AGENT_ERROR")
    {
        AgentId = agentId ?? string.Empty;
        AgentStatus = agentStatus;
    }
}