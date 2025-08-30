using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrchestratorChat.SignalR.Hubs;

namespace OrchestratorChat.SignalR.IntegrationTests.Fixtures
{
    /// <summary>
    /// Custom web application factory for SignalR integration tests
    /// </summary>
    public class TestWebApplicationFactory : WebApplicationFactory<TestStartup>
    {
        private Action<IServiceCollection>? _configureTestServices;
        
        /// <summary>
        /// Configures test-specific services
        /// </summary>
        /// <param name="configureServices">Service configuration action</param>
        public void ConfigureTestServices(Action<IServiceCollection> configureServices)
        {
            _configureTestServices = configureServices;
        }
        
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Add test-specific configuration
                config.AddJsonFile("appsettings.test.json", optional: true);
                config.AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("Logging:LogLevel:Default", "Debug"),
                    new KeyValuePair<string, string?>("Logging:LogLevel:Microsoft.AspNetCore.SignalR", "Debug"),
                    new KeyValuePair<string, string?>("Logging:LogLevel:Microsoft.AspNetCore.Http.Connections", "Debug")
                });
            });
            
            builder.UseEnvironment("Test");
            
            builder.ConfigureServices(services =>
            {
                // Add SignalR services
                services.AddSignalR(options =>
                {
                    options.EnableDetailedErrors = true;
                    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
                });
                
                // Add test-specific services
                _configureTestServices?.Invoke(services);
            });
        }
    }
    
    /// <summary>
    /// Minimal startup class for testing
    /// </summary>
    public class TestStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR();
        }
        
        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<OrchestratorHub>("/hubs/orchestrator");
                endpoints.MapHub<AgentHub>("/hubs/agents");
            });
        }
    }
}