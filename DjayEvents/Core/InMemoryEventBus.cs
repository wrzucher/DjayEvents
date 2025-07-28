using System.Collections.Concurrent;

namespace DjayEvents.Core;

public class InMemoryEventBus : IInMemoryEventBus, IDisposable
{
    private readonly ConcurrentDictionary<Type, object> _queues = new();

    public void Publish<T>(T @event)
    {
        var queue = (BlockingCollection<T>)_queues.GetOrAdd(typeof(T), _ =>
        {
            var q = new BlockingCollection<T>();
            StartWorker(q);
            return q;
        });

        queue.Add(@event);
    }

    public void Subscribe<T>(Action<T> handler)
    {
        var queue = (BlockingCollection<T>)_queues.GetOrAdd(typeof(T), _ =>
        {
            var q = new BlockingCollection<T>();
            StartWorker(q);
            return q;
        });

        StartWorker(queue, handler);
    }

    private void StartWorker<T>(BlockingCollection<T> queue, Action<T>? handler = null)
    {
        if (handler == null)
        {
            // Если без обработчика — создаём пустой воркер (чтобы очередь создавалась)
            return;
        }

        Task.Run(() =>
        {
            foreach (var evt in queue.GetConsumingEnumerable())
            {
                try
                {
                    handler(evt);
                }
                catch { /* логгировать */ }
            }
        });
    }

    public void Dispose()
    {
        foreach (var q in _queues.Values)
        {
            ((IDisposable)q).Dispose();
        }
    }
}