namespace OrchestratorChat.Core.Tools;

/// <summary>
/// Interface for all tools that can be executed by agents
/// </summary>
public interface ITool
{
    /// <summary>
    /// Unique name of the tool
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Human-readable description of what the tool does
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Schema defining the tool's parameters and structure
    /// </summary>
    ToolSchema Schema { get; }
    
    /// <summary>
    /// Whether this tool requires user approval before execution
    /// </summary>
    bool RequiresApproval { get; }
    
    /// <summary>
    /// Execute the tool with the provided parameters
    /// </summary>
    /// <param name="call">The tool call containing parameters</param>
    /// <param name="context">Execution context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the tool execution</returns>
    Task<ToolExecutionResult> ExecuteAsync(
        ToolCall call, 
        IExecutionContext context,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate a tool call before execution
    /// </summary>
    /// <param name="call">The tool call to validate</param>
    /// <returns>Validation result</returns>
    Task<ValidationResult> ValidateAsync(ToolCall call);
}