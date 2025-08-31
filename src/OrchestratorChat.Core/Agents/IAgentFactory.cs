using OrchestratorChat.Core.Agents;

namespace OrchestratorChat.Core.Agents;

public interface IAgentFactory
{
    Task<IAgent> CreateAgentAsync(AgentType type, AgentConfiguration configuration);
    List<AgentType> GetSupportedTypes();
    
    /// <summary>
    /// Gets the available agent types that can be created
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Available agent types</returns>
    Task<IEnumerable<AgentType>> GetAvailableAgentTypesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets an existing agent by ID
    /// </summary>
    /// <param name="agentId">Agent identifier</param>
    /// <returns>Agent instance if found, null otherwise</returns>
    Task<IAgent?> GetAgentAsync(string agentId);
    
    /// <summary>
    /// Disposes an agent and removes it from the factory
    /// </summary>
    /// <param name="agentId">Agent identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the disposal operation</returns>
    Task DisposeAgentAsync(string agentId, CancellationToken cancellationToken = default);
}