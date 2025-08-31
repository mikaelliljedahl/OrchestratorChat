using MudBlazor.Services;
using OrchestratorChat.Web.Services;
using OrchestratorChat.Agents;
using OrchestratorChat.Agents.Claude;
using OrchestratorChat.Agents.Saturn;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Core.Orchestration;
using OrchestratorChat.Core.Events;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Data;
using OrchestratorChat.Data.Repositories;
using OrchestratorChat.Data.Adapters;
using OrchestratorChat.Configuration;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();

// Add MudBlazor services
builder.Services.AddMudServices();

// Add SignalR
builder.Services.AddSignalR();

// Add HttpClient
builder.Services.AddHttpClient();

// Add session support for OAuth flows
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add MemoryCache for PKCE/state storage
builder.Services.AddMemoryCache();

// Add Entity Framework
builder.Services.AddDbContext<OrchestratorDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=orchestrator.db"));

// Add configuration services
builder.Services.AddSingleton<OrchestratorChat.Core.Configuration.IConfigurationProvider, OrchestratorChat.Configuration.Services.ConfigurationService>();

// Add core services
builder.Services.AddScoped<IEventBus, EventBus>();
builder.Services.AddScoped<OrchestratorChat.Core.Sessions.ISessionRepository, OrchestratorChat.Data.Adapters.SessionRepositoryAdapter>();
builder.Services.AddScoped<OrchestratorChat.Data.Repositories.IAgentRepository, OrchestratorChat.Data.Repositories.AgentRepository>();
builder.Services.AddScoped<ISessionManager, SessionManager>();
builder.Services.AddScoped<IOrchestrator, Orchestrator>();

// Add Saturn services
builder.Services.AddScoped<OrchestratorChat.Saturn.Providers.ProviderFactory>();
builder.Services.AddScoped<OrchestratorChat.Saturn.Core.ISaturnCore, OrchestratorChat.Saturn.Core.SaturnCore>();
builder.Services.AddScoped<OrchestratorChat.Saturn.Providers.Anthropic.ITokenStore, OrchestratorChat.Saturn.Providers.Anthropic.TokenStore>();

// Add agent services
builder.Services.AddScoped<IAgentFactory, AgentFactory>();
builder.Services.AddSingleton<IAgentRegistry, AgentRegistry>();
builder.Services.AddScoped<ClaudeAgent>();
builder.Services.AddScoped<SaturnAgent>();

// Add web-specific services
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddScoped<IOrchestrationService, OrchestrationService>();
builder.Services.AddScoped<IHubConnectionManager, HubConnectionManager>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IProviderVerificationService, ProviderVerificationService>();
builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();

// Add logging
builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSession();
app.UseRouting();

app.MapRazorPages();
app.MapBlazorHub();
app.MapControllers();
app.MapFallbackToPage("/_Host");

// Map SignalR hubs
app.MapHub<OrchestratorChat.SignalR.Hubs.AgentHub>("/hubs/agent");
app.MapHub<OrchestratorChat.SignalR.Hubs.OrchestratorHub>("/hubs/orchestrator");

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();
    context.Database.EnsureCreated();
}

app.Run();