using OrchestratorChat.Saturn.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace OrchestratorChat.Saturn.Tools.Implementations;

/// <summary>
/// Tool for finding and replacing text across files with preview and backup options
/// </summary>
public class SearchAndReplaceTool : ToolBase
{
    public override string Name => "search_and_replace";
    public override string Description => "Find and replace text across files with regex support, preview, and backup options";
    public override bool RequiresApproval => true;

    public override List<ToolParameter> Parameters => new()
    {
        new ToolParameter
        {
            Name = "file_path",
            Type = "string",
            Description = "The file path to search and replace in (supports glob patterns)",
            Required = true
        },
        new ToolParameter
        {
            Name = "search_pattern",
            Type = "string",
            Description = "The text or regex pattern to search for",
            Required = true
        },
        new ToolParameter
        {
            Name = "replacement",
            Type = "string",
            Description = "The replacement text (supports regex groups like $1, $2)",
            Required = true
        },
        new ToolParameter
        {
            Name = "use_regex",
            Type = "boolean",
            Description = "Whether to treat search_pattern as a regular expression (default: false)",
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
            Name = "multiline",
            Type = "boolean",
            Description = "Whether to enable multiline mode for regex (default: false)",
            Required = false
        },
        new ToolParameter
        {
            Name = "preview_only",
            Type = "boolean",
            Description = "Whether to only preview changes without applying them (default: false)",
            Required = false
        },
        new ToolParameter
        {
            Name = "create_backup",
            Type = "boolean",
            Description = "Whether to create backup files before making changes (default: true)",
            Required = false
        },
        new ToolParameter
        {
            Name = "encoding",
            Type = "string",
            Description = "File encoding to use (default: utf-8)",
            Required = false
        },
        new ToolParameter
        {
            Name = "max_matches",
            Type = "number",
            Description = "Maximum number of matches to replace per file (default: unlimited)",
            Required = false
        }
    };

