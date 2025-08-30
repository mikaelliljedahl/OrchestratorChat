using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Concurrent;

namespace OrchestratorChat.SignalR.IntegrationTests.Helpers
{
    /// <summary>
    /// Helper class for creating and managing SignalR test connections
    /// </summary>
    public class SignalRTestClient : IAsyncDisposable
    {
        private readonly HubConnection _connection;
        private readonly string _hubName;
        private readonly ConcurrentDictionary<string, List<Delegate>> _handlers = new();

        public HubConnection Connection => _connection;
        public string HubName => _hubName;
        public HubConnectionState State => _connection.State;

        /// <summary>
        /// Initializes a new SignalR test client
        /// </summary>
        /// <param name="connection">The hub connection</param>
        /// <param name="hubName">Name of the hub for logging</param>
        public SignalRTestClient(HubConnection connection, string hubName)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _hubName = hubName ?? throw new ArgumentNullException(nameof(hubName));
        }

        /// <summary>
        /// Starts the SignalR connection
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_connection.State == HubConnectionState.Disconnected)
            {
                await _connection.StartAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Stops the SignalR connection
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (_connection.State == HubConnectionState.Connected)
            {
                await _connection.StopAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Invokes a hub method without return value
        /// </summary>
        /// <param name="methodName">Method name to invoke</param>
        /// <param name="args">Method arguments</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task InvokeAsync(string methodName, params object[] args)
        {
            await _connection.InvokeAsync(methodName, args, CancellationToken.None);
        }

        /// <summary>
        /// Invokes a hub method with return value
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="methodName">Method name to invoke</param>
        /// <param name="args">Method arguments</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Method result</returns>
        public async Task<T> InvokeAsync<T>(string methodName, params object[] args)
        {
            return await _connection.InvokeAsync<T>(methodName, args, CancellationToken.None);
        }

        /// <summary>
        /// Registers a handler for receiving messages from the hub
        /// </summary>
        /// <param name="methodName">Method name to listen for</param>
        /// <param name="handler">Handler function</param>
        public IDisposable On(string methodName, Action handler)
        {
            var subscription = _connection.On(methodName, handler);
            TrackHandler(methodName, handler);
            return subscription;
        }

        /// <summary>
        /// Registers a handler for receiving messages from the hub with 1 parameter
        /// </summary>
        /// <typeparam name="T1">Parameter type</typeparam>
        /// <param name="methodName">Method name to listen for</param>
        /// <param name="handler">Handler function</param>
        public IDisposable On<T1>(string methodName, Action<T1> handler)
        {
            var subscription = _connection.On<T1>(methodName, handler);
            TrackHandler(methodName, handler);
            return subscription;
        }

        /// <summary>
        /// Registers a handler for receiving messages from the hub with 2 parameters
        /// </summary>
        /// <typeparam name="T1">First parameter type</typeparam>
        /// <typeparam name="T2">Second parameter type</typeparam>
        /// <param name="methodName">Method name to listen for</param>
        /// <param name="handler">Handler function</param>
        public IDisposable On<T1, T2>(string methodName, Action<T1, T2> handler)
        {
            var subscription = _connection.On<T1, T2>(methodName, handler);
            TrackHandler(methodName, handler);
            return subscription;
        }

        /// <summary>
        /// Registers a handler for receiving messages from the hub with 3 parameters
        /// </summary>
        /// <typeparam name="T1">First parameter type</typeparam>
        /// <typeparam name="T2">Second parameter type</typeparam>
        /// <typeparam name="T3">Third parameter type</typeparam>
        /// <param name="methodName">Method name to listen for</param>
        /// <param name="handler">Handler function</param>
        public IDisposable On<T1, T2, T3>(string methodName, Action<T1, T2, T3> handler)
        {
            var subscription = _connection.On<T1, T2, T3>(methodName, handler);
            TrackHandler(methodName, handler);
            return subscription;
        }

        /// <summary>
        /// Registers an async handler for receiving messages from the hub
        /// </summary>
        /// <param name="methodName">Method name to listen for</param>
        /// <param name="handler">Async handler function</param>
        public IDisposable On(string methodName, Func<Task> handler)
        {
            var subscription = _connection.On(methodName, handler);
            TrackHandler(methodName, handler);
            return subscription;
        }

        /// <summary>
        /// Registers an async handler for receiving messages from the hub with 1 parameter
        /// </summary>
        /// <typeparam name="T1">Parameter type</typeparam>
        /// <param name="methodName">Method name to listen for</param>
        /// <param name="handler">Async handler function</param>
        public IDisposable On<T1>(string methodName, Func<T1, Task> handler)
        {
            var subscription = _connection.On<T1>(methodName, handler);
            TrackHandler(methodName, handler);
            return subscription;
        }

        /// <summary>
        /// Waits for the connection to reach a specific state
        /// </summary>
        /// <param name="expectedState">Expected connection state</param>
        /// <param name="timeout">Timeout duration</param>
        /// <returns>True if state reached within timeout</returns>
        public async Task<bool> WaitForState(HubConnectionState expectedState, TimeSpan timeout)
        {
            var startTime = DateTime.UtcNow;
            
            while (DateTime.UtcNow - startTime < timeout)
            {
                if (_connection.State == expectedState)
                    return true;
                
                await Task.Delay(50);
            }
            
            return false;
        }

        /// <summary>
        /// Gets the current connection ID
        /// </summary>
        /// <returns>Connection ID or null if not connected</returns>
        public string? GetConnectionId()
        {
            return _connection.ConnectionId;
        }

        /// <summary>
        /// Gets all registered handler method names
        /// </summary>
        /// <returns>List of method names</returns>
        public List<string> GetRegisteredHandlers()
        {
            return _handlers.Keys.ToList();
        }

        /// <summary>
        /// Gets the count of handlers for a specific method
        /// </summary>
        /// <param name="methodName">Method name</param>
        /// <returns>Number of handlers</returns>
        public int GetHandlerCount(string methodName)
        {
            return _handlers.TryGetValue(methodName, out var handlers) ? handlers.Count : 0;
        }

        /// <summary>
        /// Clears all registered handlers
        /// </summary>
        public void ClearHandlers()
        {
            _handlers.Clear();
        }

        /// <summary>
        /// Creates a task that completes when a specific method is called
        /// </summary>
        /// <param name="methodName">Method name to wait for</param>
        /// <param name="timeout">Timeout duration</param>
        /// <returns>Task that completes when method is called</returns>
        public Task<bool> WaitForMethodCall(string methodName, TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<bool>();
            var cts = new CancellationTokenSource(timeout);
            
            cts.Token.Register(() => tcs.TrySetResult(false));
            
            var subscription = On(methodName, () => tcs.TrySetResult(true));
            
            return tcs.Task.ContinueWith(task =>
            {
                subscription.Dispose();
                cts.Dispose();
                return task.Result;
            });
        }

        /// <summary>
        /// Creates a task that completes when a specific method is called with a value
        /// </summary>
        /// <typeparam name="T">Expected value type</typeparam>
        /// <param name="methodName">Method name to wait for</param>
        /// <param name="timeout">Timeout duration</param>
        /// <returns>Task that completes with the received value</returns>
        public Task<T?> WaitForMethodCall<T>(string methodName, TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<T?>();
            var cts = new CancellationTokenSource(timeout);
            
            cts.Token.Register(() => tcs.TrySetResult(default(T)));
            
            var subscription = On<T>(methodName, value => tcs.TrySetResult(value));
            
            return tcs.Task.ContinueWith(task =>
            {
                subscription.Dispose();
                cts.Dispose();
                return task.Result;
            });
        }

        private void TrackHandler(string methodName, Delegate handler)
        {
            _handlers.AddOrUpdate(methodName,
                new List<Delegate> { handler },
                (key, existing) =>
                {
                    existing.Add(handler);
                    return existing;
                });
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                ClearHandlers();
                
                if (_connection.State == HubConnectionState.Connected)
                {
                    await _connection.StopAsync();
                }
            }
            finally
            {
                await _connection.DisposeAsync();
            }
        }
    }
}