using OrchestratorChat.Core.Events;
using OrchestratorChat.SignalR.EventHandlers;
using OrchestratorChat.SignalR.Events;
using Microsoft.Extensions.Hosting;

namespace OrchestratorChat.SignalR.Services;

public class EventBusSubscriber : IHostedService
{
    private readonly IEventBus _eventBus;
    private readonly AgentEventHandler _agentEventHandler;
    private readonly OrchestrationEventHandler _orchestrationEventHandler;
    
    public EventBusSubscriber(
        IEventBus eventBus,
        AgentEventHandler agentEventHandler,
        OrchestrationEventHandler orchestrationEventHandler)
    {
        _eventBus = eventBus;
        _agentEventHandler = agentEventHandler;
        _orchestrationEventHandler = orchestrationEventHandler;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Subscribe to agent events
        _eventBus.Subscribe<AgentStatusChangedEvent>(async e => await _agentEventHandler.HandleAsync(e));
        _eventBus.Subscribe<MessageReceivedEvent>(async e => await _agentEventHandler.HandleAsync(e));
        
        // Subscribe to orchestration events  
        _eventBus.Subscribe<OrchestrationStepCompletedEvent>(async e => await _orchestrationEventHandler.HandleAsync(e));
        
        return Task.CompletedTask;
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Unsubscribe if needed
        return Task.CompletedTask;
    }
}