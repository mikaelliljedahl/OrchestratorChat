using OrchestratorChat.Web.Models;

namespace OrchestratorChat.Web.Services;

/// <summary>
/// Service for verifying provider availability and API key validity
/// </summary>
public interface IProviderVerificationService
{
    /// <summary>
    /// Gets the current status of all providers
    /// </summary>
    /// <returns>Provider status information</returns>
    Task<ProviderStatusResponse> GetProviderStatusAsync();
    
    /// <summary>
    /// Detects if Claude CLI is available
    /// </summary>
    /// <returns>Claude CLI detection result</returns>
    Task<ProviderStatus> DetectClaudeCliAsync();
    
    /// <summary>
    /// Validates and stores OpenRouter API key
    /// </summary>
    /// <param name="apiKey">The API key to validate</param>
    /// <returns>Validation result</returns>
    Task<ValidationResult> ValidateOpenRouterKeyAsync(string apiKey);
    
    /// <summary>
    /// Validates Anthropic API key
    /// </summary>
    /// <param name="apiKey">The API key to validate</param>
    /// <returns>Validation result</returns>
    Task<ValidationResult> ValidateAnthropicKeyAsync(string apiKey);
    
    /// <summary>
    /// Checks if OpenRouter API key is present
    /// </summary>
    /// <returns>OpenRouter key status</returns>
    Task<ProviderStatus> CheckOpenRouterKeyAsync();
    
    /// <summary>
    /// Checks if Anthropic API key is present
    /// </summary>
    /// <returns>Anthropic key status</returns>
    Task<ProviderStatus> CheckAnthropicKeyAsync();
    
    /// <summary>
    /// Checks Anthropic OAuth token status
    /// </summary>
    /// <returns>OAuth token status</returns>
    Task<ProviderStatus> CheckAnthropicOAuthAsync();
    
    /// <summary>
    /// Checks Anthropic provider status (OAuth tokens or API key)
    /// </summary>
    /// <returns>Combined Anthropic provider status</returns>
    Task<ProviderStatus> CheckAnthropicProviderAsync();
}