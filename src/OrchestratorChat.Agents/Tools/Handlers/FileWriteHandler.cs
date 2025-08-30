using Microsoft.Extensions.Logging;
using OrchestratorChat.Core.Exceptions;
using OrchestratorChat.Core.Tools;

namespace OrchestratorChat.Agents.Tools.Handlers;

/// <summary>
/// Handles file write operations
/// </summary>
public class FileWriteHandler : IToolHandler
{
    private readonly ILogger<FileWriteHandler> _logger;

    public FileWriteHandler(ILogger<FileWriteHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string ToolName => "file_write";

    /// <inheritdoc />
    public async Task<ToolExecutionResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = GetRequiredParameter<string>(parameters, "file_path");
            var content = GetRequiredParameter<string>(parameters, "content");
            
            // Optional parameters
            var append = GetOptionalParameter<bool>(parameters, "append", false);
            var createDirectory = GetOptionalParameter<bool>(parameters, "create_directory", true);

            // Convert to absolute path if relative
            if (!Path.IsPathRooted(filePath))
            {
                filePath = Path.Combine(context.WorkingDirectory, filePath);
            }

            _logger.LogDebug("Writing to file: {FilePath} (append: {Append})", filePath, append);

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

            // Create directory if needed
            var directory = Path.GetDirectoryName(normalizedPath);
            if (createDirectory && !string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogDebug("Created directory: {Directory}", directory);
            }

            // Write or append content
            if (append)
            {
                await File.AppendAllTextAsync(normalizedPath, content, cancellationToken);
            }
            else
            {
                await File.WriteAllTextAsync(normalizedPath, content, cancellationToken);
            }

            var fileInfo = new FileInfo(normalizedPath);

            return new ToolExecutionResult
            {
                Success = true,
                Output = $"Successfully {(append ? "appended to" : "wrote")} file: {filePath}",
                Metadata = new Dictionary<string, object>
                {
                    ["file_path"] = normalizedPath,
                    ["file_size"] = fileInfo.Length,
                    ["bytes_written"] = System.Text.Encoding.UTF8.GetByteCount(content),
                    ["operation"] = append ? "append" : "write",
                    ["created_directory"] = createDirectory && !string.IsNullOrEmpty(directory) && Directory.Exists(directory)
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
        catch (DirectoryNotFoundException ex)
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = $"Directory not found: {ex.Message}"
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
            _logger.LogError(ex, "Unexpected error writing file");
            throw new ToolExecutionException($"Failed to write file: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public ValidationResult ValidateParameters(Dictionary<string, object> parameters)
    {
        var errors = new List<string>();

        // Validate file_path
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

        // Validate content
        if (!parameters.ContainsKey("content") || parameters["content"] == null)
        {
            errors.Add("Parameter 'content' is required");
        }
        else if (parameters["content"] is not string)
        {
            errors.Add("Parameter 'content' must be a string");
        }

        // Validate optional boolean parameters
        if (parameters.ContainsKey("append") && parameters["append"] is not bool)
        {
            errors.Add("Parameter 'append' must be a boolean");
        }

        if (parameters.ContainsKey("create_directory") && parameters["create_directory"] is not bool)
        {
            errors.Add("Parameter 'create_directory' must be a boolean");
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

    private static T GetOptionalParameter<T>(Dictionary<string, object> parameters, string paramName, T defaultValue)
    {
        if (!parameters.TryGetValue(paramName, out var value) || value == null)
        {
            return defaultValue;
        }

        if (value is not T typedValue)
        {
            throw new ToolExecutionException($"Parameter '{paramName}' must be of type {typeof(T).Name}");
        }

        return typedValue;
    }
}