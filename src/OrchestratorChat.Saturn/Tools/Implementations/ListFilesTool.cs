using OrchestratorChat.Saturn.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace OrchestratorChat.Saturn.Tools.Implementations;

/// <summary>
/// Tool for listing directory contents with filtering and metadata
/// </summary>
public class ListFilesTool : ToolBase
{
    public override string Name => "list_files";
    public override string Description => "List directory contents with filtering, sorting, and metadata";
    public override bool RequiresApproval => false;

    public override List<ToolParameter> Parameters => new()
    {
        new ToolParameter
        {
            Name = "directory",
            Type = "string",
            Description = "The directory to list (default: current directory)",
            Required = false
        },
        new ToolParameter
        {
            Name = "recursive",
            Type = "boolean",
            Description = "Whether to list files recursively (default: false)",
            Required = false
        },
        new ToolParameter
        {
            Name = "include_hidden",
            Type = "boolean",
            Description = "Whether to include hidden files and directories (default: false)",
            Required = false
        },
        new ToolParameter
        {
            Name = "pattern",
            Type = "string",
            Description = "File name pattern to filter by (supports wildcards * and ?)",
            Required = false
        },
        new ToolParameter
        {
            Name = "extension",
            Type = "string",
            Description = "Filter by file extension (e.g., '.cs', '.txt')",
            Required = false
        },
        new ToolParameter
        {
            Name = "max_depth",
            Type = "number",
            Description = "Maximum depth for recursive listing (default: unlimited)",
            Required = false
        },
        new ToolParameter
        {
            Name = "sort_by",
            Type = "string",
            Description = "Sort files by: name, size, modified, created, type (default: name)",
            Required = false
        },
        new ToolParameter
        {
            Name = "sort_descending",
            Type = "boolean",
            Description = "Sort in descending order (default: false)",
            Required = false
        },
        new ToolParameter
        {
            Name = "max_results",
            Type = "number",
            Description = "Maximum number of results to return (default: 1000)",
            Required = false
        },
        new ToolParameter
        {
            Name = "show_metadata",
            Type = "boolean",
            Description = "Include detailed metadata (size, dates, permissions) (default: true)",
            Required = false
        }
    };

