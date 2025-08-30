using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Core.Messages;
using OrchestratorChat.Core.Orchestration;
using OrchestratorChat.Core.Tools;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.SignalR.Contracts.Responses;
using OrchestratorChat.SignalR.Events;
using System.Runtime.CompilerServices;

namespace OrchestratorChat.SignalR.IntegrationTests.Helpers
{
    /// <summary>
    /// Builder class for creating test data objects
    /// </summary>
    public static class TestDataBuilder
    {
        /// <summary>
        /// Creates a test session with default values
        /// </summary>
        public static Session CreateTestSession(string? id = null, string? name = null)
        {
            return new Session
            {
                Id = id ?? Guid.NewGuid().ToString(),
                Name = name ?? "Test Session",
                Type = SessionType.MultiAgent,
                CreatedAt = DateTime.UtcNow,
                Status = SessionStatus.Active,
                ParticipantAgentIds = new List<string> { "agent-1", "agent-2" },
                Messages = new List<AgentMessage>(),
                WorkingDirectory = "/test",
                Context = new Dictionary<string, object>
                {
                    ["TestProperty"] = "TestValue"
                }
            };
        }

        /// <summary>
        /// Creates a test agent message
        /// </summary>
        public static AgentMessage CreateTestAgentMessage(string agentId, string content, string? sessionId = null)
        {
            return new AgentMessage
            {
                Id = Guid.NewGuid().ToString(),
                Content = content,
                Role = MessageRole.Assistant,
                AgentId = agentId,
                SessionId = sessionId ?? "test-session",
                Timestamp = DateTime.UtcNow,
                Attachments = new List<Attachment>(),
                Metadata = new Dictionary<string, object>
                {
                    ["TestMessage"] = true
                }
            };
        }

        /// <summary>
        /// Creates a test orchestration plan
        /// </summary>
        public static OrchestrationPlan CreateTestOrchestrationPlan(string? id = null)
        {
            return new OrchestrationPlan
            {
                Id = id ?? Guid.NewGuid().ToString(),
                Goal = "Test orchestration goal",
                Strategy = OrchestrationStrategy.Sequential,
                Steps = new List<OrchestrationStep>
                {
                    new OrchestrationStep
                    {
                        AgentId = "agent-1",
                        Task = "Set up project structure",
                        Order = 1
                    },
                    new OrchestrationStep
                    {
                        AgentId = "agent-2", 
                        Task = "Implement core features",
                        Order = 2
                    }
                }
            };
        }

        /// <summary>
        /// Creates a test orchestration result
        /// </summary>
        public static OrchestrationResult CreateTestOrchestrationResult()
        {
            return new OrchestrationResult
            {
                Success = true,
                StepResults = new List<StepResult>
                {
                    new StepResult
                    {
                        StepOrder = 1,
                        StepName = "Project structure initialization",
                        AgentId = "agent-1",
                        Success = true,
                        Output = "Project structure initialized",
                        StartTime = DateTime.UtcNow.AddMinutes(-5),
                        ExecutionTime = TimeSpan.FromMinutes(2)
                    }
                },
                FinalOutput = "Orchestration completed successfully",
                TotalDuration = TimeSpan.FromMinutes(5)
            };
        }

        /// <summary>
        /// Creates a test orchestration progress
        /// </summary>
        public static OrchestrationProgress CreateTestOrchestrationProgress()
        {
            return new OrchestrationProgress
            {
                CurrentStep = 1,
                TotalSteps = 2,
                CurrentAgent = "agent-1",
                CurrentTask = "Initialize Project",
                PercentComplete = 50.0,
                ElapsedTime = TimeSpan.FromMinutes(2)
            };
        }

        /// <summary>
        /// Creates a test tool result
        /// </summary>
        public static ToolExecutionResult CreateTestToolResult(bool success = true)
        {
            return new ToolExecutionResult
            {
                Success = success,
                Output = success ? "Tool execution completed successfully" : "Tool execution failed",
                Error = success ? null : "Test error message",
                ExecutionTime = TimeSpan.FromMilliseconds(250),
                Metadata = new Dictionary<string, object>
                {
                    ["TestTool"] = true
                }
            };
        }

        /// <summary>
        /// Creates a test tool execution update
        /// </summary>
        public static ToolExecutionUpdate CreateTestToolExecutionUpdate()
        {
            return new ToolExecutionUpdate
            {
                ToolName = "test-tool",
                Status = "executing",
                Progress = 0.75,
                Message = "Tool execution in progress"
            };
        }

        /// <summary>
        /// Creates a test agent response stream
        /// </summary>
        public static async IAsyncEnumerable<AgentResponse> CreateTestAgentResponseStream([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new AgentResponse
            {
                Content = "Starting response",
                Type = ResponseType.Text,
                IsComplete = false
            };

            await Task.Delay(10, cancellationToken);

            yield return new AgentResponse
            {
                Content = " continuing with more content",
                Type = ResponseType.Text,
                IsComplete = false
            };

            await Task.Delay(10, cancellationToken);

            yield return new AgentResponse
            {
                Content = " and completing the response.",
                Type = ResponseType.Text,
                IsComplete = true
            };
        }

        /// <summary>
        /// Creates a test orchestration step completed event
        /// </summary>
        public static OrchestrationStepCompletedEvent CreateTestOrchestrationStepCompletedEvent()
        {
            return new OrchestrationStepCompletedEvent
            {
                SessionId = "test-session",
                Step = new OrchestrationStep
                {
                    AgentId = "agent-1",
                    Task = "Test Step",
                    Order = 1
                },
                Success = true,
                Output = "Step completed successfully",
                ExecutionTime = TimeSpan.FromSeconds(30)
            };
        }

        /// <summary>
        /// Creates a test agent configuration
        /// </summary>
        public static AgentConfiguration CreateTestAgentConfiguration()
        {
            return new AgentConfiguration
            {
                Name = "Test Agent",
                Type = AgentType.Claude,
                SystemPrompt = "You are a test agent",
                MaxTokens = 1000,
                Temperature = 0.7
            };
        }

        /// <summary>
        /// Creates a test message attachment
        /// </summary>
        public static Attachment CreateTestAttachment(string type = "file", string content = "/test/file.txt")
        {
            return new Attachment
            {
                Id = Guid.NewGuid().ToString(),
                FileName = Path.GetFileName(content),
                MimeType = type,
                Size = 1024,
                Content = new byte[0],
                Url = content
            };
        }

        /// <summary>
        /// Creates a test connection info
        /// </summary>
        public static ConnectionInfo CreateTestConnectionInfo(string? connectionId = null)
        {
            return new ConnectionInfo
            {
                ConnectionId = connectionId ?? Guid.NewGuid().ToString(),
                ConnectedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a test error response
        /// </summary>
        public static ErrorResponse CreateTestErrorResponse(string error = "Test error", string? sessionId = null, string? agentId = null)
        {
            return new ErrorResponse
            {
                Error = error,
                SessionId = sessionId,
                AgentId = agentId,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a test agent status DTO
        /// </summary>
        public static AgentStatusDto CreateTestAgentStatusDto(string agentId = "test-agent", AgentStatus status = AgentStatus.Ready)
        {
            return new AgentStatusDto
            {
                AgentId = agentId,
                Status = status,
                Timestamp = DateTime.UtcNow
            };
        }
    }
}