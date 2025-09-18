# Xpand.Extensions.Reactive.Relay

[![Nuget](https://img.shields.io/nuget/v/Xpand.Extensions.Reactive.Relay.svg)](https://www.nuget.org/packages/Xpand.Extensions.Reactive.Relay/)

An advanced resilience and error-handling framework for Rx.NET, designed to provide deep, contextual diagnostics and composable, resilient workflows. It replaces lost physical stack traces in asynchronous operations with a clean, logical "story" of the operation, making complex reactive chains fully debuggable.

### Key Features

*   **Contextual Errors:** Instead of a raw exception, you get a `FaultHubException` that tells the full story of the business operation that failed.
*   **Logical Stack Tracing:** Automatically builds a meaningful, causal stack trace that is preserved across schedulers and asynchronous boundaries.
*   **Dual Resilience Models:** Provides distinct patterns for **Operator Resilience** (enrich and propagate errors) and **Item Resilience** (enrich, publish, and suppress errors).
*   **Fluent Transactional API:** A powerful `BeginWorkflow` and `Then` interface for composing complex, multi-step workflows with sophisticated resilience and data salvage strategies.
*   **Queryable Fault Bus:** A centralized `FaultHub.Bus` for system-wide monitoring, alerting, and implementing automated recovery logic.

## Documentation

This framework is documented through two primary guides that cover its philosophy, core patterns, and formal API.

*   **[The Unbreakable Stream: A Developer & AI Guide](./docs/The%20Unbreakable%20Stream%20A%20Developer%20&%20AI%20Guide%20to%20Fixing%20the%20Billion-Dollar%20Mistake.md)**
    This is the foundational guide. It explains the core principles of the framework, the distinction between Operator and Item resilience, and the best practices for building robust, "unbreakable" reactive streams. It serves as the architectural contract for the entire system.

*   **[The Reactive Transactional API: Composing Resilient Workflows](./docs/The%20Reactive%20Transactional%20API%20Composing%20Resilient%20Workflows.md)**
    This document is the formal API reference for the fluent transactional system. It provides detailed explanations and examples for `BeginWorkflow`, `Then`, `RunFailFast`, `RunToEnd`, and other operators used to compose complex, multi-step business processes.

## Installation

Install the package from NuGet:

```shell
dotnet add package Xpand.Extensions.Reactive.Relay
```

## Quick Start: Building a Diagnostic Story

The core of the framework is its ability to build a logical stack trace. This is achieved by composing two key operators: `PushStackFrame` to add context and `ChainFaultContext` to establish a resilience boundary.

```csharp


public class OrderService {
    // The top-level operation that represents the main business logic.
    // It calls a helper and establishes the resilience boundary.
    public IObservable<Unit> ProcessOrder()
        => GetOrderDetails()
            .PushStackFrame() // Adds "ProcessOrder" to the story.
            .ChainFaultContext(["Boundary"]); // Catches the error and captures the full story.

    // A mid-level helper method.
    private IObservable<Unit> GetOrderDetails()
        => ValidateCustomerAsync()
            .PushStackFrame(); // Adds "GetOrderDetails" to the story.

    // The lowest-level method where the async error occurs.
    private IObservable<Unit> ValidateCustomerAsync()
        => Observable.Timer(TimeSpan.FromMilliseconds(20))
            .SelectMany(_ => Observable.Throw<Unit>(new InvalidOperationException("Async Failure")))
            .PushStackFrame(); // Adds "ValidateCustomerAsync", the origin of the failure.
}

// Somewhere in your application startup:
var orderService = new OrderService();

// Subscribe to the central bus to see the final report.
FaultHub.Bus.Subscribe(fault => Console.WriteLine(fault));

// Execute the operation.
orderService.ProcessOrder().PublishFaults().Subscribe();

/*
Expected Console Output:

Process Order completed with errors (Boundary) <Async Failure>
--- Invocation Stack ---
  at ValidateCustomerAsync in ...\OrderService.cs:line 21
  at GetOrderDetails in ...\OrderService.cs:line 15
  at ProcessOrder in ...\OrderService.cs:line 9
--- Original Exception Details ---
  System.InvalidOperationException: Async Failure
  ...
*/
```