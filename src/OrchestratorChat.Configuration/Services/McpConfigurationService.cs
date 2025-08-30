using System.Text.Json;
using OrchestratorChat.Configuration.Models;
using OrchestratorChat.Core.Configuration;

namespace OrchestratorChat.Configuration.Services;

/// <summary>
/// Service for managing MCP (Model Context Protocol) configurations
/// </summary>
public class McpConfigurationService
{
    private readonly Core.Configuration.IConfigurationProvider _configurationProvider;
    private const string MCP_CONFIG_KEY = "mcp_configurations";
    
    public McpConfigurationService(Core.Configuration.IConfigurationProvider configurationProvider)
    {
        _configurationProvider = configurationProvider;
    }
    
    /// <summary>
    /// Get all MCP configurations
    /// </summary>
    public async Task<List<McpConfiguration>> GetAllAsync()
    {
        var configurationsJson = await _configurationProvider.GetAsync<string>(MCP_CONFIG_KEY);
        
        if (string.IsNullOrEmpty(configurationsJson))
            return new List<McpConfiguration>();
        
        try
        {
            return JsonSerializer.Deserialize<List<McpConfiguration>>(configurationsJson) 
                   ?? new List<McpConfiguration>();
        }
        catch
        {
            return new List<McpConfiguration>();
        }
    }
    
    /// <summary>
    /// Get MCP configuration by name
    /// </summary>
    public async Task<McpConfiguration?> GetByNameAsync(string name)
    {
        var configurations = await GetAllAsync();
        return configurations.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Add or update an MCP configuration
    /// </summary>
    public async Task<bool> SaveAsync(McpConfiguration configuration)
    {
        if (string.IsNullOrEmpty(configuration.Name))
            return false;
        
        var configurations = await GetAllAsync();
        
        // Update existing or add new
        var existingIndex = configurations.FindIndex(c => 
            c.Name.Equals(configuration.Name, StringComparison.OrdinalIgnoreCase));
        
        configuration.ModifiedAt = DateTime.UtcNow;
        
        if (existingIndex >= 0)
        {
            configurations[existingIndex] = configuration;
        }
        else
        {
            configuration.CreatedAt = DateTime.UtcNow;
            configurations.Add(configuration);
        }
        
        return await SaveAllAsync(configurations);
    }
    
    /// <summary>
    /// Delete an MCP configuration by name
    /// </summary>
    public async Task<bool> DeleteAsync(string name)
    {
        var configurations = await GetAllAsync();
        var removed = configurations.RemoveAll(c => 
            c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        
        if (removed == 0)
            return false;
        
        return await SaveAllAsync(configurations);
    }
    
    /// <summary>
    /// Get all enabled MCP configurations
    /// </summary>
    public async Task<List<McpConfiguration>> GetEnabledAsync()
    {
        var configurations = await GetAllAsync();
        return configurations.Where(c => c.Enabled).ToList();
    }
    
    /// <summary>
    /// Get MCP configurations by tag
    /// </summary>
    public async Task<List<McpConfiguration>> GetByTagAsync(string tag)
    {
        var configurations = await GetAllAsync();
        return configurations.Where(c => 
            c.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)).ToList();
    }
    
    /// <summary>
    /// Enable or disable an MCP configuration
    /// </summary>
    public async Task<bool> SetEnabledAsync(string name, bool enabled)
    {
        var configuration = await GetByNameAsync(name);
        if (configuration == null)
            return false;
        
        configuration.Enabled = enabled;
        configuration.ModifiedAt = DateTime.UtcNow;
        
        return await SaveAsync(configuration);
    }
    
    /// <summary>
    /// Import MCP configurations from JSON
    /// </summary>
    public async Task<int> ImportAsync(string json, bool overwriteExisting = false)
    {
        try
        {
            var importConfigurations = JsonSerializer.Deserialize<List<McpConfiguration>>(json);
            if (importConfigurations == null)
                return 0;
            
            var existingConfigurations = await GetAllAsync();
            var importedCount = 0;
            
            foreach (var config in importConfigurations)
            {
                if (string.IsNullOrEmpty(config.Name))
                    continue;
                
                var exists = existingConfigurations.Any(c => 
                    c.Name.Equals(config.Name, StringComparison.OrdinalIgnoreCase));
                
                if (!exists || overwriteExisting)
                {
                    config.CreatedAt = DateTime.UtcNow;
                    config.ModifiedAt = DateTime.UtcNow;
                    
                    await SaveAsync(config);
                    importedCount++;
                }
            }
            
            return importedCount;
        }
        catch
        {
            return 0;
        }
    }
    
    /// <summary>
    /// Export MCP configurations to JSON
    /// </summary>
    public async Task<string> ExportAsync()
    {
        var configurations = await GetAllAsync();
        return JsonSerializer.Serialize(configurations, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
    
    /// <summary>
    /// Create a default MCP configuration template
    /// </summary>
    public McpConfiguration CreateTemplate(string name)
    {
        return new McpConfiguration
        {
            Name = name,
            Description = $"MCP configuration for {name}",
            Command = "node",
            Arguments = new List<string> { "server.js" },
            Environment = new Dictionary<string, string>(),
            Enabled = true,
            TimeoutMs = 30000,
            MaxRetries = 3,
            Tags = new List<string> { "default" }
        };
    }
    
    private async Task<bool> SaveAllAsync(List<McpConfiguration> configurations)
    {
        try
        {
            var json = JsonSerializer.Serialize(configurations, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            await _configurationProvider.SetAsync(MCP_CONFIG_KEY, json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}