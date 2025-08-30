namespace OrchestratorChat.Saturn.Providers.Anthropic;

public class StoredTokens
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? TokenType { get; set; } = "Bearer";
    public string[]? Scope { get; set; }
    
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool NeedsRefresh => DateTime.UtcNow >= ExpiresAt.AddMinutes(-5);
}