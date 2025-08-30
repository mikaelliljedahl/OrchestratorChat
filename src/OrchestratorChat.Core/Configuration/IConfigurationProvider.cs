namespace OrchestratorChat.Core.Configuration;

/// <summary>
/// Interface for accessing and managing configuration data
/// </summary>
public interface IConfigurationProvider
{
    /// <summary>
    /// Get a configuration value by key
    /// </summary>
    /// <typeparam name="T">Type of the configuration value</typeparam>
    /// <param name="key">Configuration key</param>
    /// <returns>The configuration value if found, null otherwise</returns>
    Task<T> GetAsync<T>(string key) where T : class;
    
    /// <summary>
    /// Set a configuration value
    /// </summary>
    /// <typeparam name="T">Type of the configuration value</typeparam>
    /// <param name="key">Configuration key</param>
    /// <param name="value">Value to set</param>
    /// <returns>Task representing the async operation</returns>
    Task SetAsync<T>(string key, T value) where T : class;
    
    /// <summary>
    /// Check if a configuration key exists
    /// </summary>
    /// <param name="key">Configuration key to check</param>
    /// <returns>True if the key exists, false otherwise</returns>
    Task<bool> ExistsAsync(string key);
    
    /// <summary>
    /// Delete a configuration value
    /// </summary>
    /// <param name="key">Configuration key to delete</param>
    /// <returns>Task representing the async operation</returns>
    Task DeleteAsync(string key);
    
    /// <summary>
    /// Get all configuration values
    /// </summary>
    /// <returns>Dictionary of all configuration key-value pairs</returns>
    Task<Dictionary<string, object>> GetAllAsync();
}