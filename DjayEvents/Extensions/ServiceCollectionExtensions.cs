using DjayEvents.Core;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace DjayEvents.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEventBinding(this IServiceCollection services, EventBindingMode mode = EventBindingMode.Direct)
    {
        if (mode == EventBindingMode.InMemoryQueue)
        {
            services.AddSingleton<IInMemoryEventBus, InMemoryEventBus>();
        }

        // Зарегистрировать все сгенерированные байндеры
        var binderTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IEventBinder).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var binderType in binderTypes)
        {
            services.AddScoped(typeof(IEventBinder), binderType);
        }

        services.AddScoped<EventBindingScopeInitializer>();

        // Для автоматического биндинга при создании скоупа
        services.AddScoped<EventBindingAutoBinder>();

        return services;
    }
}