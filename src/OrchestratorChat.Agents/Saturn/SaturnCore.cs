using Microsoft.Extensions.Logging;
using OrchestratorChat.Saturn.Core;
using OrchestratorChat.Saturn.Models;
using OrchestratorChat.Saturn.Providers;
using OrchestratorChat.Saturn.Agents;
using OrchestratorChat.Saturn.Tools;
using ISaturnAgent = OrchestratorChat.Saturn.Core.ISaturnAgent;
using SaturnAgentConfiguration = OrchestratorChat.Saturn.Models.SaturnAgentConfiguration;
using SaturnToolInfo = OrchestratorChat.Saturn.Models.ToolInfo;
using SaturnAgentMessage = OrchestratorChat.Saturn.Models.AgentMessage;
using SaturnToolCall = OrchestratorChat.Saturn.Models.ToolCall;
using SaturnToolExecutionResult = OrchestratorChat.Saturn.Models.ToolExecutionResult;
using SaturnToolRegistry = OrchestratorChat.Saturn.Tools.ToolRegistry;
using SaturnILLMProvider = OrchestratorChat.Saturn.Providers.ILLMProvider;
using SaturnProviderType = OrchestratorChat.Saturn.Models.ProviderType;

namespace OrchestratorChat.Agents.Saturn;

/// <summary>
/// Alternative Saturn core implementation providing direct agent operations
/// </summary>
public interface ISaturnCoreOperations
{
    Task<bool> InitializeAsync(OrchestratorChat.Agents.Saturn.SaturnConfiguration config, CancellationToken cancellationToken = default);
    Task<string> ProcessMessageAsync(string message, CancellationToken cancellationToken = default);
    Task<SaturnToolExecutionResult> ExecuteToolAsync(string toolName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);
    Task<List<SaturnToolInfo>> GetAvailableToolsAsync(CancellationToken cancellationToken = default);
    Task<bool> SetWorkingDirectoryAsync(string path, CancellationToken cancellationToken = default);
}

/// <summary>
/// Direct Saturn operations implementation
/// </summary>
public class SaturnCoreOperations : ISaturnCoreOperations, IDisposable
{
    private readonly ILogger<SaturnCoreOperations> _logger;
    private readonly ISaturnCore _saturnCore;
    private ISaturnAgent? _saturnAgent;
    private SaturnILLMProvider? _provider;
    private SaturnToolRegistry? _toolRegistry;
    private bool _isInitialized;
    private string _workingDirectory = string.Empty;

    public SaturnCoreOperations(ILogger<SaturnCoreOperations> logger, ISaturnCore saturnCore)
    {
        _logger = logger;
        _saturnCore = saturnCore;
    }

