using DjayEvents.Core;

namespace DjayEvents.Extensions;

public class EventBindingAutoBinder : IDisposable
{
    public EventBindingAutoBinder(EventBindingScopeInitializer initializer)
    {
        initializer.Initialize();
    }

    public void Dispose()
    {
        // We have to think about it someday
    }
}