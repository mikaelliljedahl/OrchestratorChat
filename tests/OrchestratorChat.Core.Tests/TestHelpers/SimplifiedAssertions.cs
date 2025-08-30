using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using OrchestratorChat.Core.Messages;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Orchestration;

namespace OrchestratorChat.Core.Tests.TestHelpers;

/// <summary>
/// Simplified custom assertions for basic domain objects
/// </summary>
public static class SimplifiedAssertions
{
    /// <summary>
    /// Extension methods for Session assertions
    /// </summary>
    public static SimpleSessionAssertions Should(this Session session) => new(session);

    /// <summary>
    /// Extension methods for AgentMessage assertions
    /// </summary>
    public static SimpleMessageAssertions Should(this AgentMessage message) => new(message);

    /// <summary>
    /// Extension methods for AgentInfo assertions
    /// </summary>
    public static SimpleAgentInfoAssertions Should(this AgentInfo agentInfo) => new(agentInfo);
}

/// <summary>
/// Basic assertions for Session objects
/// </summary>
public class SimpleSessionAssertions : ReferenceTypeAssertions<Session, SimpleSessionAssertions>
{
    public SimpleSessionAssertions(Session subject) : base(subject) { }

    protected override string Identifier => "session";

    /// <summary>
    /// Assert that the session is active
    /// </summary>
    public AndConstraint<SimpleSessionAssertions> BeActive(string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(Subject.Status == SessionStatus.Active)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected session to be active{reason}, but found {0}.", Subject.Status);

        return new AndConstraint<SimpleSessionAssertions>(this);
    }

    /// <summary>
    /// Assert that the session has a specific number of messages
    /// </summary>
    public AndConstraint<SimpleSessionAssertions> HaveMessageCount(int expectedCount, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(Subject.Messages.Count == expectedCount)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected session to have {0} messages{reason}, but found {1}.", expectedCount, Subject.Messages.Count);

        return new AndConstraint<SimpleSessionAssertions>(this);
    }

    /// <summary>
    /// Assert that the session contains a specific participant agent
    /// </summary>
    public AndConstraint<SimpleSessionAssertions> HaveParticipantAgent(string agentId, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(Subject.ParticipantAgentIds.Contains(agentId))
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected session to have participant agent {0}{reason}, but it was not found.", agentId);

        return new AndConstraint<SimpleSessionAssertions>(this);
    }
}

/// <summary>
/// Basic assertions for AgentMessage objects
/// </summary>
public class SimpleMessageAssertions : ReferenceTypeAssertions<AgentMessage, SimpleMessageAssertions>
{
    public SimpleMessageAssertions(AgentMessage subject) : base(subject) { }

    protected override string Identifier => "message";

    /// <summary>
    /// Assert that the message is from a specific agent
    /// </summary>
    public AndConstraint<SimpleMessageAssertions> BeFromAgent(string agentId, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(Subject.AgentId == agentId)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected message to be from agent {0}{reason}, but found {1}.", agentId, Subject.AgentId);

        return new AndConstraint<SimpleMessageAssertions>(this);
    }

    /// <summary>
    /// Assert that the message has a specific role
    /// </summary>
    public AndConstraint<SimpleMessageAssertions> HaveRole(MessageRole role, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(Subject.Role == role)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected message to have role {0}{reason}, but found {1}.", role, Subject.Role);

        return new AndConstraint<SimpleMessageAssertions>(this);
    }

    /// <summary>
    /// Assert that the message content contains a specific text
    /// </summary>
    public AndConstraint<SimpleMessageAssertions> ContainText(string expectedText, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(Subject.Content?.Contains(expectedText) == true)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected message content to contain {0}{reason}, but found {1}.", expectedText, Subject.Content);

        return new AndConstraint<SimpleMessageAssertions>(this);
    }
}

/// <summary>
/// Basic assertions for AgentInfo objects
/// </summary>
public class SimpleAgentInfoAssertions : ReferenceTypeAssertions<AgentInfo, SimpleAgentInfoAssertions>
{
    public SimpleAgentInfoAssertions(AgentInfo subject) : base(subject) { }

    protected override string Identifier => "agent info";

    /// <summary>
    /// Assert that the agent has a specific status
    /// </summary>
    public AndConstraint<SimpleAgentInfoAssertions> HaveStatus(AgentStatus status, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(Subject.Status == status)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected agent to have status {0}{reason}, but found {1}.", status, Subject.Status);

        return new AndConstraint<SimpleAgentInfoAssertions>(this);
    }

    /// <summary>
    /// Assert that the agent is of a specific type
    /// </summary>
    public AndConstraint<SimpleAgentInfoAssertions> BeOfType(AgentType type, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(Subject.Type == type)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected agent to be of type {0}{reason}, but found {1}.", type, Subject.Type);

        return new AndConstraint<SimpleAgentInfoAssertions>(this);
    }

    /// <summary>
    /// Assert that the agent supports tools
    /// </summary>
    public AndConstraint<SimpleAgentInfoAssertions> SupportTools(string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(Subject.Capabilities?.SupportsTools == true)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected agent to support tools{reason}, but it does not.");

        return new AndConstraint<SimpleAgentInfoAssertions>(this);
    }
}