using System.Collections.Concurrent;
using Codexus.ModSDK.Event;

namespace Codexus.ModHost.Event;

public class EventBus : IEventBus
{
    private static readonly Lazy<EventBus> LazyInstance = new(() => new EventBus());

    private readonly ConcurrentDictionary<Type, List<EventHandler>> _handlers = new();
    private readonly Lock _lock = new();

    private EventBus()
    {
    }

    public static EventBus Instance => LazyInstance.Value;

    public IEventBus Subscribe<TEvent>(Action<TEvent> handler, int priority = 0) where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(handler);

        var eventType = typeof(TEvent);
        var eventHandler = new EventHandler
        {
            Priority = priority,
            SyncHandler = obj => handler((TEvent)obj)
        };

        lock (_lock)
        {
            var handlers = _handlers.GetOrAdd(eventType, _ => []);
            handlers.Add(eventHandler);
            handlers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        return this;
    }

    public IEventBus Subscribe<TEvent>(Func<TEvent, Task> handler, int priority = 0) where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(handler);

        var eventType = typeof(TEvent);
        var eventHandler = new EventHandler
        {
            Priority = priority,
            AsyncHandler = obj => handler((TEvent)obj)
        };

        lock (_lock)
        {
            var handlers = _handlers.GetOrAdd(eventType, _ => []);
            handlers.Add(eventHandler);
            handlers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        return this;
    }

    public IEventBus Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(handler);

        var eventType = typeof(TEvent);

        lock (_lock)
        {
            if (!_handlers.TryGetValue(eventType, out var handlers)) return this;

            handlers.RemoveAll(h => h.SyncHandler?.Target == handler.Target &&
                                    h.SyncHandler?.Method == handler.Method);

            if (handlers.Count == 0) _handlers.TryRemove(eventType, out _);
        }

        return this;
    }

    public IEventBus Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(handler);

        var eventType = typeof(TEvent);

        lock (_lock)
        {
            if (!_handlers.TryGetValue(eventType, out var handlers)) return this;

            handlers.RemoveAll(h => h.AsyncHandler?.Target == handler.Target &&
                                    h.AsyncHandler?.Method == handler.Method);

            if (handlers.Count == 0) _handlers.TryRemove(eventType, out _);
        }

        return this;
    }

    public TEvent Publish<TEvent>(TEvent eventData) where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(eventData);

        var eventType = typeof(TEvent);

        if (!_handlers.TryGetValue(eventType, out var handlers))
            return eventData;

        List<EventHandler> handlersCopy;
        lock (_lock)
        {
            handlersCopy = new List<EventHandler>(handlers);
        }

        foreach (var handler in handlersCopy)
            try
            {
                if (handler.SyncHandler != null)
                    handler.SyncHandler(eventData);
                else
                    handler.AsyncHandler?.Invoke(eventData).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing event handler: {ex}");
            }

        return eventData;
    }

    public async Task<TEvent> PublishAsync<TEvent>(TEvent eventData) where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(eventData);

        var eventType = typeof(TEvent);

        if (!_handlers.TryGetValue(eventType, out var handlers))
            return eventData;

        List<EventHandler> handlersCopy;
        lock (_lock)
        {
            handlersCopy = new List<EventHandler>(handlers);
        }

        foreach (var handler in handlersCopy)
            try
            {
                if (handler.AsyncHandler != null)
                    await handler.AsyncHandler(eventData);
                else
                    handler.SyncHandler?.Invoke(eventData);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing event handler: {ex}");
            }

        return eventData;
    }

    public void Clear()
    {
        _handlers.Clear();
    }

    public void Clear<TEvent>() where TEvent : class
    {
        var eventType = typeof(TEvent);
        _handlers.TryRemove(eventType, out _);
    }

    private class EventHandler
    {
        public int Priority { get; init; }
        public Action<object>? SyncHandler { get; init; }
        public Func<object, Task>? AsyncHandler { get; init; }
    }
}