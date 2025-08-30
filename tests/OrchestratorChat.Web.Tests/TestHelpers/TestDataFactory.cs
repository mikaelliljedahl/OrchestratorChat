using OrchestratorChat.Web.Models;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Core.Messages;
using OrchestratorChat.Core.Agents;

namespace OrchestratorChat.Web.Tests.TestHelpers;

public static class TestDataFactory
{
    public static ChatMessage CreateMessage(string content = "Test")
    {
        return new ChatMessage
        {
            Id = Guid.NewGuid().ToString(),
            Content = content,
            Role = MessageRole.User,
            AgentId = "test-agent",
            Timestamp = DateTime.UtcNow,
            Attachments = new List<Attachment>()
        };
    }
    
    public static Session CreateSession()
    {
        return new Session
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Session",
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            ParticipantAgentIds = new List<string> { "agent-1" }
        };
    }
    
    public static AgentInfo CreateAgent(string name = "TestAgent")
    {
        return new AgentInfo
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Type = AgentType.Claude,
            Status = AgentStatus.Ready,
            Description = "Test agent description"
        };
    }
    
    public static ExecutedStep CreateExecutedStep(string name = "Test Step", string status = "success")
    {
        return new ExecutedStep
        {
            Name = name,
            Status = status,
            Duration = TimeSpan.FromSeconds(1.5),
            Output = "Test output for step",
            Timestamp = DateTime.UtcNow,
            AgentId = "test-agent"
        };
    }
}