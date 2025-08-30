using OrchestratorChat.Core.Messages;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Orchestration;
using Xunit;

namespace OrchestratorChat.Core.Tests.TestHelpers;

/// <summary>
/// Static helper methods for common domain object assertions
/// </summary>
public static class SimplifiedAssertions
{
    /// <summary>
    /// Session assertion helpers
    /// </summary>
    public static class SessionAssert
    {
        /// <summary>
        /// Assert that the session is active
        /// </summary>
        public static void Active(Session session)
        {
            Assert.NotNull(session);
            Assert.Equal(SessionStatus.Active, session.Status);
        }

        /// <summary>
        /// Assert that the session has a specific number of messages
        /// </summary>
        public static void MessageCount(Session session, int expectedCount)
        {
            Assert.NotNull(session);
            Assert.Equal(expectedCount, session.Messages.Count);
        }

        /// <summary>
        /// Assert that the session contains a specific participant agent
        /// </summary>
        public static void HasParticipantAgent(Session session, string agentId)
        {
            Assert.NotNull(session);
            Assert.Contains(agentId, session.ParticipantAgentIds);
        }
    }

    /// <summary>
    /// AgentMessage assertion helpers
    /// </summary>
    public static class MessageAssert
    {
        /// <summary>
        /// Assert that the message is from a specific agent
        /// </summary>
        public static void FromAgent(AgentMessage message, string agentId)
        {
            Assert.NotNull(message);
            Assert.Equal(agentId, message.AgentId);
        }

        /// <summary>
        /// Assert that the message has a specific role
        /// </summary>
        public static void HasRole(AgentMessage message, MessageRole role)
        {
            Assert.NotNull(message);
            Assert.Equal(role, message.Role);
        }

        /// <summary>
        /// Assert that the message content contains a specific text
        /// </summary>
        public static void ContainsText(AgentMessage message, string expectedText)
        {
            Assert.NotNull(message);
            Assert.NotNull(message.Content);
            Assert.Contains(expectedText, message.Content);
        }
    }

    /// <summary>
    /// AgentInfo assertion helpers
    /// </summary>
    public static class AgentInfoAssert
    {
        /// <summary>
        /// Assert that the agent has a specific status
        /// </summary>
        public static void HasStatus(AgentInfo agentInfo, AgentStatus status)
        {
            Assert.NotNull(agentInfo);
            Assert.Equal(status, agentInfo.Status);
        }

        /// <summary>
        /// Assert that the agent is of a specific type
        /// </summary>
        public static void OfType(AgentInfo agentInfo, AgentType type)
        {
            Assert.NotNull(agentInfo);
            Assert.Equal(type, agentInfo.Type);
        }

        /// <summary>
        /// Assert that the agent supports tools
        /// </summary>
        public static void SupportsTools(AgentInfo agentInfo)
        {
            Assert.NotNull(agentInfo);
            Assert.NotNull(agentInfo.Capabilities);
            Assert.True(agentInfo.Capabilities.SupportsTools);
        }
    }
}