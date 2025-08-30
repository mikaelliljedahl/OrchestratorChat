using Microsoft.Extensions.Logging;
using OrchestratorChat.Core.Exceptions;
using OrchestratorChat.Core.Tools;
using System.Diagnostics;

namespace OrchestratorChat.Agents.Tools;

/// <summary>
/// Executes tools with parameter validation and timeout support
/// </summary>
public class ToolExecutor : IToolExecutor
{
    private readonly Dictionary<string, IToolHandler> _handlers;
    private readonly ILogger<ToolExecutor> _logger;

    public ToolExecutor(ILogger<ToolExecutor> logger)
    {
        _handlers = new Dictionary<string, IToolHandler>(StringComparer.OrdinalIgnoreCase);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ToolExecutionResult> ExecuteAsync(
        string toolName,
        Dictionary<string, object> parameters,
        IExecutionContext context,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = "Tool name cannot be null or empty",
                ExecutionTime = TimeSpan.Zero
            };
        }

        if (!_handlers.TryGetValue(toolName, out var handler))
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = $"No handler found for tool '{toolName}'",
                ExecutionTime = TimeSpan.Zero
            };
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("Executing tool '{ToolName}' with parameters: {@Parameters}", toolName, parameters);

            // Validate parameters
            var validationResult = handler.ValidateParameters(parameters ?? new Dictionary<string, object>());
            if (!validationResult.IsValid)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    Error = $"Parameter validation failed: {string.Join(", ", validationResult.Errors)}",
                    ExecutionTime = stopwatch.Elapsed
                };
            }

            // Execute with timeout if specified
            ToolExecutionResult result;
            if (timeout.HasValue)
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(timeout.Value);

                try
                {
                    result = await handler.ExecuteAsync(parameters ?? new Dictionary<string, object>(), context, timeoutCts.Token);
                }
                catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    result = new ToolExecutionResult
                    {
                        Success = false,
                        Error = $"Tool execution timed out after {timeout.Value.TotalSeconds:F1} seconds",
                        ExecutionTime = stopwatch.Elapsed
                    };
                }
            }
            else
            {
                result = await handler.ExecuteAsync(parameters ?? new Dictionary<string, object>(), context, cancellationToken);
            }

            result.ExecutionTime = stopwatch.Elapsed;

            _logger.LogDebug("Tool '{ToolName}' executed in {ExecutionTime}ms with success: {Success}", 
                toolName, stopwatch.ElapsedMilliseconds, result.Success);

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Tool '{ToolName}' execution was cancelled", toolName);
            return new ToolExecutionResult
            {
                Success = false,
                Error = "Tool execution was cancelled",
                ExecutionTime = stopwatch.Elapsed
            };
        }
        catch (ToolExecutionException ex)
        {
            _logger.LogError(ex, "Tool execution exception for '{ToolName}': {Message}", toolName, ex.Message);
            return new ToolExecutionResult
            {
                Success = false,
                Error = ex.Message,
                ExecutionTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error executing tool '{ToolName}'", toolName);
            return new ToolExecutionResult
            {
                Success = false,
                Error = $"Unexpected error: {ex.Message}",
                ExecutionTime = stopwatch.Elapsed
            };
        }
    }

    /// <inheritdoc />
    public void RegisterHandler(string toolName, IToolHandler handler)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            throw new ArgumentException("Tool name cannot be null or empty", nameof(toolName));
        
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        _handlers[toolName] = handler;
        _logger.LogDebug("Registered handler for tool '{ToolName}'", toolName);
    }

    /// <inheritdoc />
    public IEnumerable<string> GetRegisteredTools()
    {
        return _handlers.Keys.ToList();
    }

    /// <inheritdoc />
    public bool IsToolRegistered(string toolName)
    {
        return !string.IsNullOrWhiteSpace(toolName) && _handlers.ContainsKey(toolName);
    }
}