    public async Task<bool> InitializeAsync(OrchestratorChat.Agents.Saturn.SaturnConfiguration config, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Initializing Saturn core operations");

            // Create LLM provider based on configuration
            var providerType = config.DefaultProvider switch
            {
                "Anthropic" => SaturnProviderType.Anthropic,
                _ => SaturnProviderType.OpenRouter
            };

            var providerSettings = config.ProviderSettings.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
            _provider = await _saturnCore.CreateProviderAsync(providerType, providerSettings);

            // Create Saturn agent configuration
            var agentConfig = new SaturnAgentConfiguration
            {
                Model = config.SupportedModels.FirstOrDefault() ?? "claude-sonnet-4-20250514",
                Temperature = 0.7,
                MaxTokens = 4096,
                SystemPrompt = "You are a helpful AI assistant integrated into the OrchestratorChat system.",
                EnableTools = true,
                RequireApproval = false
            };

            // Create Saturn agent
            _saturnAgent = await _saturnCore.CreateAgentAsync(_provider, agentConfig);

            // Set up tool registry
            _toolRegistry = new SaturnToolRegistry();
            RegisterDefaultTools();

            // Set working directory if provided
            if (!string.IsNullOrEmpty(_workingDirectory))
            {
                await SetWorkingDirectoryInternalAsync(_workingDirectory);
            }

            _isInitialized = true;
            _logger.LogInformation("Saturn core operations initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Saturn core operations");
            return false;
        }
    }

    public async Task<string> ProcessMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized || _saturnAgent == null)
        {
            throw new InvalidOperationException("Saturn core operations not initialized. Call InitializeAsync first.");
        }

        try
        {
            _logger.LogDebug("Processing message: {Message}", message.Substring(0, Math.Min(message.Length, 100)));

            // Create Saturn message
            var saturnMessage = new SaturnAgentMessage
            {
                Content = message,
                Role = MessageRole.User,
                SessionId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>()
            };

            // Process through Saturn agent
            var responseStream = await _saturnAgent.ProcessMessageAsync(saturnMessage, cancellationToken);

            // Collect all responses
            var responseContent = new System.Text.StringBuilder();
            await foreach (var response in responseStream.WithCancellation(cancellationToken))
            {
                if (!string.IsNullOrEmpty(response.Content))
                {
                    responseContent.Append(response.Content);
                }

                if (response.IsComplete)
                {
                    break;
                }
            }

            var finalResponse = responseContent.ToString();
            _logger.LogDebug("Message processed successfully, response length: {Length}", finalResponse.Length);
            return finalResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            throw;
        }
    }

    public async Task<SaturnToolExecutionResult> ExecuteToolAsync(
        string toolName, 
        Dictionary<string, object> parameters, 
        CancellationToken cancellationToken = default)
    {
        if (!_isInitialized || _saturnAgent == null)
        {
            throw new InvalidOperationException("Saturn core operations not initialized. Call InitializeAsync first.");
        }

        try
        {
            _logger.LogDebug("Executing tool: {ToolName} with {ParameterCount} parameters", toolName, parameters.Count);

            // Create Saturn tool call
            var toolCall = new SaturnToolCall
            {
                Id = Guid.NewGuid().ToString(),
                Name = toolName,
                Parameters = parameters,
                Command = parameters.GetValueOrDefault("command", "")?.ToString() ?? ""
            };

            var startTime = DateTime.UtcNow;
            var result = await _saturnAgent.ExecuteToolAsync(toolCall, cancellationToken);
            var executionTime = DateTime.UtcNow - startTime;

            _logger.LogDebug("Tool execution completed: {ToolName}, Success: {Success}", toolName, result.Success);

            return new SaturnToolExecutionResult
            {
                Success = result.Success,
                Output = result.Output ?? string.Empty,
                Error = result.Error ?? string.Empty,
                ExecutionTime = executionTime,
                Metadata = new Dictionary<string, object>
                {
                    { "ToolName", toolName },
                    { "ParameterCount", parameters.Count }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool: {ToolName}", toolName);
            return new SaturnToolExecutionResult
            {
                Success = false,
                Output = string.Empty,
                Error = ex.Message,
                ExecutionTime = TimeSpan.Zero,
                Metadata = new Dictionary<string, object>
                {
                    { "ToolName", toolName },
                    { "Exception", ex.GetType().Name }
                }
            };
        }
    }

    public async Task<List<SaturnToolInfo>> GetAvailableToolsAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // For async interface compliance
        
        if (!_isInitialized)
        {
            _logger.LogWarning("Attempting to get available tools before initialization");
            return new List<SaturnToolInfo>();
        }

        try
        {
            var tools = _saturnCore.GetAvailableTools();
            _logger.LogDebug("Retrieved {ToolCount} available tools", tools.Count);
            return tools;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available tools");
            return new List<SaturnToolInfo>();
        }
    }

    public async Task<bool> SetWorkingDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            _logger.LogWarning("Attempted to set empty or null working directory");
            return false;
        }

        try
        {
            _workingDirectory = path;

            if (_isInitialized && _saturnAgent != null)
            {
                await SetWorkingDirectoryInternalAsync(path);
            }

            _logger.LogDebug("Working directory set to: {WorkingDirectory}", path);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting working directory to: {Path}", path);
            return false;
        }
    }

    private async Task SetWorkingDirectoryInternalAsync(string path)
    {
        // Update Saturn agent's working directory if possible
        // This would depend on Saturn's internal implementation
        // For now, we just store it for future use
        await Task.CompletedTask;
        
        // If Saturn agent has configuration or context that can be updated:
        // await _saturnAgent.UpdateConfigurationAsync(new { WorkingDirectory = path });
    }

    private void RegisterDefaultTools()
    {
        try
        {
            // Register Saturn's built-in tools through the tool registry
            var tools = _saturnCore.GetAvailableTools();
            _logger.LogDebug("Registered {ToolCount} default tools", tools.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering default tools");
        }
    }

    public void Dispose()
    {
        try
        {
            if (_saturnAgent != null)
            {
                // Saturn agents may not implement IDisposable directly
                // Perform any necessary cleanup
                _saturnAgent = null;
            }

            _provider = null;
            _toolRegistry = null;
            _isInitialized = false;

            _logger.LogDebug("Saturn core operations disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Saturn core operations disposal");
        }
    }
}