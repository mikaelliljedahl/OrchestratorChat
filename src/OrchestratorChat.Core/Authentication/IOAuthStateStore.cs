namespace OrchestratorChat.Core.Authentication;

/// <summary>
/// Interface for storing and retrieving OAuth state and PKCE data during authentication flow
/// SECURITY: Enforces server-side storage and one-time use of security tokens
/// </summary>
public interface IOAuthStateStore
{
    /// <summary>
    /// Stores OAuth state data with expiration
    /// </summary>
    /// <param name="state">State token</param>
    /// <param name="data">OAuth state data to store</param>
    /// <param name="expiration">How long to keep the data</param>
    /// <returns>Task representing the operation</returns>
    Task StoreAsync(string state, OAuthStateData data, TimeSpan expiration);
    
    /// <summary>
    /// Retrieves and removes OAuth state data (one-time use security enforcement)
    /// SECURITY: State tokens can only be used once to prevent replay attacks
    /// </summary>
    /// <param name="state">State token</param>
    /// <returns>OAuth state data if found, null otherwise</returns>
    Task<OAuthStateData?> RetrieveAsync(string state);
    
    /// <summary>
    /// Removes OAuth state data
    /// </summary>
    /// <param name="state">State token</param>
    /// <returns>Task representing the operation</returns>
    Task RemoveAsync(string state);
}

/// <summary>
/// OAuth state data stored during authentication flow
/// SECURITY: Contains server-generated PKCE verifier - never expose to client
/// </summary>
public class OAuthStateData
{
    /// <summary>
    /// PKCE code verifier - SECURITY: Server-generated only, never accept client-provided values
    /// </summary>
    public string PkceVerifier { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp when state was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}