    protected override async Task<ToolExecutionResult> ExecuteInternalAsync(ToolCall call, CancellationToken cancellationToken)
    {
        var filePath = call.Parameters["file_path"]?.ToString();
        var searchPattern = call.Parameters["search_pattern"]?.ToString();
        var replacement = call.Parameters["replacement"]?.ToString();
        var useRegex = bool.TryParse(call.Parameters.GetValueOrDefault("use_regex", "false")?.ToString(), out var useRegexValue) && useRegexValue;
        var ignoreCase = bool.TryParse(call.Parameters.GetValueOrDefault("ignore_case", "false")?.ToString(), out var ignoreCaseValue) && ignoreCaseValue;
        var multiline = bool.TryParse(call.Parameters.GetValueOrDefault("multiline", "false")?.ToString(), out var multilineValue) && multilineValue;
        var previewOnly = bool.TryParse(call.Parameters.GetValueOrDefault("preview_only", "false")?.ToString(), out var previewOnlyValue) && previewOnlyValue;
        var createBackup = !bool.TryParse(call.Parameters.GetValueOrDefault("create_backup", "true")?.ToString(), out var createBackupValue) || createBackupValue;
        var encoding = call.Parameters.GetValueOrDefault("encoding", "utf-8")?.ToString() ?? "utf-8";
        var maxMatches = int.TryParse(call.Parameters.GetValueOrDefault("max_matches", "-1")?.ToString(), out var maxMatchesValue) ? maxMatchesValue : -1;

        if (string.IsNullOrEmpty(filePath))
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = "file_path parameter is required"
            };
        }

        if (string.IsNullOrEmpty(searchPattern))
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = "search_pattern parameter is required"
            };
        }

        if (replacement == null) // Allow empty string replacement
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = "replacement parameter is required"
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
            // Determine if this is a glob pattern or single file
            var files = new List<string>();
            if (filePath.Contains('*') || filePath.Contains('?'))
            {
                // Handle glob pattern - use simple wildcard matching
                var directory = Path.GetDirectoryName(filePath) ?? ".";
                var pattern = Path.GetFileName(filePath);
                
                if (Directory.Exists(directory))
                {
                    files.AddRange(Directory.GetFiles(directory, pattern, SearchOption.AllDirectories));
                }
            }
            else
            {
                // Single file
                if (File.Exists(filePath))
                {
                    files.Add(filePath);
                }
                else
                {
                    return new ToolExecutionResult
                    {
                        Success = false,
                        Error = $"File not found: {filePath}"
                    };
                }
            }

            if (files.Count == 0)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    Error = "No matching files found"
                };
            }

            // Set up encoding
            var fileEncoding = encoding.ToLowerInvariant() switch
            {
                "utf-8" => Encoding.UTF8,
                "utf-16" => Encoding.Unicode,
                "ascii" => Encoding.ASCII,
                _ => Encoding.UTF8
            };

            var results = new List<object>();
            var totalMatches = 0;
            var totalReplacements = 0;
            var processedFiles = 0;
            var output = new StringBuilder();

            foreach (var file in files)
            {
                try
                {
                    var fileResult = await ProcessFileAsync(file, searchPattern, replacement, useRegex, ignoreCase, 
                        multiline, previewOnly, createBackup, fileEncoding, maxMatches, cancellationToken);
                    
                    results.Add(fileResult);
                    totalMatches += fileResult.MatchCount;
                    totalReplacements += fileResult.ReplacementCount;
                    processedFiles++;

                    // Add to output
                    if (fileResult.MatchCount > 0)
                    {
                        output.AppendLine($"File: {file}");
                        output.AppendLine($"  Matches: {fileResult.MatchCount}");
                        
                        if (!previewOnly)
                        {
                            output.AppendLine($"  Replacements: {fileResult.ReplacementCount}");
                        }

                        // Show preview of changes
                        if (fileResult.Preview.Count > 0)
                        {
                            output.AppendLine("  Changes:");
                            foreach (var preview in fileResult.Preview.Take(5)) // Limit preview lines
                            {
                                output.AppendLine($"    Line {preview.LineNumber}: {preview.Original} -> {preview.Modified}");
                            }
                            
                            if (fileResult.Preview.Count > 5)
                            {
                                output.AppendLine($"    ... and {fileResult.Preview.Count - 5} more changes");
                            }
                        }
                        
                        output.AppendLine();
                    }
                }
                catch (Exception ex)
                {
                    results.Add(new
                    {
                        file_path = file,
                        success = false,
                        error = ex.Message,
                        match_count = 0,
                        replacement_count = 0
                    });
                }
            }

            var successMessage = previewOnly 
                ? $"Preview completed: Found {totalMatches} matches in {processedFiles} files"
                : $"Search and replace completed: {totalReplacements} replacements made in {processedFiles} files";

            return new ToolExecutionResult
            {
                Success = true,
                Output = output.ToString().TrimEnd(),
                Metadata = new Dictionary<string, object>
                {
                    ["search_pattern"] = searchPattern,
                    ["replacement"] = replacement,
                    ["use_regex"] = useRegex,
                    ["ignore_case"] = ignoreCase,
                    ["multiline"] = multiline,
                    ["preview_only"] = previewOnly,
                    ["create_backup"] = createBackup,
                    ["encoding"] = encoding,
                    ["total_files_processed"] = processedFiles,
                    ["total_matches"] = totalMatches,
                    ["total_replacements"] = totalReplacements,
                    ["file_results"] = results
                }
            };
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = $"Failed to search and replace: {ex.Message}"
            };
        }
    }

    private async Task<FileProcessResult> ProcessFileAsync(string filePath, string searchPattern, string replacement,
        bool useRegex, bool ignoreCase, bool multiline, bool previewOnly, bool createBackup, 
        Encoding encoding, int maxMatches, CancellationToken cancellationToken)
    {
        var result = new FileProcessResult
        {
            FilePath = filePath,
            Success = true,
            Preview = new List<ChangePreview>()
        };

        try
        {
            var originalContent = await File.ReadAllTextAsync(filePath, encoding, cancellationToken);
            var modifiedContent = originalContent;
            var lines = originalContent.Split('\n');

            if (useRegex)
            {
                var options = RegexOptions.None;
                if (ignoreCase) options |= RegexOptions.IgnoreCase;
                if (multiline) options |= RegexOptions.Multiline;

                var regex = new Regex(searchPattern, options);
                var matches = regex.Matches(originalContent);
                result.MatchCount = matches.Count;

                if (maxMatches > 0 && matches.Count > maxMatches)
                {
                    // Limit matches
                    var limitedMatches = matches.Take(maxMatches);
                    result.MatchCount = maxMatches;
                }

                // Generate preview
                for (var i = 0; i < Math.Min(matches.Count, maxMatches > 0 ? maxMatches : matches.Count); i++)
                {
                    var match = matches[i];
                    var lineNumber = GetLineNumber(originalContent, match.Index);
                    var lineContent = GetLineContent(lines, lineNumber - 1);
                    var modifiedLine = regex.Replace(lineContent, replacement, 1);

                    result.Preview.Add(new ChangePreview
                    {
                        LineNumber = lineNumber,
                        Original = lineContent.Trim(),
                        Modified = modifiedLine.Trim()
                    });
                }

                if (!previewOnly)
                {
                    if (maxMatches > 0)
                    {
                        // Replace only the specified number of matches
                        var replacementCount = 0;
                        modifiedContent = regex.Replace(originalContent, m =>
                        {
                            if (replacementCount < maxMatches)
                            {
                                replacementCount++;
                                return regex.Replace(m.Value, replacement);
                            }
                            return m.Value;
                        });
                        result.ReplacementCount = replacementCount;
                    }
                    else
                    {
                        modifiedContent = regex.Replace(originalContent, replacement);
                        result.ReplacementCount = matches.Count;
                    }
                }
            }
            else
            {
                // String replacement
                var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                var matches = CountStringOccurrences(originalContent, searchPattern, comparison);
                result.MatchCount = matches;

                if (matches > 0)
                {
                    // Generate preview for string replacement
                    var currentIndex = 0;
                    var previewCount = 0;
                    
                    while (currentIndex < originalContent.Length && previewCount < 10)
                    {
                        var index = originalContent.IndexOf(searchPattern, currentIndex, comparison);
                        if (index == -1) break;

                        var lineNumber = GetLineNumber(originalContent, index);
                        var lineContent = GetLineContent(lines, lineNumber - 1);
                        var modifiedLine = lineContent.Replace(searchPattern, replacement, comparison);

                        result.Preview.Add(new ChangePreview
                        {
                            LineNumber = lineNumber,
                            Original = lineContent.Trim(),
                            Modified = modifiedLine.Trim()
                        });

                        currentIndex = index + searchPattern.Length;
                        previewCount++;
                    }

                    if (!previewOnly)
                    {
                        if (maxMatches > 0)
                        {
                            // Replace only the specified number of matches
                            var replacedCount = 0;
                            var currentPos = 0;
                            var sb = new StringBuilder();

                            while (currentPos < originalContent.Length && replacedCount < maxMatches)
                            {
                                var index = originalContent.IndexOf(searchPattern, currentPos, comparison);
                                if (index == -1)
                                {
                                    sb.Append(originalContent[currentPos..]);
                                    break;
                                }

                                sb.Append(originalContent[currentPos..index]);
                                sb.Append(replacement);
                                replacedCount++;
                                currentPos = index + searchPattern.Length;
                            }

                            if (currentPos < originalContent.Length)
                            {
                                sb.Append(originalContent[currentPos..]);
                            }

                            modifiedContent = sb.ToString();
                            result.ReplacementCount = replacedCount;
                        }
                        else
                        {
                            modifiedContent = originalContent.Replace(searchPattern, replacement, comparison);
                            result.ReplacementCount = matches;
                        }
                    }
                }
            }

            // Save changes if not preview only
            if (!previewOnly && result.MatchCount > 0 && modifiedContent != originalContent)
            {
                // Create backup if requested
                if (createBackup)
                {
                    var backupPath = $"{filePath}.backup.{DateTime.UtcNow:yyyyMMddHHmmss}";
                    await File.WriteAllTextAsync(backupPath, originalContent, encoding, cancellationToken);
                    result.BackupPath = backupPath;
                }

                await File.WriteAllTextAsync(filePath, modifiedContent, encoding, cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            return result;
        }
    }

    private static int CountStringOccurrences(string text, string pattern, StringComparison comparison)
    {
        var count = 0;
        var index = 0;

        while ((index = text.IndexOf(pattern, index, comparison)) != -1)
        {
            count++;
            index += pattern.Length;
        }

        return count;
    }

    private static int GetLineNumber(string text, int characterIndex)
    {
        return text[..characterIndex].Count(c => c == '\n') + 1;
    }

    private static string GetLineContent(string[] lines, int lineIndex)
    {
        return lineIndex >= 0 && lineIndex < lines.Length ? lines[lineIndex] : "";
    }

    private static bool IsValidPath(string path)
    {
        try
        {
            // Allow glob patterns
            var cleanPath = path.Replace("*", "").Replace("?", "");
            var directory = Path.GetDirectoryName(cleanPath);
            
            if (!string.IsNullOrEmpty(directory))
            {
                var fullPath = Path.GetFullPath(directory);
                
                // Check for dangerous patterns
                if (path.Contains("..") && !path.StartsWith("./") && !path.StartsWith(".\\"))
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

    private class FileProcessResult
    {
        public string FilePath { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Error { get; set; }
        public int MatchCount { get; set; }
        public int ReplacementCount { get; set; }
        public string? BackupPath { get; set; }
        public List<ChangePreview> Preview { get; set; } = new();
    }

    private class ChangePreview
    {
        public int LineNumber { get; set; }
        public string Original { get; set; } = string.Empty;
        public string Modified { get; set; } = string.Empty;
    }
}