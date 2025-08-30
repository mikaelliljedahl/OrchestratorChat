namespace OrchestratorChat.Saturn.Providers.OpenRouter;

/// <summary>
/// Configuration options for the OpenRouter API client
/// </summary>
public class OpenRouterOptions
{
    /// <summary>
    /// OpenRouter API key for authentication
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Base URL for the OpenRouter API
    /// </summary>
    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";
    
    /// <summary>
    /// Default model to use when none is specified
    /// </summary>
    public string DefaultModel { get; set; } = "anthropic/claude-3-sonnet-20240229";
    
    /// <summary>
    /// HTTP request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;
    
    /// <summary>
    /// Application name for API attribution
    /// </summary>
    public string AppName { get; set; } = "OrchestratorChat";
    
    /// <summary>
    /// Application version for API attribution
    /// </summary>
    public string AppVersion { get; set; } = "1.0.0";
    
    /// <summary>
    /// HTTP Referer header for requests
    /// </summary>
    public string HttpReferer { get; set; } = "https://github.com/OrchestratorChat";
    
    /// <summary>
    /// X-Title header for requests
    /// </summary>
    public string XTitle { get; set; } = "OrchestratorChat Saturn";
    
    /// <summary>
    /// Maximum number of retry attempts for failed requests
    /// </summary>
    public int MaxRetries { get; set; } = 3;
    
    /// <summary>
    /// Base delay between retry attempts in milliseconds
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;
    
    /// <summary>
    /// Default temperature for model requests
    /// </summary>
    public double DefaultTemperature { get; set; } = 0.7;
    
    /// <summary>
    /// Default maximum tokens for model responses
    /// </summary>
    public int DefaultMaxTokens { get; set; } = 4096;
    
    /// <summary>
    /// Default top-p value for nucleus sampling
    /// </summary>
    public double? DefaultTopP { get; set; } = null;
    
    /// <summary>
    /// Default frequency penalty for reducing repetitive text
    /// </summary>
    public double? DefaultFrequencyPenalty { get; set; } = null;
    
    /// <summary>
    /// Default presence penalty for encouraging topic diversity
    /// </summary>
    public double? DefaultPresencePenalty { get; set; } = null;
    
    /// <summary>
    /// Whether to enable fallback providers by default
    /// </summary>
    public bool EnableFallbacks { get; set; } = true;
    
    /// <summary>
    /// Default data collection preference
    /// </summary>
    public string DataCollection { get; set; } = "allow"; // allow, deny
    
    /// <summary>
    /// Whether to enable streaming responses by default
    /// </summary>
    public bool EnableStreaming { get; set; } = true;
    
    /// <summary>
    /// Whether to enable tool/function calling support
    /// </summary>
    public bool EnableTools { get; set; } = true;
    
    /// <summary>
    /// Validates that required configuration is present
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new InvalidOperationException("OpenRouter API key is required");
            
        if (string.IsNullOrWhiteSpace(BaseUrl))
            throw new InvalidOperationException("Base URL is required");
            
        if (TimeoutSeconds <= 0)
            throw new InvalidOperationException("Timeout must be greater than 0");
            
        if (MaxRetries < 0)
            throw new InvalidOperationException("Max retries cannot be negative");
            
        if (RetryDelayMs < 0)
            throw new InvalidOperationException("Retry delay cannot be negative");
            
        if (DefaultTemperature < 0 || DefaultTemperature > 2)
            throw new InvalidOperationException("Default temperature must be between 0 and 2");
            
        if (DefaultMaxTokens <= 0)
            throw new InvalidOperationException("Default max tokens must be greater than 0");
            
        if (DefaultTopP.HasValue && (DefaultTopP.Value < 0 || DefaultTopP.Value > 1))
            throw new InvalidOperationException("Default top-p must be between 0 and 1");
            
        if (DefaultFrequencyPenalty.HasValue && (DefaultFrequencyPenalty.Value < -2 || DefaultFrequencyPenalty.Value > 2))
            throw new InvalidOperationException("Default frequency penalty must be between -2 and 2");
            
        if (DefaultPresencePenalty.HasValue && (DefaultPresencePenalty.Value < -2 || DefaultPresencePenalty.Value > 2))
            throw new InvalidOperationException("Default presence penalty must be between -2 and 2");
            
        if (!string.IsNullOrWhiteSpace(DataCollection) && DataCollection != "allow" && DataCollection != "deny")
            throw new InvalidOperationException("Data collection must be 'allow' or 'deny'");
    }
}