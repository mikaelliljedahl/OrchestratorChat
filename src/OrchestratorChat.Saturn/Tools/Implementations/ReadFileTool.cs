using OrchestratorChat.Saturn.Models;

namespace OrchestratorChat.Saturn.Tools.Implementations;

/// <summary>
/// Tool for reading files
/// </summary>
public class ReadFileTool : ToolBase
{
    public override string Name => "read_file";
    public override string Description => "Read the contents of a file";
    public override bool RequiresApproval => false;

    public override List<ToolParameter> Parameters => new()
    {
        new ToolParameter
        {
            Name = "file_path",
            Type = "string",
            Description = "The path to the file to read",
            Required = true
        },
        new ToolParameter
        {
            Name = "encoding",
            Type = "string",
            Description = "The encoding to use (default: utf-8)",
            Required = false
        }
    };

    protected override async Task<ToolExecutionResult> ExecuteInternalAsync(ToolCall call, CancellationToken cancellationToken)
    {
        var filePath = call.Parameters["file_path"]?.ToString();
        var encoding = call.Parameters.GetValueOrDefault("encoding", "utf-8")?.ToString();

        if (string.IsNullOrEmpty(filePath))
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = "file_path parameter is required"
            };
        }

        try
        {
            if (!File.Exists(filePath))
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    Error = $"File not found: {filePath}"
                };
            }

            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            
            return new ToolExecutionResult
            {
                Success = true,
                Output = content,
                Metadata = new Dictionary<string, object>
                {
                    ["file_path"] = filePath,
                    ["file_size"] = new FileInfo(filePath).Length,
                    ["encoding"] = encoding ?? "utf-8"
                }
            };
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = $"Failed to read file: {ex.Message}"
            };
        }
    }
}