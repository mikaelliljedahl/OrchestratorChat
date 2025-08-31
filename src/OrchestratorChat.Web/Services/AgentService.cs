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
    private readonly Dictionary<string, IAgent> _agents = new();

    public AgentService(IAgentFactory agentFactory, IAgentRepository agentRepository)
    {
        _agentFactory = agentFactory;
        _agentRepository = agentRepository;
        
        // Initialize agents from database on startup
        _ = Task.Run(InitializeAgentsFromDatabaseAsync);
    }

    private async Task InitializeAgentsFromDatabaseAsync()
    {
        try
        {
            var activeAgents = await _agentRepository.GetActiveAgentsAsync();
            foreach (var agentEntity in activeAgents)
            {
                try
                {
                    // Recreate agent configuration from entity
                    var configuration = MapEntityToConfiguration(agentEntity);
                    
                    // Create agent instance
                    var agent = await _agentFactory.CreateAgentAsync(agentEntity.Type, configuration);
                    _agents[agent.Id] = agent;
                }
                catch (Exception ex)
                {
                    // Log error but continue with other agents
                    // Consider logging this in a real application
                    Console.WriteLine($"Failed to initialize agent {agentEntity.Id}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            // Log initialization failure
            Console.WriteLine($"Failed to initialize agents from database: {ex.Message}");
        }
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
        
        try
        {
            Console.WriteLine($"AgentService.CreateAgentAsync: Calling AgentFactory.CreateAgentAsync");
            var agent = await _agentFactory.CreateAgentAsync(type, configuration);
            Console.WriteLine($"AgentService.CreateAgentAsync: Agent created with ID '{agent.Id}'");
            
            _agents[agent.Id] = agent;

            // Create entity and persist to database
            var agentEntity = new AgentEntity
            {
                Id = agent.Id,
                Name = agent.Name,
                Type = type,
                Description = $"{type} Agent",
                WorkingDirectory = agent.WorkingDirectory ?? string.Empty,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastUsedAt = DateTime.UtcNow,
                Configuration = MapConfigurationToEntity(configuration, agent.Id)
            };

            Console.WriteLine($"AgentService.CreateAgentAsync: Persisting agent entity to database");
            await _agentRepository.AddAsync(agentEntity);
            
            var result = await MapEntityToAgentInfo(agentEntity);
            Console.WriteLine($"AgentService.CreateAgentAsync: Method completed successfully, returning AgentInfo for '{result.Name}'");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AgentService.CreateAgentAsync: Exception occurred: {ex.Message}");
            Console.WriteLine($"AgentService.CreateAgentAsync: Exception details: {ex}");
            throw;
        }
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

        // Update in-memory agent if exists
        if (_agents.TryGetValue(agentInfo.Id, out var agent))
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

        if (_agents.TryGetValue(agentId, out var agent))
        {
            // await agent.DisposeAsync(); // TODO: Add cleanup when available
            _agents.Remove(agentId);
        }
    }

    public async Task<bool> IsAgentAvailableAsync(string agentId)
    {
        // Check in-memory agents first for active status
        if (_agents.TryGetValue(agentId, out var agent))
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

    private Task<AgentInfo> MapEntityToAgentInfo(AgentEntity entity)
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

        // Get status from in-memory agent if available, otherwise use Ready for active agents
        if (_agents.TryGetValue(entity.Id, out var agent))
        {
            agentInfo.Status = agent.Status;
            agentInfo.Capabilities = agent.Capabilities;
        }
        else
        {
            agentInfo.Status = entity.IsActive ? AgentStatus.Ready : AgentStatus.Shutdown;
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

        return Task.FromResult(agentInfo);
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
}