using Microsoft.AspNetCore.SignalR.Client;

namespace OrchestratorChat.Web.Services;

public interface IHubConnectionManager : IAsyncDisposable
{
    HubConnection? AgentHubConnection { get; }
    HubConnection? OrchestratorHubConnection { get; }
    
    Task InitializeAsync(string baseUrl);
    Task StartConnectionsAsync();
    Task StopConnectionsAsync();
    bool IsConnected(string hubName);
}