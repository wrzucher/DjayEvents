# DjayEvents Binding Library

**DjayEvents** is a .NET library powered by *Source Generators* that automatically wires up events between services.  
It is designed for scenarios where your application can run as a **monolith** or be split into **microservices** — without rewriting your business logic.

---

## 📌 Core Idea

- You only define **events** and **handlers**.  
- The library automatically discovers and wires them together.  

So you can:
- Run as a **monolith** with direct, fast event handling.
- Switch to **microservices** by enabling queued events (a message broker).  
- During **development**, keep everything in one monolith for simplicity, then split into microservices at release.

---

## 🚀 Installation

```bash
dotnet add package DjayEvents
```

## 🛠 Usage

1. Define an event and a publisher

```csharp
public class UserCreatedEvent
{
    public string Name { get; set; } = "";
}

public class AccountManager
{
    public event EventHandler<UserCreatedEvent>? UserCreated;

    public void Raise(UserCreatedEvent evt) => UserCreated?.Invoke(this, evt);
}
```

2. Create a handler with the [HandlesEvent] attribute

```csharp
public class GameManager
{
    [HandlesEvent]
    public void OnUserCreated(UserCreatedEvent evt)
    {
    }
}
```

3. Register services and choose a binding mode

```csharp
builder.Services.AddScoped<AccountManager>();
builder.Services.AddScoped<GameManager>();

// Direct mode (fast, synchronous)
builder.Services.AddEventBinding(EventBindingMode.Direct);

// or InMemoryQueue mode (asynchronous event queues)
builder.Services.AddEventBinding(EventBindingMode.InMemoryQueue);
```

4. Use in runtime

```csharp
using var scope = provider.CreateScope();
scope.ServiceProvider.GetRequiredService<EventBindingScopeInitializer>().Initialize();

var account = scope.ServiceProvider.GetRequiredService<AccountManager>();
account.Raise(new UserCreatedEvent { Name = "Alice" });
```

## ✅ Benefits

📦 Zero manual wiring — handled via *Source Generator*.

🔄 Switch between monolith and microservices with one line.

⚡ Scoped binding — each DI scope auto-binds events.

🧩 Extensible — easily plug in real brokers like Kafka or RabbitMQ later.

🛠 Great for development — start as a monolith, scale to microservices later.

## 🔮 Roadmap
- Support for distributed brokers (Kafka, RabbitMQ, Azure Service Bus).

- Configurable retry policies and deduplication.

- Observability integration (Prometheus / OpenTelemetry).