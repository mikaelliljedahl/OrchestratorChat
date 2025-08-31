using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using OrchestratorChat.Web.Models;
using OrchestratorChat.Saturn.Providers.Anthropic;

namespace OrchestratorChat.Web.Services;

/// <summary>
/// Service for verifying provider availability and API key validity
/// </summary>
public class ProviderVerificationService : IProviderVerificationService
{
    private readonly OrchestratorChat.Core.Configuration.IConfigurationProvider _configurationProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProviderVerificationService> _logger;
    private readonly ITokenStore _tokenStore;
    
    private const string OpenRouterApiKeyConfigKey = "OpenRouter:ApiKey";
    private const string AnthropicApiKeyConfigKey = "Anthropic:ApiKey";
    private const string OpenRouterModelsEndpoint = "https://openrouter.ai/api/v1/models";
    
    public ProviderVerificationService(
        OrchestratorChat.Core.Configuration.IConfigurationProvider configurationProvider,
        IHttpClientFactory httpClientFactory,
        ILogger<ProviderVerificationService> logger,
        ITokenStore tokenStore)
    {
        _configurationProvider = configurationProvider;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _tokenStore = tokenStore;
    }
    
    public async Task<ProviderStatusResponse> GetProviderStatusAsync()
    {
        var claudeCliTask = DetectClaudeCliAsync();
        var openRouterTask = CheckOpenRouterKeyAsync();
        var anthropicKeyTask = CheckAnthropicKeyAsync();
        var anthropicOAuthTask = CheckAnthropicOAuthAsync();
        
        await Task.WhenAll(claudeCliTask, openRouterTask, anthropicKeyTask, anthropicOAuthTask);
        
        return new ProviderStatusResponse
        {
            ClaudeCli = claudeCliTask.Result,
            OpenRouterKey = openRouterTask.Result,
            AnthropicKey = anthropicKeyTask.Result,
            AnthropicOAuth = anthropicOAuthTask.Result
        };
    }
    
    public async Task<ProviderStatus> DetectClaudeCliAsync()
    {
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = "claude";
            process.StartInfo.Arguments = "--version";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            
            var timeoutCancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            
            process.Start();
            
            await process.WaitForExitAsync(timeoutCancellationToken.Token);
            
            return process.ExitCode == 0 ? ProviderStatus.Detected : ProviderStatus.NotFound;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Claude CLI detection failed: {Message}", ex.Message);
            return ProviderStatus.NotFound;
        }
    }
    
    public async Task<ValidationResult> ValidateOpenRouterKeyAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new ValidationResult
            {
                IsValid = false,
                Status = ProviderStatus.Missing,
                ErrorMessage = "API key cannot be empty"
            };
        }
        
        try
        {
            // Store the API key securely
            await _configurationProvider.SetAsync(OpenRouterApiKeyConfigKey, apiKey);
            
            // Optional network validation
            var networkValidation = await ValidateOpenRouterNetworkAsync(apiKey);
            
            if (networkValidation)
            {
                return new ValidationResult
                {
                    IsValid = true,
                    Status = ProviderStatus.Present
                };
            }
            else
            {
                // Key stored but network validation failed - still consider it present
                _logger.LogWarning("OpenRouter API key stored but network validation failed");
                return new ValidationResult
                {
                    IsValid = true,
                    Status = ProviderStatus.Present,
                    ErrorMessage = "API key stored but network validation failed"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate OpenRouter API key");
            return new ValidationResult
            {
                IsValid = false,
                Status = ProviderStatus.Missing,
                ErrorMessage = "Failed to validate API key"
            };
        }
    }
    
    public async Task<ValidationResult> ValidateAnthropicKeyAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new ValidationResult
            {
                IsValid = false,
                Status = ProviderStatus.Missing,
                ErrorMessage = "API key cannot be empty"
            };
        }
        
        try
        {
            // Store the API key securely
            await _configurationProvider.SetAsync(AnthropicApiKeyConfigKey, apiKey);
            
            // For Anthropic, we only do presence check as specified
            return new ValidationResult
            {
                IsValid = true,
                Status = ProviderStatus.Present
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store Anthropic API key");
            return new ValidationResult
            {
                IsValid = false,
                Status = ProviderStatus.Missing,
                ErrorMessage = "Failed to store API key"
            };
        }
    }
    
    public async Task<ProviderStatus> CheckOpenRouterKeyAsync()
    {
        try
        {
            // Check stored configuration first
            var storedKey = await _configurationProvider.GetAsync<string>(OpenRouterApiKeyConfigKey);
            if (!string.IsNullOrWhiteSpace(storedKey))
            {
                return ProviderStatus.Present;
            }
            
            // Check environment variable
            var envKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
            if (!string.IsNullOrWhiteSpace(envKey))
            {
                return ProviderStatus.Present;
            }
            
            return ProviderStatus.Missing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check OpenRouter API key status");
            return ProviderStatus.Missing;
        }
    }
    
    public async Task<ProviderStatus> CheckAnthropicKeyAsync()
    {
        try
        {
            // Check stored configuration first
            var storedKey = await _configurationProvider.GetAsync<string>(AnthropicApiKeyConfigKey);
            if (!string.IsNullOrWhiteSpace(storedKey))
            {
                return ProviderStatus.Present;
            }
            
            // Check environment variable
            var envKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
            if (!string.IsNullOrWhiteSpace(envKey))
            {
                return ProviderStatus.Present;
            }
            
            return ProviderStatus.Missing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check Anthropic API key status");
            return ProviderStatus.Missing;
        }
    }
    
    /// <summary>
    /// Checks Anthropic OAuth token status
    /// </summary>
    /// <returns>OAuth token status</returns>
    public async Task<ProviderStatus> CheckAnthropicOAuthAsync()
    {
        try
        {
            var tokens = await _tokenStore.LoadTokensAsync();
            
            if (tokens == null)
            {
                return ProviderStatus.Missing;
            }
            
            // Check if tokens are expired and can't be refreshed
            if (tokens.IsExpired && string.IsNullOrEmpty(tokens.RefreshToken))
            {
                return ProviderStatus.Missing;
            }
            
            return ProviderStatus.Present;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check Anthropic OAuth status");
            return ProviderStatus.Missing;
        }
    }
    
    /// <summary>
    /// Checks Anthropic provider status (OAuth tokens or API key)
    /// </summary>
    /// <returns>Combined Anthropic provider status</returns>
    public async Task<ProviderStatus> CheckAnthropicProviderAsync()
    {
        try
        {
            // First check OAuth tokens
            var oauthStatus = await CheckAnthropicOAuthAsync();
            if (oauthStatus == ProviderStatus.Present)
            {
                return ProviderStatus.Present;
            }
            
            // Fall back to API key check
            return await CheckAnthropicKeyAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check Anthropic provider status");
            return ProviderStatus.Missing;
        }
    }
    
    private async Task<bool> ValidateOpenRouterNetworkAsync(string apiKey)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            
            using var request = new HttpRequestMessage(HttpMethod.Head, OpenRouterModelsEndpoint);
            request.Headers.Add("Authorization", $"Bearer {apiKey}");
            
            using var response = await httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("OpenRouter network validation failed: {Message}", ex.Message);
            return false;
        }
    }
}