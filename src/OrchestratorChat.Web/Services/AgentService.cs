using System.Text.Json;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Agents;
using OrchestratorChat.Agents.Claude;
using OrchestratorChat.Agents.Saturn;
using OrchestratorChat.Data.Repositories;
using OrchestratorChat.Data.Models;

namespace OrchestratorChat.Web.Services;

public class AgentService : IAgentService
{
    private readonly IAgentFactory _agentFactory;
    private readonly IAgentRepository _agentRepository;
    private readonly IAgentRegistry _agentRegistry;

    public AgentService(IAgentFactory agentFactory, IAgentRepository agentRepository, IAgentRegistry agentRegistry)
    {
        _agentFactory = agentFactory;
        _agentRepository = agentRepository;
        _agentRegistry = agentRegistry;
    }


    public async Task<List<AgentInfo>> GetConfiguredAgentsAsync()
    {
        var agents = await _agentRepository.GetActiveAgentsAsync();
        var agentInfos = new List<AgentInfo>();
        
        foreach (var agent in agents)
        {
            var agentInfo = await MapEntityToAgentInfo(agent);
            agentInfos.Add(agentInfo);
        }

        return agentInfos;
    }

    public async Task<AgentInfo?> GetAgentAsync(string agentId)
    {
        var agent = await _agentRepository.GetWithConfigurationAsync(agentId);
        if (agent == null) return null;

        return await MapEntityToAgentInfo(agent);
    }

    public async Task<AgentInfo> CreateAgentAsync(AgentType type, AgentConfiguration configuration)
    {
        Console.WriteLine($"AgentService.CreateAgentAsync: Method called with type '{type}' and configuration for '{configuration.Name}'");

        // Always persist the agent first so it shows up in the UI, even if runtime init fails
        var persistedId = Guid.NewGuid().ToString();
        var agentEntity = new AgentEntity
        {
            Id = persistedId,
            Name = configuration.Name ?? $"{type} Agent",
            Type = type,
            Description = $"{type} Agent",
            WorkingDirectory = configuration.WorkingDirectory ?? string.Empty,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastUsedAt = DateTime.UtcNow,
            Configuration = MapConfigurationToEntity(configuration, persistedId)
        };

        Console.WriteLine("AgentService.CreateAgentAsync: Persisting agent entity to database (pre-initialization)");
        await _agentRepository.AddAsync(agentEntity);

        var result = await MapEntityToAgentInfo(agentEntity);
        Console.WriteLine($"AgentService.CreateAgentAsync: Returning AgentInfo for '{result.Name}' (Id: {result.Id})");
        return result;
    }

    public async Task UpdateAgentAsync(AgentInfo agentInfo)
    {
        var agentEntity = await _agentRepository.GetWithConfigurationAsync(agentInfo.Id);
        if (agentEntity == null) return;

        // Update entity properties
        agentEntity.Name = agentInfo.Name;
        agentEntity.WorkingDirectory = agentInfo.WorkingDirectory;
        agentEntity.LastUsedAt = DateTime.UtcNow;

        // Update configuration if provided
        if (agentInfo.Configuration.Any() && agentEntity.Configuration != null)
        {
            agentEntity.Configuration.CustomSettingsJson = JsonSerializer.Serialize(agentInfo.Configuration);
        }

        await _agentRepository.UpdateAsync(agentEntity);

        // Update in-memory agent if exists in registry
        var agent = await _agentRegistry.FindAsync(agentInfo.Id);
        if (agent != null)
        {
            agent.Name = agentInfo.Name;
            agent.WorkingDirectory = agentInfo.WorkingDirectory;
        }
    }

    public async Task DeleteAgentAsync(string agentId)
    {
        var agentEntity = await _agentRepository.GetByIdAsync(agentId);
        if (agentEntity != null)
        {
            agentEntity.IsActive = false;
            await _agentRepository.UpdateAsync(agentEntity);
        }

        // Remove from registry and dispose properly
        await _agentRegistry.RemoveAsync(agentId);
    }

    public async Task<bool> IsAgentAvailableAsync(string agentId)
    {
        // Check registry first for active runtime status
        var agent = await _agentRegistry.FindAsync(agentId);
        if (agent != null)
        {
            return agent.Status == AgentStatus.Ready;
        }
        
        // Check database for agent existence and active state
        var agentEntity = await _agentRepository.GetByIdAsync(agentId);
        return agentEntity?.IsActive == true;
    }

