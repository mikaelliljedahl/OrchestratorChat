using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Messages;
using OrchestratorChat.Core.Tools;
using OrchestratorChat.Agents.Exceptions;

namespace OrchestratorChat.Agents.Claude;

public class ClaudeAgent : IAgent, IDisposable
{
    private Process? _process;
    private StreamReader? _outputReader;
    private StreamWriter? _inputWriter;
    private readonly ILogger<ClaudeAgent> _logger;
    private readonly ClaudeConfiguration _configuration;
    private AgentStatus _status = AgentStatus.Uninitialized;
    private readonly SemaphoreSlim _processLock = new(1, 1);
    private CancellationTokenSource? _processCts;

    public string Id { get; private set; }
    public string Name { get; set; } = string.Empty;
    public AgentType Type => AgentType.Claude;
    public AgentStatus Status => _status;
    public AgentCapabilities Capabilities { get; private set; } = new();
    public string WorkingDirectory { get; set; } = string.Empty;

    public event EventHandler<AgentStatusChangedEventArgs>? StatusChanged;
    public event EventHandler<AgentOutputEventArgs>? OutputReceived;

    public ClaudeAgent(
        ILogger<ClaudeAgent> logger,
        IOptions<ClaudeConfiguration> configuration)
    {
        _logger = logger;
        _configuration = configuration.Value;
        Id = Guid.NewGuid().ToString();
    }

