using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Messages;
using OrchestratorChat.Core.Orchestration;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Core.Tools;
using OrchestratorChat.Data.Models;

namespace OrchestratorChat.Core.Tests.Fixtures;

/// <summary>
/// Builder pattern for creating test data objects with realistic defaults
/// </summary>
public static class TestDataBuilder
{
    /// <summary>
    /// Builder for Session objects
    /// </summary>
    public class SessionBuilder
    {
        private Session _session = new();

        public SessionBuilder WithId(string id)
        {
            _session.Id = id;
            return this;
        }

        public SessionBuilder WithName(string name)
        {
            _session.Name = name;
            return this;
        }

        public SessionBuilder WithType(SessionType type)
        {
            _session.Type = type;
            return this;
        }

        public SessionBuilder WithStatus(SessionStatus status)
        {
            _session.Status = status;
            return this;
        }

        public SessionBuilder WithCreatedAt(DateTime createdAt)
        {
            _session.CreatedAt = createdAt;
            return this;
        }

        public SessionBuilder WithLastActivityAt(DateTime lastActivityAt)
        {
            _session.LastActivityAt = lastActivityAt;
            return this;
        }

        public SessionBuilder WithParticipantAgent(string agentId)
        {
            _session.ParticipantAgentIds.Add(agentId);
            return this;
        }

        public SessionBuilder WithParticipantAgents(params string[] agentIds)
        {
            _session.ParticipantAgentIds.AddRange(agentIds);
            return this;
        }

        public SessionBuilder WithMessage(AgentMessage message)
        {
            _session.Messages.Add(message);
            return this;
        }

        public SessionBuilder WithMessages(params AgentMessage[] messages)
        {
            _session.Messages.AddRange(messages);
            return this;
        }

        public SessionBuilder WithContextValue(string key, object value)
        {
            _session.Context[key] = value;
            return this;
        }

        public SessionBuilder WithWorkingDirectory(string workingDirectory)
        {
            _session.WorkingDirectory = workingDirectory;
            return this;
        }

        public SessionBuilder WithProjectId(string projectId)
        {
            _session.ProjectId = projectId;
            return this;
        }

        public Session Build() => _session;
    }

    /// <summary>
    /// Builder for AgentMessage objects
    /// </summary>
    public class MessageBuilder
    {
        private AgentMessage _message = new()
        {
            Timestamp = DateTime.UtcNow
        };

        public MessageBuilder WithId(string id)
        {
            _message.Id = id;
            return this;
        }

        public MessageBuilder WithContent(string content)
        {
            _message.Content = content;
            return this;
        }

        public MessageBuilder WithRole(MessageRole role)
        {
            _message.Role = role;
            return this;
        }

        public MessageBuilder WithAgentId(string agentId)
        {
            _message.AgentId = agentId;
            return this;
        }

        public MessageBuilder WithSessionId(string sessionId)
        {
            _message.SessionId = sessionId;
            return this;
        }

        public MessageBuilder WithTimestamp(DateTime timestamp)
        {
            _message.Timestamp = timestamp;
            return this;
        }

        public MessageBuilder WithAttachment(Attachment attachment)
        {
            _message.Attachments.Add(attachment);
            return this;
        }

        public MessageBuilder WithAttachments(params Attachment[] attachments)
        {
            _message.Attachments.AddRange(attachments);
            return this;
        }

        public MessageBuilder WithMetadata(string key, object value)
        {
            _message.Metadata[key] = value;
            return this;
        }

        public MessageBuilder WithParentMessageId(string parentMessageId)
        {
            _message.ParentMessageId = parentMessageId;
            return this;
        }

        public MessageBuilder WithSenderId(string senderId)
        {
            _message.SenderId = senderId;
            return this;
        }

        public AgentMessage Build() => _message;
    }

    /// <summary>
    /// Builder for OrchestrationPlan objects
    /// </summary>
    public class OrchestrationPlanBuilder
    {
        private OrchestrationPlan _plan = new()
        {
            Id = Guid.NewGuid().ToString()
        };

        public OrchestrationPlanBuilder WithId(string id)
        {
            _plan.Id = id;
            return this;
        }

        public OrchestrationPlanBuilder WithName(string name)
        {
            _plan.Name = name;
            return this;
        }

        public OrchestrationPlanBuilder WithGoal(string goal)
        {
            _plan.Goal = goal;
            return this;
        }

