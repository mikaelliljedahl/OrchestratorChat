using Microsoft.Extensions.Logging;
using OrchestratorChat.Core.Exceptions;
using OrchestratorChat.Core.Tools;
using System.Diagnostics;
using System.Text;

namespace OrchestratorChat.Agents.Tools.Handlers;

/// <summary>
/// Handles bash/shell command execution
/// </summary>
public class BashCommandHandler : IToolHandler
{
    private readonly ILogger<BashCommandHandler> _logger;

    public BashCommandHandler(ILogger<BashCommandHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string ToolName => "bash_command";

    /// <inheritdoc />
    public async Task<ToolExecutionResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = GetRequiredParameter<string>(parameters, "command");
            
            // Optional parameters
            var workingDirectory = GetOptionalParameter<string>(parameters, "working_directory", context.WorkingDirectory);
            var timeout = GetOptionalParameter<int>(parameters, "timeout_seconds", 30);
            var shell = GetOptionalParameter<string>(parameters, "shell", GetDefaultShell());

            _logger.LogDebug("Executing command: {Command} in {WorkingDirectory}", command, workingDirectory);

            // Security validation
            if (ContainsDangerousCommands(command))
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    Error = "Command contains potentially dangerous operations and is not allowed"
                };
            }

            // Ensure working directory exists and is accessible
            if (!Directory.Exists(workingDirectory))
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    Error = $"Working directory does not exist: {workingDirectory}"
                };
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = shell,
                Arguments = GetShellArguments(shell, command),
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            using var process = new Process { StartInfo = processStartInfo };
            
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    outputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    errorBuilder.AppendLine(e.Data);
            };

            var stopwatch = Stopwatch.StartNew();
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var timeoutMs = timeout * 1000;
            
            try
            {
                await process.WaitForExitAsync(cancellationToken).WaitAsync(TimeSpan.FromMilliseconds(timeoutMs), cancellationToken);
            }
            catch (TimeoutException)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (Exception killEx)
                {
                    _logger.LogWarning(killEx, "Failed to kill process after timeout");
                }

                return new ToolExecutionResult
                {
                    Success = false,
                    Error = $"Command timed out after {timeout} seconds",
                    ExecutionTime = stopwatch.Elapsed
                };
            }

            var exitCode = process.ExitCode;
            var stdout = outputBuilder.ToString().Trim();
            var stderr = errorBuilder.ToString().Trim();

            _logger.LogDebug("Command completed with exit code {ExitCode}", exitCode);

            return new ToolExecutionResult
            {
                Success = exitCode == 0,
                Output = string.IsNullOrEmpty(stdout) ? stderr : stdout,
                Error = exitCode != 0 ? $"Command failed with exit code {exitCode}: {stderr}" : null,
                ExecutionTime = stopwatch.Elapsed,
                Metadata = new Dictionary<string, object>
                {
                    ["exit_code"] = exitCode,
                    ["stdout"] = stdout,
                    ["stderr"] = stderr,
                    ["working_directory"] = workingDirectory,
                    ["shell"] = shell,
                    ["command"] = command
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error executing command");
            throw new ToolExecutionException($"Failed to execute command: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public ValidationResult ValidateParameters(Dictionary<string, object> parameters)
    {
        var errors = new List<string>();

        // Validate command
        if (!parameters.ContainsKey("command") || parameters["command"] == null)
        {
            errors.Add("Parameter 'command' is required");
        }
        else if (parameters["command"] is not string command || string.IsNullOrWhiteSpace(command))
        {
            errors.Add("Parameter 'command' must be a non-empty string");
        }

        // Validate optional parameters
        if (parameters.ContainsKey("working_directory") && 
            parameters["working_directory"] is not null and not string)
        {
            errors.Add("Parameter 'working_directory' must be a string");
        }

        if (parameters.ContainsKey("timeout_seconds"))
        {
            if (parameters["timeout_seconds"] is not int timeout || timeout <= 0)
            {
                errors.Add("Parameter 'timeout_seconds' must be a positive integer");
            }
            else if (timeout > 300) // 5 minutes max
            {
                errors.Add("Parameter 'timeout_seconds' cannot exceed 300 seconds");
            }
        }

        if (parameters.ContainsKey("shell") && 
            parameters["shell"] is not null and not string)
        {
            errors.Add("Parameter 'shell' must be a string");
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors.ToArray());
    }

    private static string GetDefaultShell()
    {
        return Environment.OSVersion.Platform switch
        {
            PlatformID.Win32NT => "cmd.exe",
            _ => "/bin/bash"
        };
    }

    private static string GetShellArguments(string shell, string command)
    {
        return shell.EndsWith("cmd.exe", StringComparison.OrdinalIgnoreCase) 
            ? $"/c {command}"
            : $"-c \"{command.Replace("\"", "\\\"")}\"";
    }

    private static bool ContainsDangerousCommands(string command)
    {
        var dangerousPatterns = new[]
        {
            "rm -rf /",
            "rmdir /s",
            "format ",
            "del /f /s /q",
            "shutdown",
            "reboot",
            "halt",
            "init 0",
            "init 6",
            "sudo rm",
            "sudo rmdir",
            "sudo format"
        };

        var lowerCommand = command.ToLowerInvariant();
        return dangerousPatterns.Any(pattern => lowerCommand.Contains(pattern.ToLowerInvariant()));
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