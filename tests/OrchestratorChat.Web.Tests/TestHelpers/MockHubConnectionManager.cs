using Microsoft.AspNetCore.SignalR.Client;
using OrchestratorChat.Web.Services;

namespace OrchestratorChat.Web.Tests.TestHelpers;

/// <summary>
/// Mock implementation of IHubConnectionManager for testing
/// </summary>
public class MockHubConnectionManager : IHubConnectionManager
{
    public HubConnection? AgentHubConnection { get; private set; }
    public HubConnection? OrchestratorHubConnection { get; private set; }

    public Task InitializeAsync(string baseUrl)
    {
        // Create mock connections that behave like connected hubs
        AgentHubConnection = MockHubConnectionService.CreateMockHubConnection("AgentHub");
        OrchestratorHubConnection = MockHubConnectionService.CreateMockHubConnection("OrchestratorHub");
        
        return Task.CompletedTask;
    }

    public Task StartConnectionsAsync()
    {
        // Mock connections are already "connected" - return immediately
        return Task.CompletedTask;
    }

    public Task StopConnectionsAsync()
    {
        // Mock stop operation - return immediately
        return Task.CompletedTask;
    }

    public bool IsConnected(string hubName)
    {
        return hubName.ToLower() switch
        {
            "agent" => AgentHubConnection?.State == HubConnectionState.Connected,
            "orchestrator" => OrchestratorHubConnection?.State == HubConnectionState.Connected,
            _ => false
        };
    }

    public ValueTask DisposeAsync()
    {
        // Mock disposal
        return ValueTask.CompletedTask;
    }
}