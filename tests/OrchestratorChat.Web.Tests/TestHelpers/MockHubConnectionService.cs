using Microsoft.AspNetCore.SignalR.Client;
using NSubstitute;

namespace OrchestratorChat.Web.Tests.TestHelpers;

/// <summary>
/// Mock hub connection service for testing - creates mock HubConnection objects
/// that behave like connected hubs without making actual network calls
/// </summary>
public static class MockHubConnectionService
{
    /// <summary>
    /// Creates a mock HubConnection that appears connected but doesn't make network calls
    /// </summary>
    public static HubConnection CreateMockHubConnection(string hubName = "MockHub")
    {
        var mockConnection = Substitute.For<HubConnection>();
        
        // Set up the connection to appear connected
        mockConnection.State.Returns(HubConnectionState.Connected);
        
        // Set up ConnectionId
        mockConnection.ConnectionId.Returns($"mock-connection-{Guid.NewGuid()}");
        
        // Mock StartAsync to return completed task immediately
        mockConnection.StartAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        
        // Mock StopAsync to return completed task immediately
        mockConnection.StopAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        
        // Mock DisposeAsync to return completed task immediately
        mockConnection.DisposeAsync()
            .Returns(ValueTask.CompletedTask);
        
        // Mock InvokeAsync methods to return successful results
        mockConnection.InvokeAsync(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(Task.CompletedTask);
            
        mockConnection.InvokeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
            
        mockConnection.InvokeAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        
        // Generic InvokeAsync with return type - return default values for common response types
        mockConnection.InvokeAsync<object>(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(Task.FromResult<object>(new { Success = true }));
        
        // Set up event handler registration to do nothing but not throw
        var onHandlers = new Dictionary<string, List<Func<object[], object, Task>>>();
        
        mockConnection.When(x => x.On(Arg.Any<string>(), Arg.Any<Func<object[], object, Task>>()))
            .Do(callInfo =>
            {
                var methodName = callInfo.ArgAt<string>(0);
                var handler = callInfo.ArgAt<Func<object[], object, Task>>(1);
                
                if (!onHandlers.ContainsKey(methodName))
                    onHandlers[methodName] = new List<Func<object[], object, Task>>();
                
                onHandlers[methodName].Add(handler);
            });
        
        return mockConnection;
    }
    
    /// <summary>
    /// Creates a mock HubConnection that will simulate connection failures
    /// </summary>
    public static HubConnection CreateFailingMockHubConnection(string errorMessage = "Mock connection failed")
    {
        var mockConnection = Substitute.For<HubConnection>();
        
        // Set up the connection to appear disconnected
        mockConnection.State.Returns(HubConnectionState.Disconnected);
        
        // Make StartAsync throw an exception
        mockConnection.StartAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException(errorMessage)));
        
        // Mock other methods to return failed results
        mockConnection.InvokeAsync(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(Task.FromException(new InvalidOperationException(errorMessage)));
        
        return mockConnection;
    }
}