# About
All projects in this namespace contain only **static classes with extension methods**. 

Use the [XpandPwsh](https://github.com/eXpandFramework/XpandPwsh) To list all the published extension packages:

```ps1
Get-XpandPackages Release XAFExtensions

Id                                Version   Source
--                                -------   ------
Xpand.Extensions.XAF.Xpo          2.201.2.0 Release
Xpand.Extensions                  2.201.2.0 Release
Xpand.Extensions.Mono.Cecil       2.201.2.0 Release
Xpand.Extensions.XAF              2.201.2.0 Release
Xpand.Extensions.Reactive         2.201.2.0 Release
Xpand.Extensions.Reactive.Relay   2.201.2.0 Release
```

Similarly if you wish to install them you can do:
```ps1
Get-XpandPackages Release XAFExtensions|Install-Package
```

These packages are consumed from the modules of this repository therefore they are unit tested together with their consumers. 

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

*   **[The Unbreakable Stream: A Developer & AI Guide](./Xpand.Extensions.Reactive.Relay/docs/The%20Unbreakable%20Stream%20A%20Developer%20&%20AI%20Guide%20to%20Fixing%20the%20Billion-Dollar%20Mistake.md)**
    This is the foundational guide. It explains the core principles of the framework, the distinction between Operator and Item resilience, and the best practices for building robust, "unbreakable" reactive streams. It serves as the architectural contract for the entire system.

*   **[The Reactive Transactional API: Composing Resilient Workflows](./Xpand.Extensions.Reactive.Relay/docs/The%20Reactive%20Transactional%20API%20Composing%20Resilient%20Workflows.md)**
    This document is the formal API reference for the fluent transactional system. It provides detailed explanations and examples for `BeginWorkflow`, `Then`, `RunFailFast`, `RunToEnd`, and other operators used to compose complex, multi-step business processes.

## Installation

Install the package from NuGet:

```shell
dotnet add package Xpand.Extensions.Reactive.FaultHub
```

## Quick Start: Building a Diagnostic Story

The core of the framework is its ability to build a logical stack trace. This is achieved by composing two key operators: `PushStackFrame` to add context and `ChainFaultContext` to establish a resilience boundary.

```csharp
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using System.Reactive.Linq;

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

### **For a Deeper Dive into Advanced Resilience**

The examples above cover the fundamental patterns of **Operator Resilience** (enrich and propagate with `ChainFaultContext`) and **Item Resilience** (enrich, publish, and suppress with `ContinueOnFault` or `...ItemResilient` operators).

For a comprehensive understanding of the framework's architecture and advanced capabilities, please refer to the detailed documentation. It provides in-depth explanations of:

*   **The Two Resilience Models:** A formal comparison of the Propagation vs. Suppression patterns and when to use each.
*   **Building the Diagnostic Story:** A guide to using `PushStackFrame` and `ChainFaultContext` collaboratively to create rich, multi-level diagnostic reports.
*   **Advanced Resilience Operators:** How to use specialized operators like `SwitchOnFault` for fallback logic and `RethrowOnFault` for integrating with error-sensitive services.
*   **XAF Integration:** Best practices for integrating the `FaultHub` with the XAF application lifecycle and displaying user-friendly error notifications.
*   **Programmatic Querying:** How to monitor the global `FaultHub.Bus` to create custom alerting, telemetry, and automated recovery systems.

**Primary Document:**
*   **[The Unbreakable Stream: A Developer & AI Guide to Fixing the Billion-Dollar Mistake.md](./Xpand.Extensions.Reactive.Relay/docs/The%20Unbreakable%20Stream%20A%20Developer%20&%20AI%20Guide%20to%20Fixing%20the%20Billion-Dollar%20Mistake.md)**

***

### **Quick Start: Building Resilient Workflows**

Beyond handling single errors, the eXpandFramework provides a powerful **Transactional API** for composing complex, multi-step operations into robust and observable workflows. This is the recommended starting point for building resilient business logic.

The core idea is to use a fluent API to chain a series of operations, where the framework automatically manages the state, handles failures, and produces detailed diagnostic reports.

---

#### **Example 1: A Simple Fail-Fast Workflow**

Imagine a common scenario: you need to fetch a user's profile, and only if that succeeds, update their status. If the first step fails, the second should never run. This is a "fail-fast" transaction.

**1. Define Your Steps as Observables**

Each step in your workflow is represented by an `IObservable`.

```csharp
// This step succeeds and returns data.
private IObservable<string> FetchUserProfile(int userId) {
    return Observable.Return($"Profile for User {userId}");
}

// This step will fail.
private IObservable<Unit> UpdateStatus(string profile) {
    return Observable.Throw<Unit>(new InvalidOperationException("Database connection failed"));
}
```

**2. Compose the Workflow**

Use `BeginWorkflow`, `Then`, and the `RunFailFast` terminal operator to chain the steps together.

```csharp
var userId = 123;

// 1. Start a new workflow with an explicit name.
var transaction = FetchUserProfile(userId)
    .BeginWorkflow("UserProfileUpdate")
    // 2. Chain the next step. It receives the result of the first step.
    .Then(profileArray => UpdateStatus(profileArray.Single()))
    // 3. Execute the workflow. It will abort on the first error.
    .RunFailFast();

// 4. Subscribe and publish any faults to the central FaultHub.
await transaction.PublishFaults().Capture();
```

**3. Analyze the Rich Error Report**

Because `UpdateStatus` failed, the transaction aborted. The `PublishFaults()` operator sends a detailed report to the `FaultHub.Bus`. If you were to print this exception, you would see:

```
UserProfileUpdate failed [Transaction, Sequential, RunFailFast] <Database connection failed>
└ Update Status [Step]
  • Root Cause: System.InvalidOperationException: Database connection failed
    --- Invocation Stack ---
      at UpdateStatus in ...\MyFeatureTests.cs:line 21
      at FetchUserProfile in ...\MyFeatureTests.cs:line 30
    --- Original Exception Details ---
      System.InvalidOperationException: Database connection failed
```

**What this report tells you:**

*   **The What:** The `UserProfileUpdate` transaction failed.
*   **The Why:** The `UpdateStatus` step was the source of the failure.
*   **The How:** The `Invocation Stack` gives you a clear, logical story of how your code was composed, showing that `UpdateStatus` was called after `FetchUserProfile`. This is far more useful than a standard, noisy .NET stack trace.

---

#### **Example 2: Processing a Batch with `RunToEnd`**

Now, imagine you need to process a batch of items. If one fails, you want to log the error but still attempt to process the rest. This is where the `RunToEnd` strategy shines.

**1. Define the Batch**

We'll create an enumerable of observables, where some will succeed and one will fail.

```csharp
var operations = new[] {
    Observable.Return("Success 1"),
    Observable.Throw<string>(new InvalidOperationException("Failure 1")),
    Observable.Return("Success 2")
};
```

**2. Compose with `RunToEnd`**

This time, we initiate the workflow from the enumerable and use `RunToEnd`.

```csharp
var transaction = operations
    .BeginWorkflow("NotificationBatch", mode: TransactionMode.Sequential)
    .RunToEnd(); // This will execute all operations, even after a failure.

await transaction.PublishFaults().Capture();```

**3. Analyze the Aggregated Report**

The `RunToEnd` operator collects all failures and produces a single summary report.

```
NotificationBatch completed with errors [Transaction, Sequential, RunToEnd] <Failure 1>
└ operations[1] [Step]
  • Root Cause: System.InvalidOperationException: Failure 1
    --- Invocation Stack ---
      at ...
    --- Original Exception Details ---
      System.InvalidOperationException: Failure 1


**Key differences:**

*   The header now says **"completed with errors"** instead of "failed," indicating the transaction was not aborted.
*   If there were multiple failures, they would all be listed under the root `NotificationBatch` node.
*   The successful operations ("Success 1", "Success 2") still completed.

---

### **Putting It All Together**

The Transactional API is the high-level, declarative way to build resilient business logic. It is built upon the same `FaultHub` primitives as the lower-level resilience operators (`ContinueOnFault`, `...ItemResilient`), but provides a structured, compositional model for complex workflows.

*   Use **`RunFailFast`** for sequential, dependent operations where any failure is critical.
*   Use **`RunToEnd`** for batch operations where you want to process all items regardless of individual failures.
*   Use **`RunAndCollect`** when you need to gather results from *all* successful steps into a final collection.

For a deeper dive into advanced features like concurrent steps, fallbacks, and programmatic querying, please refer to the detailed documentation:
*   **[The Reactive Transactional API: Resilient Workflows Composition.md](./Xpand.Extensions.Reactive.Relay/docs/The%20Reactive%20Transactional%20API%20Composing%20Resilient%20Workflows.md)**
*   **[Case Study - A Resilient Web Scraper.md](./Xpand.Extensions.Reactive.Relay/docs/Case%20Study%20-%20A%20Resilient%20Web%20Scraper.md)**