        public OrchestrationPlanBuilder WithStrategy(OrchestrationStrategy strategy)
        {
            _plan.Strategy = strategy;
            return this;
        }

        public OrchestrationPlanBuilder WithStep(OrchestrationStep step)
        {
            _plan.Steps.Add(step);
            return this;
        }

        public OrchestrationPlanBuilder WithSteps(params OrchestrationStep[] steps)
        {
            _plan.Steps.AddRange(steps);
            return this;
        }

        public OrchestrationPlanBuilder WithRequiredAgent(string agentId)
        {
            _plan.RequiredAgents.Add(agentId);
            return this;
        }

        public OrchestrationPlanBuilder WithRequiredAgents(params string[] agentIds)
        {
            _plan.RequiredAgents.AddRange(agentIds);
            return this;
        }

        public OrchestrationPlanBuilder WithSharedContext(string key, object value)
        {
            _plan.SharedContext[key] = value;
            return this;
        }

        public OrchestrationPlan Build() => _plan;
    }

    /// <summary>
    /// Builder for OrchestrationStep objects
    /// </summary>
    public class OrchestrationStepBuilder
    {
        private OrchestrationStep _step = new();

        public OrchestrationStepBuilder WithOrder(int order)
        {
            _step.Order = order;
            return this;
        }

        public OrchestrationStepBuilder WithAgentId(string agentId)
        {
            _step.AgentId = agentId;
            return this;
        }

        public OrchestrationStepBuilder WithTask(string task)
        {
            _step.Task = task;
            return this;
        }

        public OrchestrationStepBuilder WithDependencies(params string[] dependencies)
        {
            _step.DependsOn.AddRange(dependencies);
            return this;
        }

        public OrchestrationStepBuilder WithDescription(string description)
        {
            _step.Description = description;
            return this;
        }

        public OrchestrationStepBuilder WithAssignedAgentId(string agentId)
        {
            _step.AssignedAgentId = agentId;
            return this;
        }

        public OrchestrationStepBuilder WithInput(string key, object value)
        {
            _step.Input[key] = value;
            return this;
        }

        public OrchestrationStepBuilder WithTimeout(TimeSpan timeout)
        {
            _step.Timeout = timeout;
            return this;
        }

        public OrchestrationStepBuilder WithExpectedDuration(TimeSpan duration)
        {
            _step.ExpectedDuration = duration;
            return this;
        }

        public OrchestrationStepBuilder WithCanRunInParallel(bool canRunInParallel = true)
        {
            _step.CanRunInParallel = canRunInParallel;
            return this;
        }

        public OrchestrationStep Build() => _step;
    }

    /// <summary>
    /// Builder for AgentInfo objects
    /// </summary>
    public class AgentInfoBuilder
    {
        private AgentInfo _agentInfo = new()
        {
            Id = Guid.NewGuid().ToString()
        };

        public AgentInfoBuilder WithId(string id)
        {
            _agentInfo.Id = id;
            return this;
        }

        public AgentInfoBuilder WithName(string name)
        {
            _agentInfo.Name = name;
            return this;
        }

        public AgentInfoBuilder WithType(AgentType type)
        {
            _agentInfo.Type = type;
            return this;
        }

        public AgentInfoBuilder WithStatus(AgentStatus status)
        {
            _agentInfo.Status = status;
            return this;
        }

        public AgentInfoBuilder WithCapabilities(AgentCapabilities capabilities)
        {
            _agentInfo.Capabilities = capabilities;
            return this;
        }

        public AgentInfoBuilder WithDescription(string description)
        {
            _agentInfo.Description = description;
            return this;
        }

        public AgentInfoBuilder WithLastActive(DateTime lastActive)
        {
            _agentInfo.LastActive = lastActive;
            return this;
        }

        public AgentInfoBuilder WithWorkingDirectory(string workingDirectory)
        {
            _agentInfo.WorkingDirectory = workingDirectory;
            return this;
        }

        public AgentInfoBuilder WithConfigurationValue(string key, object value)
        {
            _agentInfo.Configuration[key] = value;
            return this;
        }

        public AgentInfo Build() => _agentInfo;
    }

    /// <summary>
    /// Builder for ToolCall objects
    /// </summary>
    public class ToolCallBuilder
    {
        private string _id = Guid.NewGuid().ToString();
        private string _toolName = string.Empty;
        private Dictionary<string, object> _parameters = new();
        private string? _agentId;
        private string? _sessionId;
        private DateTime _timestamp = DateTime.UtcNow;

