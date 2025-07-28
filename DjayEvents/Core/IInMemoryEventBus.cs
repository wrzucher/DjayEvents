namespace DjayEvents.Core;

public interface IInMemoryEventBus
{
    void Publish<T>(T @event);
    void Subscribe<T>(Action<T> handler);
}