namespace OrchestratorChat.Core.Orchestration;

/// <summary>
/// Defines the strategy for orchestrating multiple agents
/// </summary>
public enum OrchestrationStrategy
{
    /// <summary>
    /// Execute agents one after another in sequence
    /// </summary>
    Sequential,
    
    /// <summary>
    /// Execute agents concurrently where possible
    /// </summary>
    Parallel,
    
    /// <summary>
    /// Dynamically adapt execution based on results
    /// </summary>
    Adaptive,
    
    /// <summary>
    /// Round-robin execution among agents
    /// </summary>
    RoundRobin
}