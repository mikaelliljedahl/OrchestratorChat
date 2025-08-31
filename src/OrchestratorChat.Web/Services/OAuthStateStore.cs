using Microsoft.Extensions.Caching.Memory;
using OrchestratorChat.Core.Authentication;

namespace OrchestratorChat.Web.Services;

/// <summary>
/// Implementation of OAuth state store using IMemoryCache for server-side state storage
/// SECURITY: Enforces one-time use of state tokens and PKCE verifiers to prevent replay attacks
/// </summary>
public class OAuthStateStore : IOAuthStateStore
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<OAuthStateStore> _logger;

    public OAuthStateStore(IMemoryCache memoryCache, ILogger<OAuthStateStore> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    /// <summary>
    /// Stores OAuth state data with expiration for secure server-side storage
    /// SECURITY: State and PKCE verifier are stored server-side only and never exposed to client
    /// </summary>
    public Task StoreAsync(string state, OAuthStateData data, TimeSpan expiration)
    {
        if (string.IsNullOrEmpty(state))
            throw new ArgumentException("State cannot be null or empty", nameof(state));

        if (data == null)
            throw new ArgumentNullException(nameof(data));

        try
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            // Store the state data
            _memoryCache.Set($"oauth_state_{state}", data, cacheOptions);
            
            // Also store a simple boolean flag for backward compatibility
            _memoryCache.Set($"oauth_state_flag_{state}", true, cacheOptions);

            _logger.LogInformation("SECURITY: OAuth state and PKCE verifier stored server-side. State: {State}, Expiry: {ExpiryMinutes} minutes", 
                state, expiration.TotalMinutes);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store OAuth state data for state: {State}", state);
            throw;
        }
    }

    /// <summary>
    /// Retrieves and immediately removes OAuth state data to enforce one-time use
    /// SECURITY: State can only be used once - prevents replay attacks and CSRF
    /// </summary>
    public Task<OAuthStateData?> RetrieveAsync(string state)
    {
        if (string.IsNullOrEmpty(state))
            return Task.FromResult<OAuthStateData?>(null);

        try
        {
            // Try to get the state data
            if (_memoryCache.TryGetValue($"oauth_state_{state}", out OAuthStateData? stateData) && stateData != null)
            {
                // Remove the entries after retrieval (one-time use security enforcement)
                _memoryCache.Remove($"oauth_state_{state}");
                _memoryCache.Remove($"oauth_state_flag_{state}");
                
                _logger.LogInformation("SECURITY: OAuth state retrieved and permanently removed (one-time use). State: {State}, Age: {AgeMinutes:F1} minutes", 
                    state, (DateTime.UtcNow - stateData.CreatedAt).TotalMinutes);
                return Task.FromResult<OAuthStateData?>(stateData);
            }

            _logger.LogWarning("SECURITY: OAuth state retrieval failed - state not found or expired: {State}", state);
            return Task.FromResult<OAuthStateData?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve OAuth state data for state: {State}", state);
            return Task.FromResult<OAuthStateData?>(null);
        }
    }

    /// <summary>
    /// Explicitly removes OAuth state data for cleanup
    /// SECURITY: Ensures no orphaned security tokens remain in memory
    /// </summary>
    public Task RemoveAsync(string state)
    {
        if (string.IsNullOrEmpty(state))
            return Task.CompletedTask;

        try
        {
            _memoryCache.Remove($"oauth_state_{state}");
            _memoryCache.Remove($"oauth_state_flag_{state}");
            
            _logger.LogInformation("SECURITY: OAuth state explicitly removed for cleanup. State: {State}", state);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove OAuth state data for state: {State}", state);
            throw;
        }
    }
}