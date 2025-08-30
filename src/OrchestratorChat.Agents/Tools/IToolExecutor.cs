using OrchestratorChat.Core.Tools;

namespace OrchestratorChat.Agents.Tools;

/// <summary>
/// Interface for executing tools with parameter validation and timeout support
/// </summary>
public interface IToolExecutor
{
    /// <summary>
    /// Execute a tool with the provided parameters
    /// </summary>
    /// <param name="toolName">Name of the tool to execute</param>
    /// <param name="parameters">Parameters to pass to the tool</param>
    /// <param name="context">Execution context</param>
    /// <param name="timeout">Optional timeout for the execution</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the tool execution</returns>
    Task<ToolExecutionResult> ExecuteAsync(
        string toolName,
        Dictionary<string, object> parameters,
        IExecutionContext context,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Register a tool handler
    /// </summary>
    /// <param name="toolName">Name of the tool</param>
    /// <param name="handler">Handler instance</param>
    void RegisterHandler(string toolName, IToolHandler handler);

    /// <summary>
    /// Get all registered tool names
    /// </summary>
    /// <returns>Collection of registered tool names</returns>
    IEnumerable<string> GetRegisteredTools();

    /// <summary>
    /// Check if a tool is registered
    /// </summary>
    /// <param name="toolName">Name of the tool to check</param>
    /// <returns>True if the tool is registered</returns>
    bool IsToolRegistered(string toolName);
}