namespace OrchestratorChat.Core.Agents;

/// <summary>
/// Registry for managing active agent instances
/// </summary>
public interface IAgentRegistry
{
    /// <summary>
    /// Find an active agent by ID
    /// </summary>
    /// <param name="agentId">Agent identifier</param>
    /// <returns>Agent instance if found, null otherwise</returns>
    Task<IAgent?> FindAsync(string agentId);

    /// <summary>
    /// Get an existing agent or create a new one using the provided factory
    /// </summary>
    /// <param name="agentId">Agent identifier</param>
    /// <param name="factory">Factory function to create agent type and configuration</param>
    /// <returns>Agent instance</returns>
    Task<IAgent> GetOrCreateAsync(string agentId, Func<Task<(AgentType type, AgentConfiguration cfg)>> factory);

    /// <summary>
    /// Remove an agent from the registry and dispose it properly
    /// </summary>
    /// <param name="agentId">Agent identifier</param>
    /// <returns>Task representing the removal operation</returns>
    Task RemoveAsync(string agentId);
}