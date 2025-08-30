using OrchestratorChat.Core.Events;

namespace OrchestratorChat.SignalR.EventHandlers;

/// <summary>
/// Interface for handling domain events
/// </summary>
/// <typeparam name="TEvent">Type of event to handle</typeparam>
public interface IEventHandler<TEvent> where TEvent : IEvent
{
    /// <summary>
    /// Handles the specified event
    /// </summary>
    /// <param name="event">Event to handle</param>
    Task HandleAsync(TEvent @event);
}