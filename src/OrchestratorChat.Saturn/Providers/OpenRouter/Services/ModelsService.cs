using Microsoft.Extensions.Logging;
using OrchestratorChat.Saturn.Providers.OpenRouter.Models;

namespace OrchestratorChat.Saturn.Providers.OpenRouter.Services;

/// <summary>
/// Service for OpenRouter models API operations
/// </summary>
public class ModelsService
{
    private readonly HttpClientAdapter _httpClient;
    private readonly ILogger<ModelsService> _logger;
    private const string MODELS_ENDPOINT = "/models";
    
    // Cache for models list to avoid frequent API calls
    private readonly Dictionary<string, (DateTime CachedAt, List<ModelInfo> Models)> _modelCache = new();
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(15);
    private readonly object _cacheLock = new object();

    public ModelsService(HttpClientAdapter httpClient, ILogger<ModelsService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all available models from OpenRouter
    /// </summary>
    public async Task<List<ModelInfo>> GetModelsAsync(
        bool useCache = true,
        CancellationToken cancellationToken = default)
    {
        // Check cache first if enabled
        if (useCache)
        {
            lock (_cacheLock)
            {
                if (_modelCache.TryGetValue("all", out var cached) &&
                    DateTime.UtcNow - cached.CachedAt < _cacheExpiry)
                {
                    _logger.LogDebug("Returning cached models list with {Count} models", cached.Models.Count);
                    return cached.Models;
                }
            }
        }

        _logger.LogDebug("Fetching models from OpenRouter API");

        try
        {
            var response = await _httpClient.GetAsync<ModelListResponse>(MODELS_ENDPOINT, cancellationToken);
            var models = response.Data ?? new List<ModelInfo>();
            
            _logger.LogInformation("Retrieved {Count} models from OpenRouter API", models.Count);

            // Update cache
            if (useCache)
            {
                lock (_cacheLock)
                {
                    _modelCache["all"] = (DateTime.UtcNow, models);
                }
                _logger.LogDebug("Updated models cache with {Count} models", models.Count);
            }

            return models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch models from OpenRouter API");
            
            // Return cached data if available during error
            if (useCache)
            {
                lock (_cacheLock)
                {
                    if (_modelCache.TryGetValue("all", out var cached))
                    {
                        _logger.LogWarning("Returning stale cached models due to API error");
                        return cached.Models;
                    }
                }
            }
            
            throw;
        }
    }

    /// <summary>
    /// Gets information about a specific model by ID
    /// </summary>
    public async Task<ModelInfo?> GetModelAsync(
        string modelId,
        bool useCache = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model ID cannot be empty", nameof(modelId));

        _logger.LogDebug("Getting model information for: {ModelId}", modelId);

        try
        {
            // Try to find in cached models first
            if (useCache)
            {
                var allModels = await GetModelsAsync(useCache, cancellationToken);
                var cachedModel = allModels.FirstOrDefault(m => m.Id == modelId);
                if (cachedModel != null)
                {
                    _logger.LogDebug("Found model {ModelId} in cached models list", modelId);
                    return cachedModel;
                }
            }

            // If not found in cache or cache disabled, try direct API call
            var model = await _httpClient.GetAsync<ModelInfo>($"{MODELS_ENDPOINT}/{modelId}", cancellationToken);
            
            _logger.LogDebug("Retrieved model information for {ModelId}: {ModelName}", 
                modelId, model?.Name ?? "Unknown");
            
            return model;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get model information for {ModelId}", modelId);
            throw;
        }
    }

    /// <summary>
    /// Gets models filtered by provider
    /// </summary>
    public async Task<List<ModelInfo>> GetModelsByProviderAsync(
        string provider,
        bool useCache = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider cannot be empty", nameof(provider));

        _logger.LogDebug("Getting models for provider: {Provider}", provider);

        var allModels = await GetModelsAsync(useCache, cancellationToken);
        
        // Filter models by provider (check if model ID starts with provider name)
        var providerModels = allModels
            .Where(m => m.Id.StartsWith($"{provider}/", StringComparison.OrdinalIgnoreCase))
            .ToList();

        _logger.LogDebug("Found {Count} models for provider {Provider}", providerModels.Count, provider);

        return providerModels;
    }

    /// <summary>
    /// Gets models that support specific features
    /// </summary>
    public async Task<List<ModelInfo>> GetModelsByCapabilityAsync(
        ModelCapability capability,
        bool useCache = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting models with capability: {Capability}", capability);

        var allModels = await GetModelsAsync(useCache, cancellationToken);
        
        var capableModels = allModels.Where(model =>
        {
            return capability switch
            {
                ModelCapability.TextGeneration => true, // All models support text generation
                ModelCapability.FunctionCalling => HasFunctionCallingSupport(model),
                ModelCapability.Vision => HasVisionSupport(model),
                ModelCapability.LongContext => model.ContextLength > 32000,
                ModelCapability.Reasoning => HasReasoningSupport(model),
                _ => false
            };
        }).ToList();

        _logger.LogDebug("Found {Count} models with capability {Capability}", capableModels.Count, capability);

        return capableModels;
    }

    /// <summary>
    /// Gets the most cost-effective models (lowest cost per token)
    /// </summary>
    public async Task<List<ModelInfo>> GetCostEffectiveModelsAsync(
        int maxResults = 10,
        bool useCache = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting {MaxResults} most cost-effective models", maxResults);

        var allModels = await GetModelsAsync(useCache, cancellationToken);
        
        var costEffectiveModels = allModels
            .Where(m => m.Pricing?.Prompt != null && decimal.TryParse(m.Pricing.Prompt, out _))
            .OrderBy(m => decimal.Parse(m.Pricing!.Prompt!))
            .Take(maxResults)
            .ToList();

        _logger.LogDebug("Found {Count} cost-effective models", costEffectiveModels.Count);

        return costEffectiveModels;
    }

    /// <summary>
    /// Searches for models by name or description
    /// </summary>
    public async Task<List<ModelInfo>> SearchModelsAsync(
        string searchTerm,
        bool useCache = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new List<ModelInfo>();

        _logger.LogDebug("Searching models with term: {SearchTerm}", searchTerm);

        var allModels = await GetModelsAsync(useCache, cancellationToken);
        
        var searchResults = allModels
            .Where(m => 
                m.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                m.Id.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (m.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false))
            .OrderBy(m => m.Name)
            .ToList();

        _logger.LogDebug("Found {Count} models matching search term '{SearchTerm}'", searchResults.Count, searchTerm);

        return searchResults;
    }

    /// <summary>
    /// Clears the models cache
    /// </summary>
    public void ClearCache()
    {
        lock (_cacheLock)
        {
            _modelCache.Clear();
            _logger.LogDebug("Models cache cleared");
        }
    }

    /// <summary>
    /// Checks if a model supports function calling based on its metadata
    /// </summary>
    private static bool HasFunctionCallingSupport(ModelInfo model)
    {
        // OpenRouter doesn't have a specific field for this, so we infer from model name/family
        var supportedFamilies = new[] { "gpt-4", "gpt-3.5", "claude-3", "gemini" };
        return supportedFamilies.Any(family => 
            model.Id.Contains(family, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if a model supports vision/multimodal capabilities
    /// </summary>
    private static bool HasVisionSupport(ModelInfo model)
    {
        return model.Architecture?.Modality?.Equals("multimodal", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    /// <summary>
    /// Checks if a model has reasoning capabilities
    /// </summary>
    private static bool HasReasoningSupport(ModelInfo model)
    {
        // Look for reasoning indicators in model name or architecture
        var reasoningKeywords = new[] { "reasoning", "o1", "o3" };
        return reasoningKeywords.Any(keyword => 
            model.Id.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            model.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Model capabilities for filtering
/// </summary>
public enum ModelCapability
{
    TextGeneration,
    FunctionCalling,
    Vision,
    LongContext,
    Reasoning
}