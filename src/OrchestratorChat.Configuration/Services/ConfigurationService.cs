using System.Text.Json;
using Microsoft.Extensions.Configuration;
using OrchestratorChat.Core.Configuration;

namespace OrchestratorChat.Configuration.Services;

public class ConfigurationService : Core.Configuration.IConfigurationProvider
{
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, object> _memoryCache = new();
    private readonly string _configurationFilePath;

    public ConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration;
        _configurationFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OrchestratorChat", "config.json");
        
        EnsureConfigurationDirectory();
        LoadFromFile();
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        await Task.CompletedTask; // For interface compatibility
        
        // First try memory cache
        if (_memoryCache.TryGetValue(key, out var cachedValue))
        {
            if (cachedValue is T typedValue)
                return typedValue;
            
            // Try to deserialize if it's a string
            if (cachedValue is string jsonString)
            {
                try
                {
                    return JsonSerializer.Deserialize<T>(jsonString);
                }
                catch
                {
                    // Fall through to configuration provider
                }
            }
        }
        
        // Try configuration provider
        var configValue = _configuration[key];
        if (configValue != null)
        {
            if (typeof(T) == typeof(string))
                return configValue as T;
            
            try
            {
                return JsonSerializer.Deserialize<T>(configValue);
            }
            catch
            {
                // Return null if deserialization fails
            }
        }
        
        return null;
    }

    public async Task SetAsync<T>(string key, T value) where T : class
    {
        await Task.CompletedTask; // For interface compatibility
        
        if (value == null)
        {
            _memoryCache.Remove(key);
        }
        else
        {
            _memoryCache[key] = value;
        }
        
        await SaveToFileAsync();
    }

    public async Task<bool> ExistsAsync(string key)
    {
        await Task.CompletedTask; // For interface compatibility
        
        return _memoryCache.ContainsKey(key) || _configuration[key] != null;
    }

    public async Task DeleteAsync(string key)
    {
        await Task.CompletedTask; // For interface compatibility
        
        _memoryCache.Remove(key);
        await SaveToFileAsync();
    }

    public async Task<Dictionary<string, object>> GetAllAsync()
    {
        await Task.CompletedTask; // For interface compatibility
        
        var result = new Dictionary<string, object>(_memoryCache);
        
        // Add configuration values that aren't in memory cache
        foreach (var item in _configuration.AsEnumerable())
        {
            if (!string.IsNullOrEmpty(item.Key) && !result.ContainsKey(item.Key))
            {
                result[item.Key] = item.Value ?? string.Empty;
            }
        }
        
        return result;
    }

    private void EnsureConfigurationDirectory()
    {
        var directory = Path.GetDirectoryName(_configurationFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private void LoadFromFile()
    {
        if (!File.Exists(_configurationFilePath))
            return;
        
        try
        {
            var json = File.ReadAllText(_configurationFilePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            
            if (data != null)
            {
                foreach (var kvp in data)
                {
                    _memoryCache[kvp.Key] = kvp.Value.GetRawText();
                }
            }
        }
        catch
        {
            // Ignore errors when loading configuration
        }
    }

    private async Task SaveToFileAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_memoryCache, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            await File.WriteAllTextAsync(_configurationFilePath, json);
        }
        catch
        {
            // Ignore errors when saving configuration
        }
    }
}