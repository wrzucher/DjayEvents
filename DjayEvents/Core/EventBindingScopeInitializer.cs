namespace DjayEvents.Core;

public class EventBindingScopeInitializer(IEnumerable<IEventBinder> binders)
{
    private readonly IEnumerable<IEventBinder> binders = binders;

    public void Initialize()
    {
        foreach (var binder in this.binders)
        {
            binder.Bind();
        }
    }
}