        public ToolCallBuilder WithId(string id)
        {
            _id = id;
            return this;
        }

        public ToolCallBuilder WithToolName(string toolName)
        {
            _toolName = toolName;
            return this;
        }

        public ToolCallBuilder WithParameter(string name, object value)
        {
            _parameters[name] = value;
            return this;
        }

        public ToolCallBuilder WithParameters(Dictionary<string, object> parameters)
        {
            _parameters = parameters;
            return this;
        }

        public ToolCallBuilder WithAgentId(string agentId)
        {
            _agentId = agentId;
            return this;
        }

        public ToolCallBuilder WithSessionId(string sessionId)
        {
            _sessionId = sessionId;
            return this;
        }

        public ToolCallBuilder WithTimestamp(DateTime timestamp)
        {
            _timestamp = timestamp;
            return this;
        }

        public ToolCall Build() => new()
        {
            Id = _id,
            ToolName = _toolName,
            Parameters = _parameters,
            AgentId = _agentId,
            SessionId = _sessionId,
            Timestamp = _timestamp
        };
    }

    /// <summary>
    /// Entry points for creating builders
    /// </summary>
    public static SessionBuilder Session() => new();
    public static MessageBuilder Message() => new();
    public static OrchestrationPlanBuilder OrchestrationPlan() => new();
    public static OrchestrationStepBuilder OrchestrationStep() => new();
    public static AgentInfoBuilder AgentInfo() => new();
    public static ToolCallBuilder ToolCall() => new();

    /// <summary>
    /// Create a typical active session with default values
    /// </summary>
    public static Session DefaultSession(string? id = null) =>
        Session()
            .WithId(id ?? Guid.NewGuid().ToString())
            .WithName("Test Session")
            .WithType(SessionType.MultiAgent)
            .WithStatus(SessionStatus.Active)
            .WithCreatedAt(DateTime.UtcNow.AddMinutes(-30))
            .WithLastActivityAt(DateTime.UtcNow)
            .WithWorkingDirectory("/test/directory")
            .Build();

    /// <summary>
    /// Create a typical user message
    /// </summary>
    public static AgentMessage DefaultUserMessage(string sessionId, string? content = null) =>
        Message()
            .WithContent(content ?? "Hello, this is a test message")
            .WithRole(MessageRole.User)
            .WithSessionId(sessionId)
            .WithAgentId("user")
            .Build();

    /// <summary>
    /// Create a typical assistant message
    /// </summary>
    public static AgentMessage DefaultAssistantMessage(string sessionId, string agentId, string? content = null) =>
        Message()
            .WithContent(content ?? "Hello, I'm an AI assistant")
            .WithRole(MessageRole.Assistant)
            .WithSessionId(sessionId)
            .WithAgentId(agentId)
            .Build();

    /// <summary>
    /// Create a typical orchestration plan
    /// </summary>
    public static OrchestrationPlan DefaultOrchestrationPlan(string? goal = null) =>
        OrchestrationPlan()
            .WithName("Test Plan")
            .WithGoal(goal ?? "Complete a test orchestration")
            .WithStrategy(OrchestrationStrategy.Sequential)
            .WithRequiredAgents("agent1", "agent2")
            .Build();

    /// <summary>
    /// Create a typical orchestration step
    /// </summary>
    public static OrchestrationStep DefaultOrchestrationStep(int order = 0, string? agentId = null, string? task = null) =>
        OrchestrationStep()
            .WithOrder(order)
            .WithAgentId(agentId ?? "test-agent")
            .WithTask(task ?? "Perform test task")
            .WithDescription("A test orchestration step")
            .WithTimeout(TimeSpan.FromMinutes(5))
            .Build();

    /// <summary>
    /// Create a typical agent info
    /// </summary>
    public static AgentInfo DefaultAgentInfo(string? name = null) =>
        AgentInfo()
            .WithName(name ?? "TestAgent")
            .WithType(AgentType.Claude)
            .WithStatus(AgentStatus.Ready)
            .WithCapabilities(new AgentCapabilities
            {
                SupportsTools = true,
                SupportsFileOperations = true,
                MaxTokens = 100000,
                MaxConcurrentRequests = 1,
                SupportedModels = new List<string> { "claude-3-sonnet", "claude-3-haiku" }
            })
            .WithDescription("A test agent")
            .WithLastActive(DateTime.UtcNow)
            .WithWorkingDirectory("/test/workspace")
            .Build();
}