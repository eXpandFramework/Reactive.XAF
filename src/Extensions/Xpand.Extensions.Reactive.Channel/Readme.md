# The Reactive Channel API

[![Nuget](https://img.shields.io/nuget/v/Xpand.Extensions.Reactive.Utility.svg)](https://www.nuget.org/packages/Xpand.Extensions.Reactive.Utility/)

A type-safe, memory-managed mechanism for **Behavioral Injection**. It allows developers to intercept, transform, and suppress observable streams at runtime without modifying method signatures, constructor dependencies, or architectural boundaries.

### The Problem: Interface Proliferation

Modern .NET architectures rely heavily on Dependency Injection (DI) to achieve testability and modularity. However, this pattern imposes a significant architectural tax: **Interface Proliferation**.

When you need to inject logging, instrumentation, feature flags, or test doubles into existing business logic, you are forced to:
1.  Define an abstraction (`ILogger<T>`, `IMetrics`, `IFeatureToggle`).
2.  Modify the constructor signature.
3.  Update all call sites and DI registrations.

For mature codebases, this **Constructor Ceremony** creates friction that prevents proper separation of concerns. It couples cross-cutting logic to the object graph's lifetime, making temporal behaviors—such as temporary test overrides or dynamic feature injection—cumbersome to implement.

### The Solution: Behavioral Injection

**The Reactive Channel API** solves this by introducing **Behavioral Injection**: a compile-time type-safe mechanism backed by `System.Reactive` and `MemoryCache`. Unlike traditional DI's resolution-at-startup model, ReactiveChannel operates on **Emission-Time Resolution**: handlers are bound when data flows, not when objects are constructed.

### Key Features

*   **Zero-Signature Modification:** Inject logic into methods without changing their parameters or the class constructor.
*   **Emission-Time Resolution:** Bind handlers dynamically when data flows, enabling "Flash Behaviors" that exist only for the duration of a request or test.
*   **Auto-Expiring Channels:** Channels are backed by `MemoryCache` with sliding expiration, ensuring no memory leaks from long-running processes.
*   **Contextual Scoping:** Use `[CallerMemberName]` to surgically target specific call sites for injection, avoiding global side effects.
*   **Global Fault Integration:** Errors within handlers automatically propagate to the `FaultHub`, ensuring no silent failures even in decoupled workflows.

## Documentation

*   **[The Reactive Channel API: Behavioral Injection for Observable Workflows](./docs/The%20Reactive%20Channel%20API%20-%20Behavioral%20Injection%20for%20Observable%20Workflows.md)**
    The comprehensive architectural guide. It details the "Signature Preservation Problem," the three core behavioral patterns (Injection, Suppression, Contextual Injection), and the memory management model that makes this approach safe for high-frequency trading and production environments.

## Installation

The Reactive Channel API is part of the Xpand Reactive Extensions suite.

```shell
dotnet add package Xpand.Extensions.Reactive.Utility
```

## Quick Start: Testing Without Mocks

The most immediate benefit of the Reactive Channel API is **Zero-Mock Testing**. You can test a specific behavior deep within a method without mocking the entire class or its dependencies.

```csharp
public class OrderService {
    // Standard business logic. Note: No IValidator injected in constructor.
    // The "Validation" channel acts as an optional hook.
    public IObservable<Order> ProcessOrder(Order order) 
        => Observable.Return(order)
            .Inject("Validation") // The injection point. 
            .Select(SaveToDatabase);
            
    private Order SaveToDatabase(Order o) { /*...*/ }
}

// In your Test Fixture:
[Test]
public void ProcessOrder_Aborts_On_Validation_Failure() {
    var service = new OrderService();
    var invalidOrder = new Order { Amount = -100 };

    // BIND: Intercept the "Validation" point for this specific test scope.
    // We replace the stream item with an error, simulating a validation failure.
    using var _ = "Validation".HandleRequest()
        .With<Order, Order>(o => Observable.Throw<Order>(new InvalidOperationException("Invalid!")))
        .Subscribe();

    // ACT: Run the actual service method.
    var observer = service.ProcessOrder(invalidOrder).Test();

    // ASSERT: Verify the behavior changed without mocking the service.
    observer.Error.ShouldBeOfType<InvalidOperationException>();
    observer.ErrorMessage.ShouldBe("Invalid!");
}
```