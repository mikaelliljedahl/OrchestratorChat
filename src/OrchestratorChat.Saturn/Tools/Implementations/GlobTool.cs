using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using OrchestratorChat.Saturn.Models;

namespace OrchestratorChat.Saturn.Tools.Implementations;

/// <summary>
/// Tool for finding files using glob patterns
/// </summary>
public class GlobTool : ToolBase
{
    public override string Name => "glob";
    public override string Description => "Find files using glob patterns like '**/*.cs' or 'src/**/*.ts'";
    public override bool RequiresApproval => false;

    public override List<ToolParameter> Parameters => new()
    {
        new ToolParameter
        {
            Name = "pattern",
            Type = "string",
            Description = "The glob pattern to match files against (e.g., '**/*.cs', 'src/**/*.txt')",
            Required = true
        },
        new ToolParameter
        {
            Name = "root_directory",
            Type = "string",
            Description = "The root directory to search in (default: current directory)",
            Required = false
        },
        new ToolParameter
        {
            Name = "ignore_case",
            Type = "boolean",
            Description = "Whether to ignore case when matching (default: false)",
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
            Name = "max_results",
            Type = "number",
            Description = "Maximum number of results to return (default: 1000)",
            Required = false
        },
        new ToolParameter
        {
            Name = "exclude_patterns",
            Type = "array",
            Description = "Array of patterns to exclude (e.g., ['node_modules/**', '.git/**'])",
            Required = false
        }
    };

    protected override async Task<ToolExecutionResult> ExecuteInternalAsync(ToolCall call, CancellationToken cancellationToken)
    {
        var pattern = call.Parameters["pattern"]?.ToString();
        var rootDirectory = call.Parameters.GetValueOrDefault("root_directory", ".")?.ToString();
        var ignoreCase = bool.TryParse(call.Parameters.GetValueOrDefault("ignore_case", "false")?.ToString(), out var ignoreCaseValue) && ignoreCaseValue;
        var includeHidden = bool.TryParse(call.Parameters.GetValueOrDefault("include_hidden", "false")?.ToString(), out var includeHiddenValue) && includeHiddenValue;
        var maxResults = int.TryParse(call.Parameters.GetValueOrDefault("max_results", "1000")?.ToString(), out var maxResultsValue) ? maxResultsValue : 1000;

        if (string.IsNullOrEmpty(pattern))
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = "pattern parameter is required"
            };
        }

        if (string.IsNullOrEmpty(rootDirectory))
        {
            rootDirectory = ".";
        }

        // Validate root directory path for security
        if (!IsValidPath(rootDirectory))
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = "Invalid root directory path - potential directory traversal attempt"
            };
        }

        try
        {
            if (!Directory.Exists(rootDirectory))
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    Error = $"Root directory not found: {rootDirectory}"
                };
            }

            var matcher = new Matcher();
            
            // Configure case sensitivity
            if (ignoreCase && OperatingSystem.IsWindows())
            {
                matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
            }
            else if (ignoreCase)
            {
                matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
            }

            // Add include patterns
            var patterns = pattern.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pat in patterns)
            {
                matcher.AddInclude(pat.Trim());
            }

            // Add exclude patterns
            if (call.Parameters.TryGetValue("exclude_patterns", out var excludePatternsObj))
            {
                var excludePatterns = excludePatternsObj switch
                {
                    string[] stringArray => stringArray,
                    string singlePattern => new[] { singlePattern },
                    _ => Array.Empty<string>()
                };

                foreach (var excludePattern in excludePatterns.Where(p => !string.IsNullOrWhiteSpace(p)))
                {
                    matcher.AddExclude(excludePattern.Trim());
                }
            }

            // Default excludes for hidden files if not explicitly included
            if (!includeHidden)
            {
                matcher.AddExclude("**/.*");
                matcher.AddExclude(".*/**");
            }

            // Common excludes for development projects
            var commonExcludes = new[]
            {
                "**/node_modules/**",
                "**/bin/**",
                "**/obj/**",
                "**/.vs/**",
                "**/.vscode/**",
                "**/packages/**"
            };

            foreach (var exclude in commonExcludes)
            {
                matcher.AddExclude(exclude);
            }

            // Execute the matching
            var directoryInfo = new DirectoryInfo(rootDirectory);
            var result = matcher.Execute(new DirectoryInfoWrapper(directoryInfo));
            var matchedFiles = result.Files.Take(maxResults).Select(f => f.Path).ToList();

            // Sort results for consistent output
            matchedFiles.Sort(StringComparer.OrdinalIgnoreCase);

            // Get file information
            var fileInfoList = new List<object>();
            var totalSize = 0L;

            foreach (var filePath in matchedFiles)
            {
                try
                {
                    var fullPath = Path.Combine(rootDirectory, filePath);
                    var fileInfo = new FileInfo(fullPath);
                    
                    if (fileInfo.Exists)
                    {
                        fileInfoList.Add(new
                        {
                            path = filePath,
                            full_path = fullPath,
                            size = fileInfo.Length,
                            modified = fileInfo.LastWriteTimeUtc,
                            is_readonly = fileInfo.IsReadOnly
                        });
                        totalSize += fileInfo.Length;
                    }
                }
                catch
                {
                    // Skip files that can't be accessed
                    continue;
                }
            }

            var output = string.Join("\n", matchedFiles);
            var hasMore = result.Files.Count() > maxResults;

            return new ToolExecutionResult
            {
                Success = true,
                Output = output,
                Metadata = new Dictionary<string, object>
                {
                    ["pattern"] = pattern,
                    ["root_directory"] = rootDirectory,
                    ["match_count"] = matchedFiles.Count,
                    ["total_size"] = totalSize,
                    ["has_more"] = hasMore,
                    ["max_results"] = maxResults,
                    ["ignore_case"] = ignoreCase,
                    ["include_hidden"] = includeHidden,
                    ["file_info"] = fileInfoList
                }
            };
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = $"Failed to execute glob pattern: {ex.Message}"
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