namespace OrchestratorChat.Web.Models;

/// <summary>
/// Response model for provider status endpoint
/// </summary>
public class ProviderStatusResponse
{
    /// <summary>
    /// Status of Claude CLI detection
    /// </summary>
    public ProviderStatus ClaudeCli { get; set; } = ProviderStatus.NotFound;
    
    /// <summary>
    /// Status of OpenRouter API key
    /// </summary>
    public ProviderStatus OpenRouterKey { get; set; } = ProviderStatus.Missing;
    
    /// <summary>
    /// Status of Anthropic API key
    /// </summary>
    public ProviderStatus AnthropicKey { get; set; } = ProviderStatus.Missing;
    
    /// <summary>
    /// Status of Anthropic OAuth tokens
    /// </summary>
    public ProviderStatus AnthropicOAuth { get; set; } = ProviderStatus.Missing;
}

/// <summary>
/// Status enumeration for providers
/// </summary>
public enum ProviderStatus
{
    /// <summary>
    /// Provider is detected and verified
    /// </summary>
    Detected,
    
    /// <summary>
    /// Provider is not found or not available
    /// </summary>
    NotFound,
    
    /// <summary>
    /// API key is present
    /// </summary>
    Present,
    
    /// <summary>
    /// API key is missing
    /// </summary>
    Missing
}