using Microsoft.Extensions.Logging;
using OrchestratorChat.Core.Exceptions;
using OrchestratorChat.Core.Tools;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrchestratorChat.Agents.Tools.Handlers;

/// <summary>
/// Handles web search operations
/// </summary>
public class WebSearchHandler : IToolHandler
{
    private readonly ILogger<WebSearchHandler> _logger;
    private readonly HttpClient _httpClient;

    public WebSearchHandler(ILogger<WebSearchHandler> logger, HttpClient httpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public string ToolName => "web_search";

    /// <inheritdoc />
    public async Task<ToolExecutionResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = GetRequiredParameter<string>(parameters, "query");
            
            // Optional parameters
            var maxResults = GetOptionalParameter<int>(parameters, "max_results", 10);
            var region = GetOptionalParameter<string>(parameters, "region", "us-en");
            var safesearch = GetOptionalParameter<string>(parameters, "safesearch", "moderate");

            _logger.LogDebug("Performing web search for: {Query}", query);

            // Note: This is a placeholder implementation
            // In a real implementation, you would integrate with a search API like:
            // - DuckDuckGo API
            // - Bing Web Search API
            // - Google Custom Search API
            // - Serper API
            
            // For now, return a mock response indicating the search would be performed
            var mockResults = new List<SearchResult>
            {
                new()
                {
                    Title = $"Search results for: {query}",
                    Url = "https://example.com/search",
                    Snippet = "This is a placeholder for web search functionality. " +
                             "To implement actual web search, integrate with a search API provider.",
                    DisplayUrl = "example.com"
                }
            };

            var searchResponse = new SearchResponse
            {
                Query = query,
                Results = mockResults,
                TotalResults = mockResults.Count,
                SearchTime = TimeSpan.FromMilliseconds(150)
            };

            var jsonOutput = JsonSerializer.Serialize(searchResponse, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return new ToolExecutionResult
            {
                Success = true,
                Output = jsonOutput,
                Metadata = new Dictionary<string, object>
                {
                    ["query"] = query,
                    ["results_count"] = mockResults.Count,
                    ["region"] = region,
                    ["safesearch"] = safesearch,
                    ["max_results"] = maxResults,
                    ["search_provider"] = "placeholder"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error performing web search");
            throw new ToolExecutionException($"Failed to perform web search: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public ValidationResult ValidateParameters(Dictionary<string, object> parameters)
    {
        var errors = new List<string>();

        // Validate query
        if (!parameters.ContainsKey("query") || parameters["query"] == null)
        {
            errors.Add("Parameter 'query' is required");
        }
        else if (parameters["query"] is not string query || string.IsNullOrWhiteSpace(query))
        {
            errors.Add("Parameter 'query' must be a non-empty string");
        }

        // Validate optional parameters
        if (parameters.ContainsKey("max_results"))
        {
            if (parameters["max_results"] is not int maxResults || maxResults <= 0)
            {
                errors.Add("Parameter 'max_results' must be a positive integer");
            }
            else if (maxResults > 100)
            {
                errors.Add("Parameter 'max_results' cannot exceed 100");
            }
        }

        if (parameters.ContainsKey("region") && 
            parameters["region"] is not null and not string)
        {
            errors.Add("Parameter 'region' must be a string");
        }

        if (parameters.ContainsKey("safesearch"))
        {
            if (parameters["safesearch"] is not string safesearch)
            {
                errors.Add("Parameter 'safesearch' must be a string");
            }
            else if (!IsValidSafeSearchValue(safesearch))
            {
                errors.Add("Parameter 'safesearch' must be one of: strict, moderate, off");
            }
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors.ToArray());
    }

    private static bool IsValidSafeSearchValue(string value)
    {
        var validValues = new[] { "strict", "moderate", "off" };
        return validValues.Contains(value.ToLowerInvariant());
    }

    private static T GetRequiredParameter<T>(Dictionary<string, object> parameters, string paramName)
    {
        if (!parameters.TryGetValue(paramName, out var value) || value == null)
        {
            throw new ToolExecutionException($"Required parameter '{paramName}' is missing");
        }

        if (value is not T typedValue)
        {
            throw new ToolExecutionException($"Parameter '{paramName}' must be of type {typeof(T).Name}");
        }

        return typedValue;
    }

    private static T GetOptionalParameter<T>(Dictionary<string, object> parameters, string paramName, T defaultValue)
    {
        if (!parameters.TryGetValue(paramName, out var value) || value == null)
        {
            return defaultValue;
        }

        if (value is not T typedValue)
        {
            throw new ToolExecutionException($"Parameter '{paramName}' must be of type {typeof(T).Name}");
        }

        return typedValue;
    }
}

/// <summary>
/// Represents a single search result
/// </summary>
public class SearchResult
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("snippet")]
    public string Snippet { get; set; } = string.Empty;

    [JsonPropertyName("displayUrl")]
    public string DisplayUrl { get; set; } = string.Empty;
}

/// <summary>
/// Represents the complete search response
/// </summary>
public class SearchResponse
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    [JsonPropertyName("results")]
    public List<SearchResult> Results { get; set; } = new();

    [JsonPropertyName("totalResults")]
    public int TotalResults { get; set; }

    [JsonPropertyName("searchTime")]
    public TimeSpan SearchTime { get; set; }
}