    protected override async Task<ToolExecutionResult> ExecuteInternalAsync(ToolCall call, CancellationToken cancellationToken)
    {
        var directory = call.Parameters.GetValueOrDefault("directory", ".")?.ToString();
        var recursive = bool.TryParse(call.Parameters.GetValueOrDefault("recursive", "false")?.ToString(), out var recursiveValue) && recursiveValue;
        var includeHidden = bool.TryParse(call.Parameters.GetValueOrDefault("include_hidden", "false")?.ToString(), out var includeHiddenValue) && includeHiddenValue;
        var pattern = call.Parameters.GetValueOrDefault("pattern")?.ToString();
        var extension = call.Parameters.GetValueOrDefault("extension")?.ToString();
        var maxDepth = int.TryParse(call.Parameters.GetValueOrDefault("max_depth", "-1")?.ToString(), out var maxDepthValue) ? maxDepthValue : -1;
        var sortBy = call.Parameters.GetValueOrDefault("sort_by", "name")?.ToString()?.ToLowerInvariant() ?? "name";
        var sortDescending = bool.TryParse(call.Parameters.GetValueOrDefault("sort_descending", "false")?.ToString(), out var sortDescValue) && sortDescValue;
        var maxResults = int.TryParse(call.Parameters.GetValueOrDefault("max_results", "1000")?.ToString(), out var maxResultsValue) ? maxResultsValue : 1000;
        var showMetadata = !bool.TryParse(call.Parameters.GetValueOrDefault("show_metadata", "true")?.ToString(), out var showMetadataValue) || showMetadataValue;

        if (string.IsNullOrEmpty(directory))
        {
            directory = ".";
        }

        // Validate directory path for security
        if (!IsValidPath(directory))
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = "Invalid directory path - potential directory traversal attempt"
            };
        }

        try
        {
            if (!Directory.Exists(directory))
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    Error = $"Directory not found: {directory}"
                };
            }

            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var entries = new List<FileSystemInfo>();

            // Get files and directories
            var directoryInfo = new DirectoryInfo(directory);
            
            if (recursive && maxDepth > 0)
            {
                entries.AddRange(GetEntriesWithMaxDepth(directoryInfo, maxDepth, 0));
            }
            else
            {
                entries.AddRange(directoryInfo.GetFileSystemInfos("*", searchOption));
            }

            // Apply filters
            var filteredEntries = entries.AsEnumerable();

            // Filter hidden files
            if (!includeHidden)
            {
                filteredEntries = filteredEntries.Where(e => !e.Name.StartsWith('.') && 
                    (e.Attributes & FileAttributes.Hidden) == 0);
            }

            // Filter by pattern
            if (!string.IsNullOrEmpty(pattern))
            {
                var regex = CreatePatternRegex(pattern);
                filteredEntries = filteredEntries.Where(e => regex.IsMatch(e.Name));
            }

            // Filter by extension
            if (!string.IsNullOrEmpty(extension))
            {
                var ext = extension.StartsWith('.') ? extension : $".{extension}";
                filteredEntries = filteredEntries.Where(e => 
                    e is FileInfo && string.Equals(e.Extension, ext, StringComparison.OrdinalIgnoreCase));
            }

            // Sort entries
            filteredEntries = sortBy switch
            {
                "size" => filteredEntries.OrderBy(e => e is FileInfo f ? f.Length : 0),
                "modified" => filteredEntries.OrderBy(e => e.LastWriteTimeUtc),
                "created" => filteredEntries.OrderBy(e => e.CreationTimeUtc),
                "type" => filteredEntries.OrderBy(e => e is DirectoryInfo ? "0" : e.Extension),
                _ => filteredEntries.OrderBy(e => e.Name)
            };

            if (sortDescending)
            {
                filteredEntries = filteredEntries.Reverse();
            }

            // Apply max results limit
            var results = filteredEntries.Take(maxResults).ToList();

            // Generate output
            var output = new StringBuilder();
            var fileInfoList = new List<object>();
            var totalFiles = 0;
            var totalDirectories = 0;
            var totalSize = 0L;

            foreach (var entry in results)
            {
                var relativePath = Path.GetRelativePath(directory, entry.FullName);
                
                if (showMetadata)
                {
                    var isDirectory = entry is DirectoryInfo;
                    var size = isDirectory ? 0L : ((FileInfo)entry).Length;
                    var permissions = GetPermissionsString(entry);
                    
                    output.AppendLine($"{permissions} {FormatSize(size),10} {entry.LastWriteTimeUtc:yyyy-MM-dd HH:mm:ss} {relativePath}{(isDirectory ? "/" : "")}");
                    
                    fileInfoList.Add(new
                    {
                        name = entry.Name,
                        path = relativePath,
                        full_path = entry.FullName,
                        is_directory = isDirectory,
                        size = size,
                        created = entry.CreationTimeUtc,
                        modified = entry.LastWriteTimeUtc,
                        accessed = entry.LastAccessTimeUtc,
                        attributes = entry.Attributes.ToString(),
                        extension = entry.Extension,
                        is_readonly = (entry.Attributes & FileAttributes.ReadOnly) != 0,
                        is_hidden = (entry.Attributes & FileAttributes.Hidden) != 0
                    });

                    if (isDirectory)
                        totalDirectories++;
                    else
                    {
                        totalFiles++;
                        totalSize += size;
                    }
                }
                else
                {
                    var isDirectory = entry is DirectoryInfo;
                    output.AppendLine($"{relativePath}{(isDirectory ? "/" : "")}");
                    
                    if (isDirectory)
                        totalDirectories++;
                    else
                    {
                        totalFiles++;
                        totalSize += entry is FileInfo f ? f.Length : 0;
                    }
                }
            }

            var hasMore = filteredEntries.Count() > maxResults;

            return new ToolExecutionResult
            {
                Success = true,
                Output = output.ToString().TrimEnd(),
                Metadata = new Dictionary<string, object>
                {
                    ["directory"] = directory,
                    ["total_files"] = totalFiles,
                    ["total_directories"] = totalDirectories,
                    ["total_size"] = totalSize,
                    ["total_entries"] = results.Count,
                    ["has_more"] = hasMore,
                    ["recursive"] = recursive,
                    ["include_hidden"] = includeHidden,
                    ["pattern"] = pattern ?? "",
                    ["extension"] = extension ?? "",
                    ["sort_by"] = sortBy,
                    ["sort_descending"] = sortDescending,
                    ["max_results"] = maxResults,
                    ["file_info"] = fileInfoList
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
        catch (Exception ex)
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = $"Failed to list directory: {ex.Message}"
            };
        }
    }

    private static IEnumerable<FileSystemInfo> GetEntriesWithMaxDepth(DirectoryInfo dir, int maxDepth, int currentDepth)
    {
        if (currentDepth >= maxDepth)
            yield break;

        FileSystemInfo[] entries;
        try
        {
            entries = dir.GetFileSystemInfos();
        }
        catch (UnauthorizedAccessException)
        {
            // Skip directories we can't access
            yield break;
        }

        foreach (var entry in entries)
        {
            yield return entry;

            if (entry is DirectoryInfo subDir && currentDepth + 1 < maxDepth)
            {
                foreach (var subEntry in GetEntriesWithMaxDepth(subDir, maxDepth, currentDepth + 1))
                {
                    yield return subEntry;
                }
            }
        }
    }

    private static Regex CreatePatternRegex(string pattern)
    {
        // Convert wildcard pattern to regex
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        
        return new Regex(regexPattern, RegexOptions.IgnoreCase);
    }

    private static string GetPermissionsString(FileSystemInfo entry)
    {
        var permissions = new char[4];
        
        permissions[0] = entry is DirectoryInfo ? 'd' : '-';
        permissions[1] = (entry.Attributes & FileAttributes.ReadOnly) == 0 ? 'w' : 'r';
        permissions[2] = 'r'; // Always readable if we can see it
        permissions[3] = entry.Extension?.ToLowerInvariant() is ".exe" or ".bat" or ".cmd" or ".ps1" ? 'x' : '-';
        
        return new string(permissions);
    }

    private static string FormatSize(long bytes)
    {
        if (bytes == 0) return "0 B";

        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        var index = 0;
        double size = bytes;

        while (size >= 1024 && index < suffixes.Length - 1)
        {
            size /= 1024;
            index++;
        }

        return $"{size:F1} {suffixes[index]}";
    }

    private static bool IsValidPath(string path)
    {
        try
        {
            // Normalize the path and check for directory traversal
            var fullPath = Path.GetFullPath(path);
            
            // Check for dangerous patterns
            if (path.Contains("..") && !path.StartsWith("./") && !path.StartsWith(".\\"))
            {
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}