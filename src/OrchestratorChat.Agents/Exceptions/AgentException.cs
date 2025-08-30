namespace OrchestratorChat.Agents.Exceptions;

/// <summary>
/// Exception thrown when an agent operation fails
/// </summary>
public class AgentException : Exception
{
    public string? AgentId { get; }

    public AgentException() : base()
    {
    }

    public AgentException(string message) : base(message)
    {
    }

    public AgentException(string message, string agentId) : base(message)
    {
        AgentId = agentId;
    }

    public AgentException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public AgentException(string message, string agentId, Exception innerException) : base(message, innerException)
    {
        AgentId = agentId;
    }
}

/// <summary>
/// Exception thrown when agent initialization fails
/// </summary>
public class AgentInitializationException : AgentException
{
    public AgentInitializationException() : base()
    {
    }

    public AgentInitializationException(string message) : base(message)
    {
    }

    public AgentInitializationException(string message, string agentId) : base(message, agentId)
    {
    }

    public AgentInitializationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public AgentInitializationException(string message, string agentId, Exception innerException) : base(message, agentId, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when agent communication fails
/// </summary>
public class AgentCommunicationException : AgentException
{
    public AgentCommunicationException() : base()
    {
    }

    public AgentCommunicationException(string message) : base(message)
    {
    }

    public AgentCommunicationException(string message, string agentId) : base(message, agentId)
    {
    }

    public AgentCommunicationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public AgentCommunicationException(string message, string agentId, Exception innerException) : base(message, agentId, innerException)
    {
    }
}