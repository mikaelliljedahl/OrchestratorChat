using OrchestratorChat.Saturn.Models;

namespace OrchestratorChat.Saturn.Tools;

/// <summary>
/// Interface for all Saturn tools
/// </summary>
public interface ITool
{
    string Name { get; }
    string Description { get; }
    bool RequiresApproval { get; }
    List<ToolParameter> Parameters { get; }

    Task<ToolExecutionResult> ExecuteAsync(ToolCall call, CancellationToken cancellationToken = default);
    Task<bool> ValidateParametersAsync(Dictionary<string, object> parameters);
}

/// <summary>
/// Base class for tool implementations
/// </summary>
public abstract class ToolBase : ITool
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public virtual bool RequiresApproval => false;
    public abstract List<ToolParameter> Parameters { get; }

    public delegate Task<bool> ApprovalCallback(ToolCall call);
    public ApprovalCallback? OnApprovalRequired { get; set; }

    public async Task<ToolExecutionResult> ExecuteAsync(ToolCall call, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate parameters
            if (!await ValidateParametersAsync(call.Parameters))
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    Error = "Invalid parameters provided"
                };
            }

            // Check for approval if required
            if (RequiresApproval && OnApprovalRequired != null)
            {
                var approved = await OnApprovalRequired(call);
                if (!approved)
                {
                    return new ToolExecutionResult
                    {
                        Success = false,
                        Error = "Tool execution was not approved"
                    };
                }
            }

            // Execute the tool
            var startTime = DateTime.UtcNow;
            var result = await ExecuteInternalAsync(call, cancellationToken);
            var endTime = DateTime.UtcNow;
            
            result.ExecutionTime = endTime - startTime;
            return result;
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    protected abstract Task<ToolExecutionResult> ExecuteInternalAsync(ToolCall call, CancellationToken cancellationToken);

    public virtual async Task<bool> ValidateParametersAsync(Dictionary<string, object> parameters)
    {
        // Basic validation - ensure all required parameters are present
        var requiredParams = Parameters.Where(p => p.Required).ToList();
        foreach (var param in requiredParams)
        {
            if (!parameters.ContainsKey(param.Name))
            {
                return false;
            }
        }
        return true;
    }
}