using OrchestratorChat.Saturn.Models;

namespace OrchestratorChat.Saturn.Tools.Implementations;

/// <summary>
/// Tool for writing files
/// </summary>
public class WriteFileTool : ToolBase
{
    public override string Name => "write_file";
    public override string Description => "Write content to a file";
    public override bool RequiresApproval => true;

    public override List<ToolParameter> Parameters => new()
    {
        new ToolParameter
        {
            Name = "file_path",
            Type = "string",
            Description = "The path to the file to write",
            Required = true
        },
        new ToolParameter
        {
            Name = "content",
            Type = "string",
            Description = "The content to write to the file",
            Required = true
        },
        new ToolParameter
        {
            Name = "encoding",
            Type = "string",
            Description = "The encoding to use (default: utf-8)",
            Required = false
        },
        new ToolParameter
        {
            Name = "append",
            Type = "boolean",
            Description = "Whether to append to the file instead of overwriting (default: false)",
            Required = false
        }
    };

    protected override async Task<ToolExecutionResult> ExecuteInternalAsync(ToolCall call, CancellationToken cancellationToken)
    {
        var filePath = call.Parameters["file_path"]?.ToString();
        var content = call.Parameters["content"]?.ToString();
        var encoding = call.Parameters.GetValueOrDefault("encoding", "utf-8")?.ToString();
        var append = bool.TryParse(call.Parameters.GetValueOrDefault("append", "false")?.ToString(), out var appendValue) && appendValue;

        if (string.IsNullOrEmpty(filePath))
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = "file_path parameter is required"
            };
        }

        if (content == null)
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = "content parameter is required"
            };
        }

        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (append)
            {
                await File.AppendAllTextAsync(filePath, content, cancellationToken);
            }
            else
            {
                await File.WriteAllTextAsync(filePath, content, cancellationToken);
            }

            return new ToolExecutionResult
            {
                Success = true,
                Output = $"Successfully {(append ? "appended to" : "wrote")} file: {filePath}",
                Metadata = new Dictionary<string, object>
                {
                    ["file_path"] = filePath,
                    ["content_length"] = content.Length,
                    ["encoding"] = encoding ?? "utf-8",
                    ["append"] = append
                }
            };
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = $"Failed to write file: {ex.Message}"
            };
        }
    }
}