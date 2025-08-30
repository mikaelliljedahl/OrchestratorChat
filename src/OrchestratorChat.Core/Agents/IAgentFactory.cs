using OrchestratorChat.Core.Agents;

namespace OrchestratorChat.Core.Agents;

public interface IAgentFactory
{
    Task<IAgent> CreateAgentAsync(AgentType type, AgentConfiguration configuration);
    Task<List<AgentInfo>> GetConfiguredAgents();
    Task<IAgent?> GetAgentAsync(string agentId);
    void RegisterAgent(string agentId, IAgent agent);
    List<AgentType> GetSupportedTypes();
    
    /// <summary>
    /// Gets the available agent types that can be created
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Available agent types</returns>
    Task<IEnumerable<AgentType>> GetAvailableAgentTypesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Disposes a specific agent by ID
    /// </summary>
    /// <param name="agentId">The ID of the agent to dispose</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the disposal operation</returns>
    Task DisposeAgentAsync(string agentId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the status of a specific agent by ID
    /// </summary>
    /// <param name="agentId">The ID of the agent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The agent status</returns>
    Task<AgentStatus> GetAgentStatusAsync(string agentId, CancellationToken cancellationToken = default);
}