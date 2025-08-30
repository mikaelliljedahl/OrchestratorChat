using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace OrchestratorChat.Web.Services;

public class HubConnectionManager : IHubConnectionManager
{
    private readonly ILogger<HubConnectionManager> _logger;
    private HubConnection? _agentHubConnection;
    private HubConnection? _orchestratorHubConnection;
    private string _baseUrl = "";

    public HubConnectionManager(ILogger<HubConnectionManager> logger)
    {
        _logger = logger;
    }

    public HubConnection? AgentHubConnection => _agentHubConnection;
    public HubConnection? OrchestratorHubConnection => _orchestratorHubConnection;

    public async Task InitializeAsync(string baseUrl)
    {
        _baseUrl = baseUrl;
        
        _agentHubConnection = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}/hubs/agent")
            .WithAutomaticReconnect()
            .Build();
            
        _orchestratorHubConnection = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}/hubs/orchestrator")
            .WithAutomaticReconnect()
            .Build();

        // Set up connection event handlers
        SetupConnectionEventHandlers();
    }

    public async Task StartConnectionsAsync()
    {
        try
        {
            if (_agentHubConnection?.State == HubConnectionState.Disconnected)
            {
                await _agentHubConnection.StartAsync();
                _logger.LogInformation("Agent hub connection started");
            }

            if (_orchestratorHubConnection?.State == HubConnectionState.Disconnected)
            {
                await _orchestratorHubConnection.StartAsync();
                _logger.LogInformation("Orchestrator hub connection started");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting hub connections");
            throw;
        }
    }

    public async Task StopConnectionsAsync()
    {
        try
        {
            if (_agentHubConnection?.State == HubConnectionState.Connected)
            {
                await _agentHubConnection.StopAsync();
            }

            if (_orchestratorHubConnection?.State == HubConnectionState.Connected)
            {
                await _orchestratorHubConnection.StopAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping hub connections");
        }
    }

    public bool IsConnected(string hubName)
    {
        return hubName.ToLower() switch
        {
            "agent" => _agentHubConnection?.State == HubConnectionState.Connected,
            "orchestrator" => _orchestratorHubConnection?.State == HubConnectionState.Connected,
            _ => false
        };
    }

    private void SetupConnectionEventHandlers()
    {
        if (_agentHubConnection != null)
        {
            _agentHubConnection.Closed += async (error) =>
            {
                _logger.LogWarning(error, "Agent hub connection closed");
                await Task.Delay(Random.Shared.Next(0, 5) * 1000);
                // Connection will automatically try to reconnect due to WithAutomaticReconnect()
            };

            _agentHubConnection.Reconnecting += (error) =>
            {
                _logger.LogInformation("Agent hub connection reconnecting...");
                return Task.CompletedTask;
            };

            _agentHubConnection.Reconnected += (connectionId) =>
            {
                _logger.LogInformation($"Agent hub connection reconnected. Connection ID: {connectionId}");
                return Task.CompletedTask;
            };
        }

        if (_orchestratorHubConnection != null)
        {
            _orchestratorHubConnection.Closed += async (error) =>
            {
                _logger.LogWarning(error, "Orchestrator hub connection closed");
                await Task.Delay(Random.Shared.Next(0, 5) * 1000);
            };

            _orchestratorHubConnection.Reconnecting += (error) =>
            {
                _logger.LogInformation("Orchestrator hub connection reconnecting...");
                return Task.CompletedTask;
            };

            _orchestratorHubConnection.Reconnected += (connectionId) =>
            {
                _logger.LogInformation($"Orchestrator hub connection reconnected. Connection ID: {connectionId}");
                return Task.CompletedTask;
            };
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await StopConnectionsAsync();
            
            if (_agentHubConnection != null)
            {
                await _agentHubConnection.DisposeAsync();
            }
            
            if (_orchestratorHubConnection != null)
            {
                await _orchestratorHubConnection.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing hub connections");
        }
    }
}