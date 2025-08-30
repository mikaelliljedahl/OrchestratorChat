using OrchestratorChat.Saturn.Models;

namespace OrchestratorChat.Saturn.Tools.Implementations;

/// <summary>
/// Tool for safely deleting files and directories with confirmation
/// </summary>
public class DeleteFileTool : ToolBase
{
    public override string Name => "delete_file";
    public override string Description => "Safely delete files or directories with confirmation";
    public override bool RequiresApproval => true;

    public override List<ToolParameter> Parameters => new()
    {
        new ToolParameter
        {
            Name = "file_path",
            Type = "string",
            Description = "The path to the file or directory to delete",
            Required = true
        },
        new ToolParameter
        {
            Name = "recursive",
            Type = "boolean",
            Description = "Whether to delete directories recursively (default: false)",
            Required = false
        },
        new ToolParameter
        {
            Name = "force",
            Type = "boolean",
            Description = "Whether to force deletion of read-only files (default: false)",
            Required = false
        },
        new ToolParameter
        {
            Name = "create_backup",
            Type = "boolean",
            Description = "Whether to create a backup before deletion (default: false)",
            Required = false
        }
    };

    protected override async Task<ToolExecutionResult> ExecuteInternalAsync(ToolCall call, CancellationToken cancellationToken)
    {
        var filePath = call.Parameters["file_path"]?.ToString();
        var recursive = bool.TryParse(call.Parameters.GetValueOrDefault("recursive", "false")?.ToString(), out var recursiveValue) && recursiveValue;
        var force = bool.TryParse(call.Parameters.GetValueOrDefault("force", "false")?.ToString(), out var forceValue) && forceValue;
        var createBackup = bool.TryParse(call.Parameters.GetValueOrDefault("create_backup", "false")?.ToString(), out var backupValue) && backupValue;

        if (string.IsNullOrEmpty(filePath))
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = "file_path parameter is required"
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
            // Check if path exists
            var isFile = File.Exists(filePath);
            var isDirectory = Directory.Exists(filePath);

            if (!isFile && !isDirectory)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    Error = $"Path not found: {filePath}"
                };
            }

            var metadata = new Dictionary<string, object>
            {
                ["file_path"] = filePath,
                ["is_directory"] = isDirectory,
                ["recursive"] = recursive,
                ["force"] = force,
                ["create_backup"] = createBackup
            };

            // Get information about what will be deleted
            var itemsToDelete = new List<string>();
            if (isFile)
            {
                itemsToDelete.Add(filePath);
                var fileInfo = new FileInfo(filePath);
                metadata["file_size"] = fileInfo.Length;
                metadata["is_readonly"] = fileInfo.IsReadOnly;
            }
            else if (isDirectory)
            {
                if (!recursive)
                {
                    // Check if directory is empty
                    var hasFiles = Directory.EnumerateFileSystemEntries(filePath).Any();
                    if (hasFiles)
                    {
                        return new ToolExecutionResult
                        {
                            Success = false,
                            Error = "Directory is not empty. Use recursive=true to delete non-empty directories."
                        };
                    }
                }

                // Get all items that will be deleted
                itemsToDelete.AddRange(Directory.EnumerateFiles(filePath, "*", SearchOption.AllDirectories));
                itemsToDelete.AddRange(Directory.EnumerateDirectories(filePath, "*", SearchOption.AllDirectories));
                itemsToDelete.Add(filePath);

                metadata["total_files"] = Directory.EnumerateFiles(filePath, "*", SearchOption.AllDirectories).Count();
                metadata["total_directories"] = Directory.EnumerateDirectories(filePath, "*", SearchOption.AllDirectories).Count() + 1;
            }

            // Create backup if requested
            var backupPath = string.Empty;
            if (createBackup && isFile)
            {
                backupPath = $"{filePath}.backup.{DateTime.UtcNow:yyyyMMddHHmmss}";
                File.Copy(filePath, backupPath);
                metadata["backup_path"] = backupPath;
            }
            else if (createBackup && isDirectory)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    Error = "Backup creation for directories is not supported. Please use a separate backup tool."
                };
            }

            // Perform deletion
            if (isFile)
            {
                if (force)
                {
                    var fileInfo = new FileInfo(filePath);
                    if (fileInfo.IsReadOnly)
                    {
                        fileInfo.IsReadOnly = false;
                    }
                }

                File.Delete(filePath);
            }
            else if (isDirectory)
            {
                if (force)
                {
                    // Remove readonly attribute from all files if force is enabled
                    foreach (var file in Directory.EnumerateFiles(filePath, "*", SearchOption.AllDirectories))
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.IsReadOnly)
                        {
                            fileInfo.IsReadOnly = false;
                        }
                    }
                }

                Directory.Delete(filePath, recursive);
            }

            var output = isFile 
                ? $"Successfully deleted file: {filePath}"
                : $"Successfully deleted directory: {filePath}";

            if (createBackup && !string.IsNullOrEmpty(backupPath))
            {
                output += $"\nBackup created at: {backupPath}";
            }

            return new ToolExecutionResult
            {
                Success = true,
                Output = output,
                Metadata = metadata
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = $"Access denied: {ex.Message}. Try using force=true for read-only files."
            };
        }
        catch (DirectoryNotEmptyException ex)
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = $"Directory not empty: {ex.Message}. Use recursive=true to delete non-empty directories."
            };
        }
        catch (IOException ex)
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = $"IO error during deletion: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = $"Failed to delete: {ex.Message}"
            };
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
            // and doesn't contain dangerous patterns
            if (path.Contains("..") || path.Contains("~"))
            {
                return false;
            }

            // Additional security checks for critical system paths
            var criticalPaths = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };

            foreach (var criticalPath in criticalPaths.Where(p => !string.IsNullOrEmpty(p)))
            {
                if (fullPath.StartsWith(criticalPath, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}