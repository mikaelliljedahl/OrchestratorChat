using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Data.Models;

namespace OrchestratorChat.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(OrchestratorDbContext context)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();
        
        // Apply any pending migrations
        await context.Database.MigrateAsync();
        
        // Seed initial data if needed
        await SeedDataAsync(context);
    }
    
    private static async Task SeedDataAsync(OrchestratorDbContext context)
    {
        // Check if already seeded
        if (await context.Agents.AnyAsync())
            return;
        
        // Add default Claude agent
        var claudeAgent = new AgentEntity
        {
            Id = "default-claude",
            Name = "Claude Assistant",
            Type = AgentType.Claude,
            Description = "Default Claude agent for general assistance",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Configuration = new AgentConfigurationEntity
            {
                Model = "claude-sonnet-4-20250514",
                Temperature = 0.7,
                MaxTokens = 4096,
                SystemPrompt = "You are a helpful AI assistant.",
                RequireApproval = false,
                CapabilitiesJson = JsonSerializer.Serialize(new AgentCapabilities
                {
                    SupportsStreaming = true,
                    SupportsTools = true,
                    SupportsFileOperations = true,
                    MaxTokens = 100000
                })
            }
        };
        
        await context.Agents.AddAsync(claudeAgent);
        
        // Add default Saturn agent
        var saturnAgent = new AgentEntity
        {
            Id = "default-saturn",
            Name = "Saturn Developer",
            Type = AgentType.Saturn,
            Description = "Saturn agent for development tasks",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Configuration = new AgentConfigurationEntity
            {
                Model = "claude-opus-4-1-20250805",
                Temperature = 0.3,
                MaxTokens = 8192,
                SystemPrompt = "You are an expert software developer.",
                RequireApproval = true,
                CapabilitiesJson = JsonSerializer.Serialize(new AgentCapabilities
                {
                    SupportsStreaming = true,
                    SupportsTools = true,
                    SupportsFileOperations = true,
                    MaxTokens = 100000
                })
            }
        };
        
        await context.Agents.AddAsync(saturnAgent);
        await context.SaveChangesAsync();
    }
}