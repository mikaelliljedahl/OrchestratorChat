using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrchestratorChat.Core.Messages;
using OrchestratorChat.Data.Models;

namespace OrchestratorChat.Data.Repositories;

public class AgentRepository : Repository<AgentEntity>, IAgentRepository
{
    public AgentRepository(OrchestratorDbContext context) : base(context)
    {
    }
    
    public async Task<AgentEntity?> GetWithConfigurationAsync(string agentId)
    {
        return await _dbSet
            .Include(a => a.Configuration)
            .FirstOrDefaultAsync(a => a.Id == agentId);
    }
    
    public async Task<IEnumerable<AgentEntity>> GetActiveAgentsAsync()
    {
        return await _dbSet
            .Where(a => a.IsActive)
            .Include(a => a.Configuration)
            .OrderBy(a => a.Name)
            .ToListAsync();
    }
    
    public async Task<bool> UpdateConfigurationAsync(
        string agentId, 
        AgentConfigurationEntity config)
    {
        var agent = await GetWithConfigurationAsync(agentId);
        if (agent == null) return false;
        
        if (agent.Configuration == null)
        {
            config.AgentId = agentId;
            await _context.AgentConfigurations.AddAsync(config);
        }
        else
        {
            agent.Configuration.Model = config.Model;
            agent.Configuration.Temperature = config.Temperature;
            agent.Configuration.MaxTokens = config.MaxTokens;
            agent.Configuration.SystemPrompt = config.SystemPrompt;
            agent.Configuration.RequireApproval = config.RequireApproval;
            agent.Configuration.CustomSettingsJson = config.CustomSettingsJson;
            agent.Configuration.EnabledToolsJson = config.EnabledToolsJson;
            agent.Configuration.CapabilitiesJson = config.CapabilitiesJson;
            
            _context.AgentConfigurations.Update(agent.Configuration);
        }
        
        await _context.SaveChangesAsync();
        return true;
    }
    
    public async Task<AgentUsageStatistics?> GetUsageStatisticsAsync(
        string agentId, 
        DateTime? from = null)
    {
        var query = _context.Messages
            .Where(m => m.AgentId == agentId);
        
        if (from.HasValue)
        {
            query = query.Where(m => m.Timestamp >= from.Value);
        }
        
        var messages = await query.ToListAsync();
        
        if (!messages.Any()) return null;
        
        return new AgentUsageStatistics
        {
            AgentId = agentId,
            MessageCount = messages.Count,
            SessionCount = messages.Select(m => m.SessionId).Distinct().Count(),
            TotalTokensUsed = messages
                .Where(m => !string.IsNullOrEmpty(m.TokenUsageJson))
                .Select(m => JsonSerializer.Deserialize<TokenUsage>(m.TokenUsageJson))
                .Sum(t => t?.TotalTokens ?? 0),
            ToolCallCount = await _context.ToolCalls
                .CountAsync(t => messages.Select(m => m.Id).Contains(t.MessageId)),
            Period = from.HasValue ? DateTime.UtcNow - from.Value : null
        };
    }
    
    public async Task IncrementUsageAsync(string agentId, int messageCount, long tokensUsed)
    {
        var agent = await GetByIdAsync(agentId);
        if (agent == null) return;
        
        agent.TotalMessages += messageCount;
        agent.TotalTokensUsed += tokensUsed;
        agent.LastUsedAt = DateTime.UtcNow;
        
        await UpdateAsync(agent);
    }
    
    public async Task<AgentEntity?> GetDefaultAgentAsync()
    {
        return await _dbSet
            .Include(a => a.Configuration)
            .FirstOrDefaultAsync(a => a.IsDefault && a.IsActive);
    }
    
    public async Task<bool> SetDefaultAgentAsync(string agentId)
    {
        // First, clear any existing default agent
        var currentDefault = await _dbSet
            .FirstOrDefaultAsync(a => a.IsDefault);
        
        if (currentDefault != null)
        {
            currentDefault.IsDefault = false;
            _context.Update(currentDefault);
        }
        
        // Set the new default agent
        var newDefault = await GetByIdAsync(agentId);
        if (newDefault == null) return false;
        
        newDefault.IsDefault = true;
        _context.Update(newDefault);
        
        await _context.SaveChangesAsync();
        return true;
    }
}