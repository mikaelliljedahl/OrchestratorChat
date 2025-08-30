using OrchestratorChat.Core.Tools;

namespace OrchestratorChat.Agents.Tools;

/// <summary>
/// Interface for handling specific tool operations
/// </summary>
public interface IToolHandler
{
    /// <summary>
    /// Name of the tool this handler supports
    /// </summary>
    string ToolName { get; }

    /// <summary>
    /// Execute the tool with the provided parameters
    /// </summary>
    /// <param name="parameters">Parameters for the tool execution</param>
    /// <param name="context">Execution context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the tool execution</returns>
    Task<ToolExecutionResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        IExecutionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate the parameters for this tool
    /// </summary>
    /// <param name="parameters">Parameters to validate</param>
    /// <returns>Validation result</returns>
    ValidationResult ValidateParameters(Dictionary<string, object> parameters);
}