    private AgentType GetAgentTypeFromAgent(IAgent agent)
    {
        return agent.GetType().Name switch
        {
            nameof(ClaudeAgent) => AgentType.Claude,
            nameof(SaturnAgent) => AgentType.Saturn,
            _ => AgentType.Custom
        };
    }

    private async Task<AgentInfo> MapEntityToAgentInfo(AgentEntity entity)
    {
        var agentInfo = new AgentInfo
        {
            Id = entity.Id,
            Name = entity.Name,
            Type = entity.Type,
            Description = entity.Description,
            WorkingDirectory = entity.WorkingDirectory,
            LastActive = entity.LastUsedAt ?? entity.CreatedAt
        };

        // Get status from registry if available, otherwise use Initializing for active agents without runtime instance
        var agent = await _agentRegistry.FindAsync(entity.Id);
        if (agent != null)
        {
            agentInfo.Status = agent.Status;
            agentInfo.Capabilities = agent.Capabilities;
        }
        else
        {
            agentInfo.Status = entity.IsActive ? AgentStatus.Initializing : AgentStatus.Shutdown;
        }

        // Deserialize custom settings from configuration
        if (entity.Configuration != null && !string.IsNullOrEmpty(entity.Configuration.CustomSettingsJson))
        {
            try
            {
                agentInfo.Configuration = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    entity.Configuration.CustomSettingsJson) ?? new Dictionary<string, object>();
            }
            catch (JsonException)
            {
                agentInfo.Configuration = new Dictionary<string, object>();
            }
        }

        return agentInfo;
    }

    private static AgentConfigurationEntity MapConfigurationToEntity(AgentConfiguration configuration, string agentId)
    {
        return new AgentConfigurationEntity
        {
            AgentId = agentId,
            Model = configuration.Model,
            Temperature = configuration.Temperature,
            MaxTokens = configuration.MaxTokens,
            SystemPrompt = configuration.SystemPrompt,
            RequireApproval = configuration.RequireApproval,
            CustomSettingsJson = JsonSerializer.Serialize(configuration.CustomSettings),
            EnabledToolsJson = JsonSerializer.Serialize(configuration.EnabledTools),
            CapabilitiesJson = string.Empty // Will be populated when agent capabilities are determined
        };
    }

    private static AgentConfiguration MapEntityToConfiguration(AgentEntity entity)
    {
        var configuration = new AgentConfiguration
        {
            Name = entity.Name,
            Type = entity.Type,
            WorkingDirectory = entity.WorkingDirectory
        };

        if (entity.Configuration != null)
        {
            configuration.Model = entity.Configuration.Model;
            configuration.Temperature = entity.Configuration.Temperature;
            configuration.MaxTokens = entity.Configuration.MaxTokens;
            configuration.SystemPrompt = entity.Configuration.SystemPrompt;
            configuration.RequireApproval = entity.Configuration.RequireApproval;

            // Deserialize custom settings
            if (!string.IsNullOrEmpty(entity.Configuration.CustomSettingsJson))
            {
                try
                {
                    configuration.CustomSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        entity.Configuration.CustomSettingsJson) ?? new Dictionary<string, object>();
                }
                catch (JsonException)
                {
                    configuration.CustomSettings = new Dictionary<string, object>();
                }
            }

            // Deserialize enabled tools
            if (!string.IsNullOrEmpty(entity.Configuration.EnabledToolsJson))
            {
                try
                {
                    configuration.EnabledTools = JsonSerializer.Deserialize<List<string>>(
                        entity.Configuration.EnabledToolsJson) ?? new List<string>();
                }
                catch (JsonException)
                {
                    configuration.EnabledTools = new List<string>();
                }
            }
        }

        return configuration;
    }

    public async Task<AgentInfo?> GetDefaultAgentAsync()
    {
        var defaultAgent = await _agentRepository.GetDefaultAgentAsync();
        if (defaultAgent == null) return null;

        return await MapEntityToAgentInfo(defaultAgent);
    }

    public async Task<bool> SetDefaultAgentAsync(string agentId)
    {
        return await _agentRepository.SetDefaultAgentAsync(agentId);
    }
}