    public async Task<AgentInitializationResult> InitializeAsync(
        AgentConfiguration configuration)
    {
        try
        {
            SetStatus(AgentStatus.Initializing);

            // Validate Claude CLI is available
            if (!await ValidateClaudeCliAsync())
            {
                return new AgentInitializationResult
                {
                    Success = false,
                    ErrorMessage = "Claude CLI not found or not authenticated"
                };
            }

            // Start Claude process
            await StartClaudeProcessAsync(configuration);

            // Set capabilities based on model
            Capabilities = GetCapabilitiesForModel(configuration.Model);

            SetStatus(AgentStatus.Ready);

            return new AgentInitializationResult
            {
                Success = true,
                Capabilities = Capabilities,
                InitializationTime = TimeSpan.FromSeconds(2)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Claude agent");
            SetStatus(AgentStatus.Error);
            return new AgentInitializationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<bool> ValidateClaudeCliAsync()
    {
        try
        {
            var baseCmd = GetClaudeBaseCommand();
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            if (OperatingSystem.IsWindows())
            {
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = $"/c {baseCmd} --version";
            }
            else
            {
                startInfo.FileName = baseCmd;
                startInfo.Arguments = "--version";
            }

            var process = new Process { StartInfo = startInfo };
            process.Start();
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> StartClaudeProcessAsync()
    {
        try
        {
            // Find claude executable
            var claudePath = await FindClaudeExecutableAsync();
            if (string.IsNullOrEmpty(claudePath))
            {
                _logger.LogError("Claude executable not found");
                return false;
            }

            // Set up process start info
            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = claudePath,
                    Arguments = "--output-format json-stream",
                    WorkingDirectory = WorkingDirectory,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            // Configure stdin/stdout redirection
            _process.StartInfo.Environment["CLAUDE_OUTPUT_FORMAT"] = "json-stream";

            // Add custom environment variables from configuration
            foreach (var envVar in _configuration.EnvironmentVariables)
            {
                _process.StartInfo.Environment[envVar.Key] = envVar.Value;
            }

            // Start process
            _process.Start();
            _outputReader = _process.StandardOutput;
            _inputWriter = _process.StandardInput;
            _processCts = new CancellationTokenSource();

            // Wait a brief moment for the process to initialize
            await Task.Delay(100);
            
            // Verify process started successfully
            if (_process.HasExited)
            {
                _logger.LogError("Claude process exited immediately with code {ExitCode}", _process.ExitCode);
                return false;
            }
            
            _logger.LogDebug("Claude process started successfully with PID {ProcessId}", _process.Id);

            // Set up output handlers
            _ = Task.Run(() => MonitorOutputAsync(_processCts.Token));
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Claude process");
            return false;
        }
    }

    private async Task StartClaudeProcessAsync(AgentConfiguration config)
    {
        var arguments = BuildClaudeArguments(config);
        
        _logger.LogDebug("Starting Claude process with arguments: {Arguments}", arguments);

        var baseCmd = GetClaudeBaseCommand();
        var startInfo = new ProcessStartInfo
        {
            WorkingDirectory = WorkingDirectory,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        if (OperatingSystem.IsWindows())
        {
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = $"/c {baseCmd} {arguments}";
        }
        else
        {
            startInfo.FileName = baseCmd;
            startInfo.Arguments = arguments;
        }

        _process = new Process { StartInfo = startInfo };
        
        // Set default environment variables
        _process.StartInfo.Environment["CLAUDE_OUTPUT_FORMAT"] = "json-stream";
        
        // Add custom environment variables from configuration
        foreach (var envVar in _configuration.EnvironmentVariables)
        {
            _process.StartInfo.Environment[envVar.Key] = envVar.Value;
        }

        _process.Start();
        _outputReader = _process.StandardOutput;
        _inputWriter = _process.StandardInput;
        _processCts = new CancellationTokenSource();

        // Wait a brief moment for the process to initialize
        await Task.Delay(100);
        
        // Verify process started successfully
        if (_process.HasExited)
        {
            throw new AgentInitializationException($"Claude process exited immediately with code {_process.ExitCode}", Id);
        }
        
        _logger.LogDebug("Claude process started successfully with PID {ProcessId}", _process.Id);

        // Start output monitoring
        _ = Task.Run(() => MonitorOutputAsync(_processCts.Token));
    }

    private string GetClaudeBaseCommand()
    {
        // Allow override via environment variable if set
        var envPath = Environment.GetEnvironmentVariable("CLAUDE_PATH");
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            return envPath.Trim();
        }

        return _configuration.ClaudeExecutablePath ?? "claude";
    }

    private string BuildClaudeArguments(AgentConfiguration config)
    {
        var args = new List<string>();

        // Continue session if exists
        var sessionId = config.CustomSettings?.GetValueOrDefault("SessionId", null as object)?.ToString();
        if (!string.IsNullOrEmpty(sessionId))
        {
            args.Add($"--continue {EscapeArgument(sessionId)}");
        }

        // Model selection
        if (!string.IsNullOrEmpty(config.Model))
        {
            args.Add($"--model {EscapeArgument(config.Model)}");
        }

        // Output format
        args.Add("--output-format json-stream");

        // Temperature (validate range)
        var temperature = Math.Max(0.0, Math.Min(2.0, config.Temperature));
        args.Add($"--temperature {temperature:F1}");

        // Max tokens (validate range)
        var maxTokens = Math.Max(1, Math.Min(200000, config.MaxTokens));
        args.Add($"--max-tokens {maxTokens}");

        // System prompt
        if (!string.IsNullOrEmpty(config.SystemPrompt))
        {
            args.Add($"--system \"{EscapeArgument(config.SystemPrompt)}\"");
        }

        // Tools
        if (config.EnabledTools?.Any() == true)
        {
            var toolsArg = string.Join(",", config.EnabledTools.Select(EscapeArgument));
            args.Add($"--tools {toolsArg}");
        }

        // Add MCP configuration if enabled
        if (_configuration.EnableMcp && !string.IsNullOrEmpty(_configuration.McpConfigPath))
        {
            args.Add($"--mcp-config \"{EscapeArgument(_configuration.McpConfigPath)}\"");
        }

        var result = string.Join(" ", args);
        _logger.LogDebug("Built Claude arguments: {Arguments}", result);
        return result;
    }

    public async Task<IAsyncEnumerable<AgentResponse>> SendMessageStreamAsync(
        AgentMessage message,
        CancellationToken cancellationToken = default)
    {
        await _processLock.WaitAsync(cancellationToken);
        try
        {
            SetStatus(AgentStatus.Busy);
            return await ProcessMessageInternalAsync(message, cancellationToken);
        }
        finally
        {
            _processLock.Release();
            SetStatus(AgentStatus.Ready);
        }
    }

    public async Task<AgentResponse> SendMessageAsync(
        AgentMessage message,
        CancellationToken cancellationToken = default)
    {
        await _processLock.WaitAsync(cancellationToken);
        try
        {
            SetStatus(AgentStatus.Busy);
            var streamResponse = await ProcessMessageInternalAsync(message, cancellationToken);
            
            // Collect all responses and return the final one
            AgentResponse finalResponse = new AgentResponse { Type = ResponseType.Success };
            await foreach (var response in streamResponse.WithCancellation(cancellationToken))
            {
                finalResponse = response;
                if (response.IsComplete)
                    break;
            }
            
            // Ensure success type for completed responses
            if (finalResponse.Type != ResponseType.Error && finalResponse.IsComplete)
            {
                finalResponse.Type = ResponseType.Success;
            }
            
            return finalResponse;
        }
        finally
        {
            _processLock.Release();
            SetStatus(AgentStatus.Ready);
        }
    }

    protected async Task<IAsyncEnumerable<AgentResponse>> ProcessMessageInternalAsync(
        AgentMessage message, 
        CancellationToken cancellationToken)
    {
        if (_inputWriter == null)
            throw new AgentCommunicationException("Agent process not initialized", Id);

        try
        {
            // Format message for Claude
            var formattedMessage = await FormatMessageForClaude(message);
            
            // Handle attachments if any
            if (message.Attachments?.Any() == true)
            {
                formattedMessage = await HandleAttachments(formattedMessage, message.Attachments);
            }

            // Send to Claude process via stdin/stdout
            _logger.LogDebug("Sending formatted message to Claude: {Message}", formattedMessage);
            await _inputWriter.WriteLineAsync(formattedMessage);
            await _inputWriter.FlushAsync();

            // Return streaming responses
            return StreamResponsesAsync(message.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message internally");
            throw new AgentCommunicationException($"Failed to process message: {ex.Message}", Id);
        }
    }

    private async IAsyncEnumerable<AgentResponse> StreamResponsesAsync(
        string messageId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var buffer = new StringBuilder();
        var isComplete = false;

        while (!isComplete && !cancellationToken.IsCancellationRequested)
        {
            var line = await ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(line))
                continue;

            if (TryParseJsonResponse(line, out var response))
            {
                response.MessageId = messageId;

                if (response.Type == ResponseType.Text)
                {
                    buffer.Append(response.Content);
                }

                if (response.IsComplete)
                {
                    isComplete = true;
                    response.Content = buffer.ToString();
                }

                yield return response;
            }
        }
    }

    private bool TryParseJsonResponse(string line, out AgentResponse response)
    {
        response = new AgentResponse();
        
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }
        
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var json = JsonSerializer.Deserialize<ClaudeJsonResponse>(line, options);
            if (json != null)
            {
                response = MapToAgentResponse(json);
                _logger.LogTrace("Successfully parsed JSON response: {Type}, Complete: {IsComplete}", json.Type, json.Done);
                return true;
            }
            
            _logger.LogWarning("Failed to deserialize JSON response - result was null");
            return false;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON response: {Line}", line);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error parsing JSON response: {Line}", line);
            return false;
        }
    }

    public async Task<ToolExecutionResult> ExecuteToolAsync(
        ToolCall toolCall,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteClaudeToolAsync(toolCall);
    }

    private async Task<ToolExecutionResult> ExecuteClaudeToolAsync(ToolCall toolCall)
    {
        try
        {
            // Map tool to Claude's tool format
            var claudeToolCall = MapToClaudeToolFormat(toolCall);
            
            // Send tool execution request
            var toolMessage = new AgentMessage
            {
                Id = Guid.NewGuid().ToString(),
                Content = JsonSerializer.Serialize(claudeToolCall),
                Role = MessageRole.Tool,
                AgentId = Id,
                Timestamp = DateTime.UtcNow
            };

            var responses = new List<AgentResponse>();
            await foreach (var response in await ProcessMessageInternalAsync(toolMessage, CancellationToken.None))
            {
                responses.Add(response);
            }

            // Parse result
            var lastResponse = responses.LastOrDefault();
            var success = lastResponse?.Type != ResponseType.Error;
            
            _logger.LogDebug("Tool execution completed. Success: {Success}, Response: {Response}", 
                success, lastResponse?.Content);

            return new ToolExecutionResult
            {
                Success = success,
                Output = lastResponse?.Content,
                Error = lastResponse?.Type == ResponseType.Error ? lastResponse.Content : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute Claude tool: {ToolName}", toolCall.ToolName);
            return new ToolExecutionResult
            {
                Success = false,
                Error = $"Tool execution failed: {ex.Message}"
            };
        }
    }

    public async Task<AgentStatusInfo> GetStatusAsync()
    {
        return await Task.FromResult(new AgentStatusInfo
        {
            AgentId = Id,
            AgentName = Name,
            Type = Type,
            Status = _status,
            IsHealthy = _status != AgentStatus.Error && _status != AgentStatus.Shutdown,
            LastActivity = DateTime.UtcNow, // Could track actual last activity time
            Capabilities = Capabilities,
            WorkingDirectory = WorkingDirectory,
            Metadata = new Dictionary<string, object>
            {
                { "ProcessId", _process?.Id ?? -1 },
                { "HasProcess", _process != null && !_process.HasExited }
            }
        });
    }

    public async Task ShutdownAsync()
    {
        _logger.LogDebug("Starting Claude agent shutdown");
        SetStatus(AgentStatus.Shutdown);

        try
        {
            // Cancel ongoing operations
            _processCts?.Cancel();

            if (_process != null && !_process.HasExited)
            {
                _logger.LogDebug("Sending exit command to Claude process");
                
                try
                {
                    _inputWriter?.WriteLine("exit");
                    await _inputWriter?.FlushAsync()!;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send exit command to Claude process");
                }

                // Wait for graceful shutdown
                if (!_process.WaitForExit(5000))
                {
                    _logger.LogWarning("Claude process did not exit gracefully, forcing termination");
                    try
                    {
                        _process.Kill();
                        await _process.WaitForExitAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to kill Claude process");
                    }
                }
                else
                {
                    _logger.LogDebug("Claude process exited gracefully");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Claude agent shutdown");
        }
        finally
        {
            Dispose();
            _logger.LogDebug("Claude agent shutdown completed");
        }
    }

    private void SetStatus(AgentStatus status)
    {
        var oldStatus = _status;
        _status = status;

        if (oldStatus != status)
        {
            StatusChanged?.Invoke(this, new AgentStatusChangedEventArgs
            {
                AgentId = Id,
                OldStatus = oldStatus,
                NewStatus = status
            });
        }
    }

    private async Task<string?> ReadLineAsync(CancellationToken cancellationToken)
    {
        if (_outputReader == null) 
        {
            _logger.LogWarning("Cannot read line - output reader is null");
            return null;
        }

        try
        {
            // Create a task that completes when either reading completes or cancellation is requested
            var readTask = _outputReader.ReadLineAsync();
            var completedTask = await Task.WhenAny(readTask, Task.Delay(Timeout.Infinite, cancellationToken));
            
            if (completedTask == readTask)
            {
                return await readTask;
            }
            else
            {
                // Cancellation was requested
                cancellationToken.ThrowIfCancellationRequested();
                return null;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("ReadLine operation cancelled");
            return null;
        }
        catch (ObjectDisposedException)
        {
            _logger.LogDebug("ReadLine operation failed - stream disposed");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading line from Claude output");
            return null;
        }
    }

    private async Task HandleClaudeOutputAsync(string output)
    {
        try
        {
            // Parse output format
            if (string.IsNullOrWhiteSpace(output))
                return;

            // Detect tool calls
            if (TryParseJsonResponse(output, out var response))
            {
                // Handle streaming updates
                if (response.Type == ResponseType.ToolCall && response.ToolCalls?.Any() == true)
                {
                    _logger.LogDebug("Detected tool calls in output: {ToolCount}", response.ToolCalls.Count);
                    
                    // Process each tool call
                    foreach (var toolCall in response.ToolCalls)
                    {
                        _logger.LogDebug("Processing tool call: {ToolName}", toolCall.ToolName);
                        // Tool execution is handled by Claude internally in this context
                    }
                }

                // Emit events
                OutputReceived?.Invoke(this, new AgentOutputEventArgs
                {
                    AgentId = Id,
                    Content = output
                });
            }
            else
            {
                // Handle raw text output
                _logger.LogTrace("Received raw output: {Output}", output);
                
                OutputReceived?.Invoke(this, new AgentOutputEventArgs
                {
                    AgentId = Id,
                    Content = output
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Claude output: {Output}", output);
        }
    }

    private async Task MonitorOutputAsync(CancellationToken cancellationToken)
    {
        if (_outputReader == null)
        {
            _logger.LogWarning("Cannot monitor output - output reader is null");
            return;
        }

        try
        {
            _logger.LogDebug("Starting output monitoring for Claude process");
            
            while (!cancellationToken.IsCancellationRequested && !_outputReader.EndOfStream)
            {
                var line = await _outputReader.ReadLineAsync();
                if (!string.IsNullOrEmpty(line))
                {
                    _logger.LogTrace("Received output line: {Line}", line);
                    
                    // Enhanced output handling
                    await HandleClaudeOutputAsync(line);
                }
            }
            
            _logger.LogDebug("Output monitoring stopped - stream ended or cancellation requested");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Output monitoring cancelled");
        }
        catch (ObjectDisposedException)
        {
            _logger.LogDebug("Output monitoring stopped - stream disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring Claude output");
            SetStatus(AgentStatus.Error);
        }
    }

    private AgentCapabilities GetCapabilitiesForModel(string model)
    {
        var maxTokens = model.ToLower() switch
        {
            var m when m.Contains("claude-opus-4") => 200000,
            var m when m.Contains("claude-sonnet-4") => 200000,
            var m when m.Contains("claude-3.7-sonnet") => 200000,
            var m when m.Contains("claude-3.5-haiku") => 200000,
            var m when m.Contains("claude-3.5-sonnet") => 200000,
            var m when m.Contains("claude-3-opus") => 200000,  // Legacy support
            var m when m.Contains("claude-3-sonnet") => 200000, // Legacy support
            var m when m.Contains("claude-3-haiku") => 200000,  // Legacy support
            _ => 100000
        };

        return new AgentCapabilities
        {
            SupportsStreaming = true,
            SupportsTools = true,
            SupportsFileOperations = true,
            SupportsWebSearch = false,
            SupportedModels = new List<string> { model },
            MaxTokens = maxTokens,
            MaxConcurrentRequests = 1
        };
    }

    private AgentResponse MapToAgentResponse(ClaudeJsonResponse json)
    {
        return new AgentResponse
        {
            Content = json.Content ?? string.Empty,
            Type = MapResponseType(json.Type),
            IsComplete = json.Done,
            ToolCalls = json.ToolCalls,
            Usage = json.Usage != null ? new TokenUsage
            {
                InputTokens = json.Usage.InputTokens,
                OutputTokens = json.Usage.OutputTokens,
                TotalTokens = json.Usage.TotalTokens
            } : null
        };
    }

    private ResponseType MapResponseType(string? type)
    {
        return type switch
        {
            "text" => ResponseType.Text,
            "tool_call" => ResponseType.ToolCall,
            "error" => ResponseType.Error,
            _ => ResponseType.Text
        };
    }

    private async Task<string> FindClaudeExecutableAsync()
    {
        // Check configuration first
        if (!string.IsNullOrEmpty(_configuration.ClaudeExecutablePath))
        {
            return _configuration.ClaudeExecutablePath;
        }

        // Try common paths and check if claude is in PATH
        var commonPaths = new[]
        {
            "claude",
            "claude.exe",
            Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\.claude\claude.exe"),
            Environment.ExpandEnvironmentVariables(@"%PROGRAMFILES%\Claude\claude.exe")
        };

        foreach (var path in commonPaths)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync();
                if (process.ExitCode == 0)
                {
                    _logger.LogDebug("Found Claude executable at: {Path}", path);
                    return path;
                }
            }
            catch
            {
                // Continue checking other paths
            }
        }

        throw new FileNotFoundException("Claude executable not found. Please ensure Claude CLI is installed and in your PATH.");
    }

    private async Task<string> FormatMessageForClaude(AgentMessage message)
    {
        // Format message according to Claude's expected input format
        var formattedMessage = new
        {
            role = message.Role.ToString().ToLower(),
            content = message.Content,
            timestamp = message.Timestamp.ToString("O")
        };

        return JsonSerializer.Serialize(formattedMessage, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private async Task<string> HandleAttachments(string message, IEnumerable<Attachment> attachments)
    {
        var messageObj = JsonSerializer.Deserialize<dynamic>(message);
        var attachmentData = new List<object>();

        foreach (var attachment in attachments)
        {
            // Determine attachment type based on MIME type
            if (attachment.MimeType.StartsWith("image/"))
            {
                attachmentData.Add(new
                {
                    type = "image",
                    source = new
                    {
                        type = "base64",
                        media_type = attachment.MimeType,
                        data = Convert.ToBase64String(attachment.Content)
                    }
                });
            }
            else
            {
                // Treat as document for other types
                attachmentData.Add(new
                {
                    type = "document",
                    source = new
                    {
                        type = "base64",
                        media_type = attachment.MimeType,
                        data = Convert.ToBase64String(attachment.Content)
                    },
                    name = attachment.FileName
                });
            }
        }

        var enhancedMessage = new
        {
            role = "user",
            content = new object[]
            {
                new { type = "text", text = messageObj?.content?.ToString() ?? "" }
            }.Concat(attachmentData).ToArray()
        };

        return JsonSerializer.Serialize(enhancedMessage, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private object MapToClaudeToolFormat(ToolCall toolCall)
    {
        return new
        {
            type = "tool_use",
            id = toolCall.Id,
            name = toolCall.ToolName,
            input = toolCall.Parameters
        };
    }

    private static string EscapeArgument(string argument)
    {
        if (string.IsNullOrEmpty(argument))
            return string.Empty;
            
        // Escape quotes and backslashes for shell
        return argument
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");
    }

    public void Dispose()
    {
        try
        {
            _processCts?.Cancel();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error cancelling process token during dispose");
        }
        
        try
        {
            _processCts?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing process cancellation token source");
        }
        
        try
        {
            _inputWriter?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing input writer");
        }
        
        try
        {
            _outputReader?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing output reader");
        }
        
        try
        {
            _process?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing process");
        }
        
        try
        {
            _processLock?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing process lock");
        }
    }
}
