using Moq;
using OrchestratorChat.Core.Agents;
using System.Collections.Concurrent;

namespace OrchestratorChat.SignalR.IntegrationTests.Helpers
{
    /// <summary>
    /// Mock implementation of IAgentFactory for testing
    /// </summary>
    public class MockAgentFactory : Mock<IAgentFactory>
    {
        private readonly ConcurrentDictionary<string, IAgent> _createdAgents = new();
        private readonly ConcurrentDictionary<string, Func<IAgent>> _agentFactories = new();

        public MockAgentFactory()
        {
            // Setup default behavior
            Setup(x => x.CreateAgentAsync(It.IsAny<AgentType>(), It.IsAny<AgentConfiguration>()))
                .ReturnsAsync((AgentType type, AgentConfiguration config) =>
                {
                    var agentId = config.Id ?? Guid.NewGuid().ToString();
                    
                    if (_createdAgents.TryGetValue(agentId, out var existingAgent))
                    {
                        return existingAgent;
                    }

                    if (_agentFactories.TryGetValue(agentId, out var factory))
                    {
                        var agent = factory();
                        _createdAgents[agentId] = agent;
                        return agent;
                    }

                    // Create default mock agent
                    var mockAgent = CreateDefaultMockAgent(agentId, type);
                    _createdAgents[agentId] = mockAgent;
                    return mockAgent;
                });

            Setup(x => x.GetAgentAsync(It.IsAny<string>()))
                .ReturnsAsync((string agentId) =>
                {
                    _createdAgents.TryGetValue(agentId, out var agent);
                    return agent;
                });

            Setup(x => x.GetAvailableAgentTypesAsync())
                .ReturnsAsync(new List<AgentType> { AgentType.Claude, AgentType.GPT4, AgentType.Local });

            Setup(x => x.DisposeAgentAsync(It.IsAny<string>()))
                .Returns((string agentId) =>
                {
                    _createdAgents.TryRemove(agentId, out _);
                    return Task.CompletedTask;
                });
        }

        /// <summary>
        /// Sets up the factory to return a specific agent for a given agent ID
        /// </summary>
        /// <param name="agentId">Agent ID</param>
        /// <param name="agent">Agent instance to return</param>
        public void SetupCreateAgentAsync(string agentId, IAgent agent)
        {
            _agentFactories[agentId] = () => agent;
        }

        /// <summary>
        /// Sets up the factory to use a factory function for creating agents with a specific ID
        /// </summary>
        /// <param name="agentId">Agent ID</param>
        /// <param name="agentFactory">Factory function</param>
        public void SetupCreateAgentAsync(string agentId, Func<IAgent> agentFactory)
        {
            _agentFactories[agentId] = agentFactory;
        }

        /// <summary>
        /// Gets all created agents
        /// </summary>
        /// <returns>Dictionary of created agents by ID</returns>
        public Dictionary<string, IAgent> GetCreatedAgents()
        {
            return _createdAgents.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>
        /// Clears all created agents and factories
        /// </summary>
        public void Clear()
        {
            _createdAgents.Clear();
            _agentFactories.Clear();
        }

        /// <summary>
        /// Gets the count of created agents
        /// </summary>
        public int CreatedAgentCount => _createdAgents.Count;

        /// <summary>
        /// Checks if an agent with the specified ID has been created
        /// </summary>
        /// <param name="agentId">Agent ID to check</param>
        /// <returns>True if agent exists</returns>
        public bool HasAgent(string agentId)
        {
            return _createdAgents.ContainsKey(agentId);
        }

        private static IAgent CreateDefaultMockAgent(string agentId, AgentType type)
        {
            var mockAgent = new Mock<IAgent>();

            mockAgent.SetupGet(x => x.Id).Returns(agentId);
            mockAgent.SetupGet(x => x.Type).Returns(type);
            mockAgent.SetupGet(x => x.Status).Returns(AgentStatus.Ready);
            mockAgent.SetupGet(x => x.Capabilities).Returns(new List<string> { "chat", "code" });
            
            mockAgent.Setup(x => x.SendMessageAsync(It.IsAny<Core.Messages.AgentMessage>()))
                .Returns(CreateDefaultResponseStream());

            mockAgent.Setup(x => x.ExecuteToolAsync(It.IsAny<Core.Tools.ToolCall>()))
                .ReturnsAsync(new Core.Tools.ToolExecutionResult
                {
                    Success = true,
                    Output = "Mock tool execution result",
                    ExecutionTime = TimeSpan.FromMilliseconds(100)
                });

            // Setup status changed event
            mockAgent.SetupAdd(x => x.StatusChanged += It.IsAny<EventHandler<AgentStatusChangedEventArgs>>());
            mockAgent.SetupRemove(x => x.StatusChanged -= It.IsAny<EventHandler<AgentStatusChangedEventArgs>>());

            return mockAgent.Object;
        }

        private static async IAsyncEnumerable<Core.Messages.AgentResponse> CreateDefaultResponseStream()
        {
            yield return new Core.Messages.AgentResponse
            {
                Content = "Mock response part 1",
                Type = Core.Messages.ResponseType.Text,
                IsComplete = false
            };

            await Task.Delay(10);

            yield return new Core.Messages.AgentResponse
            {
                Content = " Mock response part 2",
                Type = Core.Messages.ResponseType.Text,
                IsComplete = false
            };

            await Task.Delay(10);

            yield return new Core.Messages.AgentResponse
            {
                Content = " Final part",
                Type = Core.Messages.ResponseType.Text,
                IsComplete = true
            };
        }
    }
}