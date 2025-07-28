using DjayEvents.Attributes;
using DjayEvents.Core;
using DjayEvents.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace DjayEvents.UnitTests;

[TestClass]
public class EventBindingTests
{
    [TestMethod]
    public void DirectMode_BindsEventsImmediately()
    {
        var services = new ServiceCollection();
        services.AddScoped<AccountManager>();
        services.AddScoped<GameManager>();
        services.AddEventBinding(EventBindingMode.Direct);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var initializer = scope.ServiceProvider.GetRequiredService<EventBindingScopeInitializer>();
        initializer.Initialize();

        var account = scope.ServiceProvider.GetRequiredService<AccountManager>();
        var game = scope.ServiceProvider.GetRequiredService<GameManager>();

        account.Raise(new UserCreatedEvent { Name = "Direct" });

        Assert.AreEqual("Direct", game.Captured?.Name);
    }

    [TestMethod]
    public void InMemoryQueueMode_QueuesEventsAndProcessesAsync()
    {
        var services = new ServiceCollection();
        services.AddScoped<AccountManager>();
        services.AddScoped<GameManager>();
        services.AddEventBinding(EventBindingMode.InMemoryQueue);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var initializer = scope.ServiceProvider.GetRequiredService<EventBindingScopeInitializer>();
        initializer.Initialize();

        var account = scope.ServiceProvider.GetRequiredService<AccountManager>();
        var game = scope.ServiceProvider.GetRequiredService<GameManager>();

        account.Raise(new UserCreatedEvent { Name = "Queued" });

        // Дать время обработчику в фоне
        Thread.Sleep(200);
        
        Assert.AreEqual("Queued", game.Captured?.Name);
    }

    public class UserCreatedEvent
    {
        public string? Name { get; set; }
    }

    public class AccountManager
    {
        public event EventHandler<UserCreatedEvent>? UserCreated;

        public void Raise(UserCreatedEvent evt)
        {
            UserCreated?.Invoke(this, evt);
        }
    }

    public class GameManager
    {
        public UserCreatedEvent? Captured;

        [HandlesEvent]
        public void OnUserCreated(UserCreatedEvent evt)
        {
            Captured = evt;
        }
    }
}