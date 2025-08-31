namespace OrchestratorChat.Saturn.Providers;

/// <summary>
/// Strongly-typed settings for provider creation
/// </summary>
public abstract class ProviderSettings
{
    /// <summary>
    /// Provider type identifier
    /// </summary>
    public abstract string ProviderType { get; }
}

/// <summary>
/// Settings for Anthropic provider
/// </summary>
public class AnthropicProviderSettings : ProviderSettings
{
    /// <summary>
    /// Provider type identifier
    /// </summary>
    public override string ProviderType => "anthropic";
    
    /// <summary>
    /// OAuth access token (preferred over API key)
    /// </summary>
    public string? OAuthToken { get; set; }
    
    /// <summary>
    /// API key for Anthropic (fallback if OAuth not available)
    /// </summary>
    public string? ApiKey { get; set; }
}

/// <summary>
/// Settings for OpenRouter provider
/// </summary>
public class OpenRouterProviderSettings : ProviderSettings
{
    /// <summary>
    /// Provider type identifier
    /// </summary>
    public override string ProviderType => "openrouter";
    
    /// <summary>
    /// API key for OpenRouter
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional site URL for OpenRouter requests
    /// </summary>
    public string? SiteUrl { get; set; }
    
    /// <summary>
    /// Optional site name for OpenRouter requests
    /// </summary>
    public string? SiteName { get; set; }
}

/// <summary>
/// Settings for OpenAI provider
/// </summary>
public class OpenAIProviderSettings : ProviderSettings
{
    /// <summary>
    /// Provider type identifier
    /// </summary>
    public override string ProviderType => "openai";
    
    /// <summary>
    /// API key for OpenAI
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional organization ID for OpenAI
    /// </summary>
    public string? OrganizationId { get; set; }
}