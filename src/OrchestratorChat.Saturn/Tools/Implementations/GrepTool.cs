using OrchestratorChat.Saturn.Models;
using System.Text.RegularExpressions;
using System.Text;

namespace OrchestratorChat.Saturn.Tools.Implementations;

/// <summary>
/// Enhanced tool for searching text using regex patterns across files and directories
/// </summary>
public class GrepTool : ToolBase
{
    public override string Name => "grep";
    public override string Description => "Search for patterns in text, files, or directories with advanced filtering and context options";
    public override bool RequiresApproval => false;

    public override List<ToolParameter> Parameters => new()
    {
        new ToolParameter
        {
            Name = "pattern",
            Type = "string",
            Description = "The regex pattern to search for",
            Required = true
        },
        new ToolParameter
        {
            Name = "text",
            Type = "string",
            Description = "The text to search in (alternative to file_path)",
            Required = false
        },
        new ToolParameter
        {
            Name = "file_path",
            Type = "string",
            Description = "The file or directory to search in (supports glob patterns like '*.cs')",
            Required = false
        },
        new ToolParameter
        {
            Name = "recursive",
            Type = "boolean",
            Description = "Whether to search directories recursively (default: true when file_path is a directory)",
            Required = false
        },
        new ToolParameter
        {
            Name = "ignore_case",
            Type = "boolean",
            Description = "Whether to ignore case (default: false)",
            Required = false
        },
        new ToolParameter
        {
            Name = "line_numbers",
            Type = "boolean",
            Description = "Whether to include line numbers (default: true)",
            Required = false
        },
        new ToolParameter
        {
            Name = "max_matches",
            Type = "number",
            Description = "Maximum number of matches to return (default: 100)",
            Required = false
        },
        new ToolParameter
        {
            Name = "context_before",
            Type = "number",
            Description = "Number of lines to show before each match (default: 0)",
            Required = false
        },
        new ToolParameter
        {
            Name = "context_after",
            Type = "number",
            Description = "Number of lines to show after each match (default: 0)",
            Required = false
        },
        new ToolParameter
        {
            Name = "file_extensions",
            Type = "array",
            Description = "File extensions to include in search (e.g., ['cs', 'js', 'txt'])",
            Required = false
        },
        new ToolParameter
        {
            Name = "exclude_patterns",
            Type = "array",
            Description = "Patterns to exclude from search (e.g., ['*.min.js', 'node_modules/**'])",
            Required = false
        },
        new ToolParameter
        {
            Name = "include_binary",
            Type = "boolean",
            Description = "Whether to search in binary files (default: false)",
            Required = false
        },
        new ToolParameter
        {
            Name = "multiline",
            Type = "boolean",
            Description = "Whether to enable multiline regex mode (default: false)",
            Required = false
        },
        new ToolParameter
        {
            Name = "word_boundary",
            Type = "boolean",
            Description = "Whether to match whole words only (default: false)",
            Required = false
        }
    };

