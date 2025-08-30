using OrchestratorChat.Saturn.Models;
using System.Text.Json.Serialization;

namespace OrchestratorChat.Saturn.Core;

/// <summary>
/// Saturn configuration settings
/// </summary>
public class SaturnConfiguration
{
    [JsonPropertyName("providers")]
    public Dictionary<string, ProviderConfiguration> Providers { get; set; } = new();

    [JsonPropertyName("defaultConfiguration")]
    public SaturnAgentConfiguration DefaultConfiguration { get; set; } = new();

    [JsonPropertyName("tools")]
    public ToolConfiguration Tools { get; set; } = new();

    [JsonPropertyName("multiAgent")]
    public MultiAgentConfiguration MultiAgent { get; set; } = new();
}

/// <summary>
/// Provider-specific configuration
/// </summary>
public class ProviderConfiguration
{
    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = string.Empty;

    [JsonPropertyName("defaultModel")]
    public string DefaultModel { get; set; } = string.Empty;

    [JsonPropertyName("settings")]
    public Dictionary<string, object> Settings { get; set; } = new();
}

/// <summary>
/// Tool-specific configuration
/// </summary>
public class ToolConfiguration
{
    [JsonPropertyName("enabled")]
    public List<string> Enabled { get; set; } = new();

    [JsonPropertyName("requireApproval")]
    public List<string> RequireApproval { get; set; } = new();
}

/// <summary>
/// Multi-agent configuration
/// </summary>
public class MultiAgentConfiguration
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("maxConcurrentAgents")]
    public int MaxConcurrentAgents { get; set; } = 5;
}