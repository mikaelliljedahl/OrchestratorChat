using OrchestratorChat.Saturn.Models;
using System.Diagnostics;
using System.Text;

namespace OrchestratorChat.Saturn.Tools.Implementations;

/// <summary>
/// Tool for executing bash/shell commands
/// </summary>
public class BashTool : ToolBase
{
    public override string Name => "bash";
    public override string Description => "Execute shell/bash commands";
    public override bool RequiresApproval => true;

    public override List<ToolParameter> Parameters => new()
    {
        new ToolParameter
        {
            Name = "command",
            Type = "string",
            Description = "The command to execute",
            Required = true
        },
        new ToolParameter
        {
            Name = "working_directory",
            Type = "string",
            Description = "The working directory for the command",
            Required = false
        },
        new ToolParameter
        {
            Name = "timeout_seconds",
            Type = "number",
            Description = "Timeout in seconds (default: 30)",
            Required = false
        }
    };

    protected override async Task<ToolExecutionResult> ExecuteInternalAsync(ToolCall call, CancellationToken cancellationToken)
    {
        var command = call.Parameters["command"]?.ToString();
        var workingDirectory = call.Parameters.GetValueOrDefault("working_directory")?.ToString();
        var timeoutSeconds = int.TryParse(call.Parameters.GetValueOrDefault("timeout_seconds", "30")?.ToString(), out var timeout) ? timeout : 30;

        if (string.IsNullOrEmpty(command))
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = "command parameter is required"
            };
        }

        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "cmd" : "bash",
                Arguments = OperatingSystem.IsWindows() ? $"/c {command}" : $"-c \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            if (!string.IsNullOrEmpty(workingDirectory) && Directory.Exists(workingDirectory))
            {
                processInfo.WorkingDirectory = workingDirectory;
            }

            using var process = new Process { StartInfo = processInfo };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var processTask = process.WaitForExitAsync(cancellationToken);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), cancellationToken);

            var completedTask = await Task.WhenAny(processTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }

                return new ToolExecutionResult
                {
                    Success = false,
                    Error = $"Command timed out after {timeoutSeconds} seconds"
                };
            }

            var output = outputBuilder.ToString().Trim();
            var error = errorBuilder.ToString().Trim();
            var exitCode = process.ExitCode;

            return new ToolExecutionResult
            {
                Success = exitCode == 0,
                Output = output,
                Error = error,
                Metadata = new Dictionary<string, object>
                {
                    ["command"] = command,
                    ["exit_code"] = exitCode,
                    ["working_directory"] = workingDirectory ?? Environment.CurrentDirectory,
                    ["timeout_seconds"] = timeoutSeconds
                }
            };
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = $"Failed to execute command: {ex.Message}"
            };
        }
    }
}