using Microsoft.Extensions.Logging;
using OrchestratorChat.Core.Exceptions;
using OrchestratorChat.Core.Tools;

namespace OrchestratorChat.Agents.Tools.Handlers;

/// <summary>
/// Handles file read operations
/// </summary>
public class FileReadHandler : IToolHandler
{
    private readonly ILogger<FileReadHandler> _logger;

    public FileReadHandler(ILogger<FileReadHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string ToolName => "file_read";

    /// <inheritdoc />
    public async Task<ToolExecutionResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = GetRequiredParameter<string>(parameters, "file_path");
            
            // Convert to absolute path if relative
            if (!Path.IsPathRooted(filePath))
            {
                filePath = Path.Combine(context.WorkingDirectory, filePath);
            }

            _logger.LogDebug("Reading file: {FilePath}", filePath);

            // Security check - ensure the file is within allowed directories
            var normalizedPath = Path.GetFullPath(filePath);
            var workingDirPath = Path.GetFullPath(context.WorkingDirectory);
            
            if (!normalizedPath.StartsWith(workingDirPath, StringComparison.OrdinalIgnoreCase))
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    Error = "Access denied: File path is outside the working directory"
                };
            }

            if (!File.Exists(normalizedPath))
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    Error = $"File not found: {filePath}"
                };
            }

            var content = await File.ReadAllTextAsync(normalizedPath, cancellationToken);
            var fileInfo = new FileInfo(normalizedPath);

            return new ToolExecutionResult
            {
                Success = true,
                Output = content,
                Metadata = new Dictionary<string, object>
                {
                    ["file_path"] = normalizedPath,
                    ["file_size"] = fileInfo.Length,
                    ["last_modified"] = fileInfo.LastWriteTimeUtc,
                    ["line_count"] = content.Split('\n').Length
                }
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = $"Access denied: {ex.Message}"
            };
        }
        catch (IOException ex)
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = $"IO error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error reading file");
            throw new ToolExecutionException($"Failed to read file: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public ValidationResult ValidateParameters(Dictionary<string, object> parameters)
    {
        var errors = new List<string>();

        if (!parameters.ContainsKey("file_path") || parameters["file_path"] == null)
        {
            errors.Add("Parameter 'file_path' is required");
        }
        else if (parameters["file_path"] is not string filePath || string.IsNullOrWhiteSpace(filePath))
        {
            errors.Add("Parameter 'file_path' must be a non-empty string");
        }
        else
        {
            // Basic path validation
            if (filePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                errors.Add("Parameter 'file_path' contains invalid characters");
            }
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors.ToArray());
    }

    private static T GetRequiredParameter<T>(Dictionary<string, object> parameters, string paramName)
    {
        if (!parameters.TryGetValue(paramName, out var value) || value == null)
        {
            throw new ToolExecutionException($"Required parameter '{paramName}' is missing");
        }

        if (value is not T typedValue)
        {
            throw new ToolExecutionException($"Parameter '{paramName}' must be of type {typeof(T).Name}");
        }

        return typedValue;
    }
}