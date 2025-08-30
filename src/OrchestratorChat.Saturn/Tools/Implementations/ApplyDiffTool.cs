using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using OrchestratorChat.Saturn.Models;
using System.Text;

namespace OrchestratorChat.Saturn.Tools.Implementations;

/// <summary>
/// Tool for applying unified diff patches to files
/// </summary>
public class ApplyDiffTool : ToolBase
{
    public override string Name => "apply_diff";
    public override string Description => "Apply a unified diff patch to a file";
    public override bool RequiresApproval => true;

    public override List<ToolParameter> Parameters => new()
    {
        new ToolParameter
        {
            Name = "file_path",
            Type = "string",
            Description = "The path to the file to patch",
            Required = true
        },
        new ToolParameter
        {
            Name = "diff",
            Type = "string",
            Description = "The unified diff content to apply",
            Required = true
        },
        new ToolParameter
        {
            Name = "validate",
            Type = "boolean",
            Description = "Whether to validate the result after applying (default: true)",
            Required = false
        },
        new ToolParameter
        {
            Name = "create_backup",
            Type = "boolean",
            Description = "Whether to create a backup of the original file (default: true)",
            Required = false
        }
    };

    protected override async Task<ToolExecutionResult> ExecuteInternalAsync(ToolCall call, CancellationToken cancellationToken)
    {
        var filePath = call.Parameters["file_path"]?.ToString();
        var diff = call.Parameters["diff"]?.ToString();
        var validate = !bool.TryParse(call.Parameters.GetValueOrDefault("validate", "true")?.ToString(), out var validateValue) || validateValue;
        var createBackup = !bool.TryParse(call.Parameters.GetValueOrDefault("create_backup", "true")?.ToString(), out var backupValue) || backupValue;

        if (string.IsNullOrEmpty(filePath))
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = "file_path parameter is required"
            };
        }

        if (string.IsNullOrEmpty(diff))
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = "diff parameter is required"
            };
        }

        // Validate file path for security
        if (!IsValidPath(filePath))
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = "Invalid file path - potential directory traversal attempt"
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

            var originalContent = await File.ReadAllTextAsync(filePath, cancellationToken);

            // Create backup if requested
            var backupPath = string.Empty;
            if (createBackup)
            {
                backupPath = $"{filePath}.backup.{DateTime.UtcNow:yyyyMMddHHmmss}";
                await File.WriteAllTextAsync(backupPath, originalContent, cancellationToken);
            }

            // Parse and apply the unified diff
            var patchedContent = await ApplyUnifiedDiffAsync(originalContent, diff);
            
            if (patchedContent == null)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    Error = "Failed to parse or apply the unified diff"
                };
            }

            // Write the patched content
            await File.WriteAllTextAsync(filePath, patchedContent, cancellationToken);

            var result = new ToolExecutionResult
            {
                Success = true,
                Output = $"Successfully applied diff to {filePath}",
                Metadata = new Dictionary<string, object>
                {
                    ["file_path"] = filePath,
                    ["backup_created"] = createBackup,
                    ["original_length"] = originalContent.Length,
                    ["patched_length"] = patchedContent.Length
                }
            };

            if (createBackup)
            {
                result.Metadata["backup_path"] = backupPath;
            }

            // Validate result if requested
            if (validate)
            {
                var validationResult = await ValidatePatchedContentAsync(originalContent, patchedContent, diff);
                result.Metadata["validation"] = validationResult;
                
                if (!validationResult)
                {
                    result.Output += " (Warning: Validation failed - the patch may not have been applied correctly)";
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = $"Failed to apply diff: {ex.Message}"
            };
        }
    }

    private async Task<string?> ApplyUnifiedDiffAsync(string originalContent, string diffContent)
    {
        try
        {
            // Parse unified diff manually since DiffPlex doesn't have built-in unified diff application
            var lines = diffContent.Split('\n');
            var originalLines = originalContent.Split('\n').ToList();
            var result = new List<string>(originalLines);
            
            var currentIndex = 0;
            var lineOffset = 0;

            foreach (var line in lines)
            {
                var trimmedLine = line.TrimEnd('\r');
                
                // Skip header lines
                if (trimmedLine.StartsWith("---") || trimmedLine.StartsWith("+++") || 
                    trimmedLine.StartsWith("diff ") || trimmedLine.StartsWith("index "))
                {
                    continue;
                }

                // Parse hunk header
                if (trimmedLine.StartsWith("@@"))
                {
                    var parts = trimmedLine.Split(' ');
                    if (parts.Length >= 3 && parts[1].StartsWith("-"))
                    {
                        var oldRange = parts[1][1..].Split(',');
                        if (oldRange.Length > 0 && int.TryParse(oldRange[0], out var startLine))
                        {
                            currentIndex = Math.Max(0, startLine - 1 + lineOffset); // Convert to 0-based index
                        }
                    }
                    continue;
                }

                // Apply changes
                if (trimmedLine.StartsWith("-"))
                {
                    // Remove line
                    var lineContent = trimmedLine[1..];
                    if (currentIndex < result.Count && result[currentIndex].TrimEnd() == lineContent.TrimEnd())
                    {
                        result.RemoveAt(currentIndex);
                        lineOffset--;
                    }
                    else
                    {
                        // Try to find the line nearby
                        var found = false;
                        for (var i = Math.Max(0, currentIndex - 2); i < Math.Min(result.Count, currentIndex + 3); i++)
                        {
                            if (result[i].TrimEnd() == lineContent.TrimEnd())
                            {
                                result.RemoveAt(i);
                                lineOffset--;
                                currentIndex = i;
                                found = true;
                                break;
                            }
                        }
                        
                        if (!found)
                        {
                            // Line not found - this might be a conflict
                            return null;
                        }
                    }
                }
                else if (trimmedLine.StartsWith("+"))
                {
                    // Add line
                    var lineContent = trimmedLine[1..];
                    result.Insert(currentIndex, lineContent);
                    currentIndex++;
                    lineOffset++;
                }
                else if (trimmedLine.StartsWith(" "))
                {
                    // Context line - advance index
                    currentIndex++;
                }
            }

            return string.Join("\n", result);
        }
        catch
        {
            return null;
        }
    }

    private async Task<bool> ValidatePatchedContentAsync(string originalContent, string patchedContent, string diff)
    {
        try
        {
            // Use DiffPlex to generate a diff between original and patched content
            var diffBuilder = new InlineDiffBuilder(new Differ());
            var diffResult = diffBuilder.BuildDiffModel(originalContent, patchedContent);
            
            // Check if the generated diff is reasonable (not perfect validation, but a sanity check)
            var hasChanges = diffResult.Lines.Any(line => line.Type != ChangeType.Unchanged);
            return hasChanges; // If there are changes, we assume the patch was applied
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidPath(string path)
    {
        try
        {
            // Normalize the path and check for directory traversal
            var fullPath = Path.GetFullPath(path);
            var currentDir = Directory.GetCurrentDirectory();
            
            // Ensure the path is within or relative to the current directory
            return fullPath.StartsWith(currentDir, StringComparison.OrdinalIgnoreCase) ||
                   !Path.IsPathRooted(path);
        }
        catch
        {
            return false;
        }
    }
}