    protected override async Task<ToolExecutionResult> ExecuteInternalAsync(ToolCall call, CancellationToken cancellationToken)
    {
        var pattern = call.Parameters["pattern"]?.ToString();
        var text = call.Parameters.GetValueOrDefault("text")?.ToString();
        var filePath = call.Parameters.GetValueOrDefault("file_path")?.ToString();
        var recursive = bool.TryParse(call.Parameters.GetValueOrDefault("recursive")?.ToString(), out var recursiveValue) ? recursiveValue : (string.IsNullOrEmpty(filePath) ? false : Directory.Exists(filePath));
        var ignoreCase = bool.TryParse(call.Parameters.GetValueOrDefault("ignore_case", "false")?.ToString(), out var ignoreCaseValue) && ignoreCaseValue;
        var lineNumbers = !bool.TryParse(call.Parameters.GetValueOrDefault("line_numbers", "true")?.ToString(), out var lineNumbersValue) || lineNumbersValue;
        var maxMatches = int.TryParse(call.Parameters.GetValueOrDefault("max_matches", "100")?.ToString(), out var maxMatchesValue) ? maxMatchesValue : 100;
        var contextBefore = int.TryParse(call.Parameters.GetValueOrDefault("context_before", "0")?.ToString(), out var contextBeforeValue) ? contextBeforeValue : 0;
        var contextAfter = int.TryParse(call.Parameters.GetValueOrDefault("context_after", "0")?.ToString(), out var contextAfterValue) ? contextAfterValue : 0;
        var includeBinary = bool.TryParse(call.Parameters.GetValueOrDefault("include_binary", "false")?.ToString(), out var includeBinaryValue) && includeBinaryValue;
        var multiline = bool.TryParse(call.Parameters.GetValueOrDefault("multiline", "false")?.ToString(), out var multilineValue) && multilineValue;
        var wordBoundary = bool.TryParse(call.Parameters.GetValueOrDefault("word_boundary", "false")?.ToString(), out var wordBoundaryValue) && wordBoundaryValue;

        if (string.IsNullOrEmpty(pattern))
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = "pattern parameter is required"
            };
        }

        if (string.IsNullOrEmpty(text) && string.IsNullOrEmpty(filePath))
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = "Either text or file_path parameter is required"
            };
        }

        // Parse file extensions filter
        var fileExtensions = ParseArrayParameter(call.Parameters.GetValueOrDefault("file_extensions"));
        var excludePatterns = ParseArrayParameter(call.Parameters.GetValueOrDefault("exclude_patterns"));

        // Validate file path for security
        if (!string.IsNullOrEmpty(filePath) && !IsValidPath(filePath))
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = "Invalid file path - potential directory traversal attempt"
            };
        }

        try
        {
            // Adjust pattern for word boundary if requested
            if (wordBoundary)
            {
                pattern = $@"\b{pattern}\b";
            }

            var regexOptions = multiline ? RegexOptions.Multiline | RegexOptions.Singleline : RegexOptions.Multiline;
            if (ignoreCase)
                regexOptions |= RegexOptions.IgnoreCase;

            var regex = new Regex(pattern, regexOptions);
            var results = new List<SearchResult>();
            var totalMatches = 0;
            var processedFiles = 0;

            if (!string.IsNullOrEmpty(text))
            {
                // Search in provided text
                var matches = await SearchInTextAsync(regex, text, "text", lineNumbers, contextBefore, contextAfter, maxMatches);
                results.AddRange(matches);
                totalMatches += matches.Count;
                processedFiles = 1;
            }
            else
            {
                // Search in files
                var files = await GetFilesToSearchAsync(filePath!, recursive, fileExtensions, excludePatterns, cancellationToken);
                
                foreach (var file in files)
                {
                    if (totalMatches >= maxMatches)
                        break;

                    try
                    {
                        // Skip binary files unless explicitly included
                        if (!includeBinary && await IsBinaryFileAsync(file))
                            continue;

                        var fileContent = await File.ReadAllTextAsync(file, cancellationToken);
                        var remainingMatches = maxMatches - totalMatches;
                        var matches = await SearchInTextAsync(regex, fileContent, file, lineNumbers, contextBefore, contextAfter, remainingMatches);
                        
                        results.AddRange(matches);
                        totalMatches += matches.Count;
                        processedFiles++;
                    }
                    catch (Exception ex)
                    {
                        // Log file access error but continue with other files
                        results.Add(new SearchResult
                        {
                            File = file,
                            LineNumber = 0,
                            Content = $"Error reading file: {ex.Message}",
                            IsError = true
                        });
                    }
                }
            }

            // Format output
            var output = FormatSearchResults(results, lineNumbers, contextBefore > 0 || contextAfter > 0);

            return new ToolExecutionResult
            {
                Success = true,
                Output = output,
                Metadata = new Dictionary<string, object>
                {
                    ["pattern"] = pattern,
                    ["match_count"] = totalMatches,
                    ["processed_files"] = processedFiles,
                    ["ignore_case"] = ignoreCase,
                    ["line_numbers"] = lineNumbers,
                    ["context_before"] = contextBefore,
                    ["context_after"] = contextAfter,
                    ["recursive"] = recursive,
                    ["include_binary"] = includeBinary,
                    ["multiline"] = multiline,
                    ["word_boundary"] = wordBoundary,
                    ["file_extensions"] = fileExtensions,
                    ["exclude_patterns"] = excludePatterns,
                    ["source"] = !string.IsNullOrEmpty(filePath) ? $"files:{filePath}" : "text",
                    ["search_results"] = results.Select(r => new
                    {
                        file = r.File,
                        line_number = r.LineNumber,
                        content = r.Content,
                        is_context = r.IsContext,
                        is_error = r.IsError
                    }).ToList()
                }
            };
        }
        catch (RegexParseException ex)
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = $"Invalid regex pattern: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = $"Failed to search: {ex.Message}"
            };
        }
    }

    private async Task<List<SearchResult>> SearchInTextAsync(Regex regex, string text, string source, 
        bool lineNumbers, int contextBefore, int contextAfter, int maxMatches)
    {
        var results = new List<SearchResult>();
        var lines = text.Split('\n');
        var matchedLines = new HashSet<int>();
        var matchCount = 0;

        // Find all matches first
        for (var i = 0; i < lines.Length && matchCount < maxMatches; i++)
        {
            var line = lines[i];
            var match = regex.Match(line);
            
            if (match.Success)
            {
                matchedLines.Add(i);
                matchCount++;
            }
        }

        // Add context lines and format results
        var contextLines = new HashSet<int>();
        foreach (var matchedLine in matchedLines)
        {
            // Add context before
            for (var i = Math.Max(0, matchedLine - contextBefore); i < matchedLine; i++)
            {
                contextLines.Add(i);
            }
            
            // Add context after
            for (var i = matchedLine + 1; i <= Math.Min(lines.Length - 1, matchedLine + contextAfter); i++)
            {
                contextLines.Add(i);
            }
        }

        // Create results with proper ordering
        var allLines = matchedLines.Union(contextLines).OrderBy(x => x).ToList();
        
        foreach (var lineIndex in allLines)
        {
            var line = lines[lineIndex];
            var isMatch = matchedLines.Contains(lineIndex);
            var isContext = !isMatch && contextLines.Contains(lineIndex);
            
            results.Add(new SearchResult
            {
                File = source,
                LineNumber = lineIndex + 1,
                Content = line.TrimEnd(),
                IsContext = isContext
            });
        }

        return results;
    }

    private async Task<List<string>> GetFilesToSearchAsync(string path, bool recursive, 
        List<string> fileExtensions, List<string> excludePatterns, CancellationToken cancellationToken)
    {
        var files = new List<string>();

        if (File.Exists(path))
        {
            // Single file
            files.Add(path);
        }
        else if (Directory.Exists(path))
        {
            // Directory
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var allFiles = Directory.EnumerateFiles(path, "*.*", searchOption);
            
            foreach (var file in allFiles)
            {
                // Apply extension filter
                if (fileExtensions.Count > 0)
                {
                    var extension = Path.GetExtension(file).TrimStart('.');
                    if (!fileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                        continue;
                }

                // Apply exclude patterns
                var relativePath = Path.GetRelativePath(path, file);
                if (excludePatterns.Any(pattern => IsMatchingPattern(relativePath, pattern)))
                    continue;

                files.Add(file);
            }
        }
        else if (path.Contains('*') || path.Contains('?'))
        {
            // Glob pattern
            var directory = Path.GetDirectoryName(path) ?? ".";
            var pattern = Path.GetFileName(path);
            
            if (Directory.Exists(directory))
            {
                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                files.AddRange(Directory.GetFiles(directory, pattern, searchOption));
            }
        }

        return files;
    }

    private static async Task<bool> IsBinaryFileAsync(string filePath)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var buffer = new byte[8000];
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            
            // Check for null bytes (common in binary files)
            for (var i = 0; i < bytesRead; i++)
            {
                if (buffer[i] == 0)
                    return true;
            }

            return false;
        }
        catch
        {
            return true; // Assume binary if we can't read it
        }
    }

    private static List<string> ParseArrayParameter(object? parameter)
    {
        return parameter switch
        {
            string[] stringArray => stringArray.ToList(),
            string singleString when !string.IsNullOrEmpty(singleString) => new List<string> { singleString },
            _ => new List<string>()
        };
    }

    private static bool IsMatchingPattern(string path, string pattern)
    {
        // Simple glob pattern matching
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        
        return Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase);
    }

    private static string FormatSearchResults(List<SearchResult> results, bool lineNumbers, bool hasContext)
    {
        if (results.Count == 0)
            return "No matches found";

        var output = new StringBuilder();
        var currentFile = string.Empty;
        var fileGroups = results.GroupBy(r => r.File).ToList();

        foreach (var fileGroup in fileGroups)
        {
            var file = fileGroup.Key;
            var fileResults = fileGroup.ToList();
            
            // Show file header if multiple files
            if (fileGroups.Count > 1)
            {
                if (!string.IsNullOrEmpty(currentFile))
                    output.AppendLine();
                
                output.AppendLine($"=== {file} ===");
                currentFile = file;
            }

            // Show results
            foreach (var result in fileResults)
            {
                if (result.IsError)
                {
                    output.AppendLine($"ERROR: {result.Content}");
                    continue;
                }

                var prefix = result.IsContext ? "-" : ":";
                var line = lineNumbers ? $"{result.LineNumber}{prefix}{result.Content}" : result.Content;
                output.AppendLine(line);
            }
        }

        return output.ToString().TrimEnd();
    }

    private static bool IsValidPath(string path)
    {
        try
        {
            // Allow glob patterns and relative paths
            var cleanPath = path.Replace("*", "").Replace("?", "");
            
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

    private class SearchResult
    {
        public string File { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsContext { get; set; }
        public bool IsError { get; set; }
    }
}