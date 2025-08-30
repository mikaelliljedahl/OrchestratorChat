using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrchestratorChat.Data;
using Xunit;

namespace OrchestratorChat.Core.Tests.Fixtures;

/// <summary>
/// Minimal working base test class with in-memory database
/// </summary>
public abstract class WorkingTestBase : IDisposable
{
    protected OrchestratorDbContext DbContext { get; private set; }
    protected IServiceProvider ServiceProvider { get; private set; }
    private bool _disposed;

    protected WorkingTestBase()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();
        
        DbContext = ServiceProvider.GetRequiredService<OrchestratorDbContext>();
        InitializeDatabase();
    }

    /// <summary>
    /// Configure services for dependency injection
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Configure in-memory database with unique name per test
        var databaseName = $"TestDb_{Guid.NewGuid()}";
        services.AddDbContext<OrchestratorDbContext>(options =>
        {
            options.UseInMemoryDatabase(databaseName);
            options.EnableSensitiveDataLogging();
        });

        // Add minimal logging without console dependency
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        ConfigureTestServices(services);
    }

    /// <summary>
    /// Configure additional test-specific services
    /// </summary>
    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
        // Override in derived classes to add specific services
    }

    /// <summary>
    /// Initialize the in-memory database
    /// </summary>
    protected virtual void InitializeDatabase()
    {
        DbContext.Database.EnsureCreated();
    }

    /// <summary>
    /// Get a service from the dependency injection container
    /// </summary>
    protected T GetService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Create a new database context instance
    /// </summary>
    protected OrchestratorDbContext CreateNewDbContext()
    {
        return ServiceProvider.GetRequiredService<OrchestratorDbContext>();
    }

    /// <summary>
    /// Clear all data from the database while keeping the schema
    /// </summary>
    protected async Task ClearDatabaseAsync()
    {
        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.Database.EnsureCreatedAsync();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            DbContext?.Dispose();
            ServiceProvider?.GetService<IServiceScope>()?.Dispose();
            _disposed = true;
        }
    }
}