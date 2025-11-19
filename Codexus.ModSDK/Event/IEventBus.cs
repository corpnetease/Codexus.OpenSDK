namespace Codexus.ModSDK.Event;

public interface IEventBus
{
    IEventBus Subscribe<TEvent>(Action<TEvent> handler, int priority = 0) where TEvent : class;

    IEventBus Subscribe<TEvent>(Func<TEvent, Task> handler, int priority = 0) where TEvent : class;

    IEventBus Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class;

    IEventBus Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class;

    TEvent Publish<TEvent>(TEvent eventData) where TEvent : class;

    Task<TEvent> PublishAsync<TEvent>(TEvent eventData) where TEvent : class;

    void Clear();

    void Clear<TEvent>() where TEvent : class;
}