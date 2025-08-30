using Microsoft.Extensions.Logging;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Messages;

namespace OrchestratorChat.Agents.Monitoring;

public interface IAgentHealthMonitor
{
    Task<AgentHealth> CheckHealthAsync(IAgent agent);
    void StartMonitoring(IAgent agent, TimeSpan interval);
    void StopMonitoring(string agentId);
    event EventHandler<AgentHealthChangedEventArgs>? HealthChanged;
}

public class AgentHealthMonitor : IAgentHealthMonitor, IDisposable
{
    private readonly Dictionary<string, Timer> _monitors = new();
    private readonly ILogger<AgentHealthMonitor> _logger;

    public event EventHandler<AgentHealthChangedEventArgs>? HealthChanged;

    public AgentHealthMonitor(ILogger<AgentHealthMonitor> logger)
    {
        _logger = logger;
    }

    public async Task<AgentHealth> CheckHealthAsync(IAgent agent)
    {
        try
        {
            // Send health check message
            var healthMessage = new AgentMessage
            {
                Content = "ping",
                Role = MessageRole.System
            };

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var responses = new List<AgentResponse>();

            var responseStream = await agent.SendMessageStreamAsync(healthMessage, cts.Token);
            await foreach (var response in responseStream)
            {
                responses.Add(response);
            }

            return new AgentHealth
            {
                AgentId = agent.Id,
                Status = HealthStatus.Healthy,
                LastCheckTime = DateTime.UtcNow,
                ResponseTime = TimeSpan.FromMilliseconds(100)
            };
        }
        catch (Exception ex)
        {
            return new AgentHealth
            {
                AgentId = agent.Id,
                Status = HealthStatus.Unhealthy,
                LastCheckTime = DateTime.UtcNow,
                Error = ex.Message
            };
        }
    }

    public void StartMonitoring(IAgent agent, TimeSpan interval)
    {
        if (_monitors.ContainsKey(agent.Id))
            return;

        var timer = new Timer(
            async _ => await CheckAndReportHealthAsync(agent),
            null,
            TimeSpan.Zero,
            interval);

        _monitors[agent.Id] = timer;
    }

    public void StopMonitoring(string agentId)
    {
        if (_monitors.TryGetValue(agentId, out var timer))
        {
            timer?.Dispose();
            _monitors.Remove(agentId);
        }
    }

    private async Task CheckAndReportHealthAsync(IAgent agent)
    {
        try
        {
            var health = await CheckHealthAsync(agent);
            HealthChanged?.Invoke(this, new AgentHealthChangedEventArgs
            {
                AgentId = agent.Id,
                Health = health
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking health for agent {AgentId}", agent.Id);
        }
    }

    public void Dispose()
    {
        foreach (var timer in _monitors.Values)
        {
            timer?.Dispose();
        }
        _monitors.Clear();
    }
}

public class AgentHealth
{
    public string AgentId { get; set; } = string.Empty;
    public HealthStatus Status { get; set; }
    public DateTime LastCheckTime { get; set; }
    public TimeSpan? ResponseTime { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
}

public enum HealthStatus
{
    Unknown,
    Healthy,
    Degraded,
    Unhealthy
}

public class AgentHealthChangedEventArgs : EventArgs
{
    public string AgentId { get; set; } = string.Empty;
    public AgentHealth Health { get; set; } = new();
}