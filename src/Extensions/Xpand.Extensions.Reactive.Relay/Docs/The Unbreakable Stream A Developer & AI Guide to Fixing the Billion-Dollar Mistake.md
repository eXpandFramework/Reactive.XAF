### **The Unbreakable Stream: A Developer & AI Guide to Fixing the Billion-Dollar Mistake**

This guide provides a comprehensive overview of the sophisticated, two-tiered error-handling model in the eXpandFramework. It is designed to make your applications more robust, your debugging process easier, and to serve as the foundational architectural contract for the entire Xpand.XAF.Modules.\* ecosystem.

#### **1. Why the Change? (Fixing Rx's Billion-Dollar Mistake)**

In complex applications, when something goes wrong, two questions are often hard to answer:

* **"What was the application trying to do when the error happened?"**
  A simple error message like *Object reference not set to an instance of an object* is useless without knowing the business context. Was it loading user settings? Processing a payment? The new system solves this by automatically building a detailed "story" or "call stack" for your operations. When an error occurs, you get a clear history of the steps that led to it.

* **"Should this single error stop everything?"** The default behavior of Reactive Extensions (Rx) is for any unhandled error to terminate the entire stream. This "fail-fast" principle, while simple, is a modern incarnation of the same thinking that led to the null reference being called the `billion-dollar mistake`. It forces developers to manually and inconstantly sprinkle `Catch` operators throughout their code, leading to brittle, hard-to-reason-about streams. This framework challenges this fail-fast-by-default philosophy by providing a new, structured approach to resilience. It replaces the need for manual `Catch` operators with a composable system, allowing you to gracefully manage partial failures without bringing everything to a halt..

#### **2. A Note on Terminology**

The following terms are used to describe the relationships between operators and streams.

* **Upstream / Downstream (Chaining):** Describes the flow of data. In `source.OperatorA().OperatorB()`, `OperatorA` is upstream of `B`.

* **Outer / Inner (Projection / Creation):** Describes one observable creating another, typically inside `SelectMany`. The stream that contains the `SelectMany` is the **Outer** stream; the stream created inside the projection is the **Inner** stream.

#### **3. Core Principles at a Glance**

This section summarizes the fundamental principles of the eXpandFramework's error handling system.

* **Rule 1: The Default Pattern is Propagation** To handle errors in a standard reactive stream, compose it with the `.ChainFaultContext()` operator. By default, it enriches an error with context and then propagates it downstream or outerstream. It is also the designated operator for applying resilience policies, such as retries, to the stream it manages. This is the standard pattern for Operator Resilience.

* **Rule 2: Specialized Operators Provide Suppression** For scenarios where a single item's failure should not terminate the parent stream (e.g., inside `SelectMany`), use a specialized `...ItemResilient` operator. Its purpose is to suppress an inner stream's error by enriching it, publishing it to the `FaultHub`, and then gracefully completing the inner stream. This is the pattern for Item Resilience.

* **Rule 3: Inner Contexts Yield to Outer Contexts** If a `ChainFaultContext` detects it is running inside another `ChainFaultContext`, it will always propagate its enriched error to the outer context, allowing the outer context's logic (e.g., a retry strategy) to take precedence.

* **Rule 4: Explicit Handlers Always Win** An explicit, local handler instruction from an operator like `CompleteOnFault` or `RethrowOnFault` will be honored immediately, overriding any default or nesting behavior.

* **Rule 5: Framework Event Helpers Enable Resilience at the Source.** High-level helpers like `ProcessEvent`, specifically the overloads that accept a selector function, provide Item Resilience by default for the logic within that selector. This ensures the underlying event subscription is not terminated by a single failed event within the selector's logic.

#### **4. The Resilience Models**

The framework provides two distinct and complementary resilience models. It is critical to understand their different purposes, as they are not interchangeable. One model, **Operator Resilience**, is designed for **error propagation**, allowing outer layers to react to failures. The other, **Item Resilience**, is designed for **error suppression**, allowing a parent stream to survive the failure of a single item. Using the suppression pattern where the propagation pattern is needed will break critical features like the stacking of logical context from nested operations and the ability to trigger transactional retries from an outer layer. The following table summarizes when to use each model.

| Feature | Operator Resilience (Propagation Pattern) | Item Resilience (Suppression Pattern) | 
 | ----- | ----- | ----- | 
| **Primary Goal** | Enrich and **Propagate** Error | "Enrich, Publish, and **Suppress** Error" | 
| **Allows Context Stacking?** | Yes (Essential for nested logic) | Yes (Preserves and adds to existing logical stacks) | 
| **Triggers Outer Retries?** | Yes (Enables transactional resilience) | No (Errors do not propagate to outer layers) | 
| **Use** Case | Nested business logic; operations that must succeed or fail as a whole. | "Processing discrete items in a long-running stream (e.g., UI events, message queues)." | 

##### **4.1. Operator Resilience (The Propagation Pattern)**

Operator Resilience is not a default behavior of the framework's operators but rather a pattern that the consumer is expected to apply to achieve robust error handling. The framework enables this pattern through the `.ChainFaultContext()` operator.

* **Implementation:** To make a reactive stream resilient, the consumer composes a native Rx operator with `.ChainFaultContext()`.

  ```csharp
  // Standard composition for operator resilience
  var stream = 1.Hours().Interval().Select(i => Process(i))
      .ChainFaultContext(["GetData"]);
  
  ```

* **Behavior:** When an error occurs anywhere within the operator's stream, `ChainFaultContext()` catches the error, enriches it with a full logical story, and then propagates it by default. This allows downstream/outer operators (like retry mechanisms) or a final `.PublishFaults()` call to process the now-contextualized fault. This is the recommended pattern for most reactive streams where an error should terminate the current operation and be handled by an outer layer.

##### **4.2. Item Resilience (The Composable Suppression Pattern)**

Item Resilience is a more granular, specialized model designed for scenarios where the failure of a single unit of work should not terminate the parent stream. It is ideal for long-running processes that handle discrete events or items, such as event listeners or message queues.

* **Naming Convention and Scope:** This resilience pattern is provided by a family of operators that share the same core suppression logic.

  * Operators suffixed with `...ItemResilient` (e.g., `DoItemResilient`, `SelectManyItemResilient`) are designed to apply the suppression pattern to the logic associated with *each individual item* as it flows through the stream.

  * The `ContinueOnFault` operator applies the exact same suppression logic, but to the *entire stream* it is appended to, rather than on a per-item basis. While it does not share the suffix, it is a primary member of this resilience family, serving as a concise tool for stream-level suppression.

* **Behavior:** When an error occurs, the item resilience pattern manages the fault:

  1. **Preserve & Enhance:** The `PushStackFrame()` call adds the current operator's context to the `LogicalStackContext`, preserving any stack built by upstream operators.

  2. **Suppress & Publish:** The `.Catch()` block enriches the exception with the full logical stack, publishes it to the `FaultHub`, and gracefully completes the inner stream.

  3. **Continue:** Because the inner stream completes without error from the perspective of the parent operator (e.g., `SelectMany`), the parent stream remains active and can process the next item.

* **Adding Retries to Item Resilience:** These operators can also attempt to recover from transient failures before suppressing an error. Most item resilience operators have an overload that accepts a `retryStrategy` delegate. This allows you to apply policies like `Retry` or `RetryWithBackoff` to the specific item being processed.

  ```csharp
  // Example of retrying a failing operation for a single item.
  var stream = source.SelectManyItemResilient(item => {
      return ProcessItemThatMightFail(item);
  },
  // This retry policy applies ONLY to this specific item.
  // If it fails 3 times, the error will be suppressed by SelectManyItemResilient,
  // and the parent stream will continue to the next item.
  retryStrategy: innerStream => innerStream.RetryWithBackoff(3, TimeSpan.FromMilliseconds(100)));
  
  ```

###### **4.2.1. Guideline: Choosing Between `...ItemResilient` and `ContinueOnFault`**

The choice between a specialized `...ItemResilient` operator and the general `ContinueOnFault` operator depends entirely on the scope at which you need to handle failures.

* **Use** `...ItemResilient` for **Per-Item Projections:** When using projection operators like `SelectMany`, you must use the corresponding `...ItemResilient` variant (`SelectManyItemResilient`). This applies the suppression logic to the inner stream created for each item, correctly isolating failures and allowing the parent `SelectMany` to continue with the next item.

  ```csharp
  // CORRECT: Isolates failures for each item.
  var results = source.SelectManyItemResilient(item => {
      if (item.IsInvalid) {
          return Observable.Throw<string>(new Exception("Invalid item"));
      }
      return Observable.Return(item.Process());
  });
  // If one item throws, the error is published, and the stream proceeds to the next item.
  
  ```

* **Use `ContinueOnFault` for Entire Streams:** Use `ContinueOnFault` when you want an entire observable chain to suppress its error and complete gracefully. It is often used on streams that represent a single, self-contained unit of work. Applying it after a `SelectMany` that does *not* use internal item resilience will terminate the `SelectMany` on the first inner failure.

  ```csharp
  // INCORRECT for per-item resilience: This will not work as intended.
  var results = source.SelectMany(item => { // Using standard SelectMany
      if (item.IsInvalid) {
          return Observable.Throw<string>(new Exception("Invalid item"));
      }
      return Observable.Return(item.Process());
  }).ContinueOnFault(); // Applied to the outer stream
  // The first invalid item will throw, terminating the SelectMany.
  // ContinueOnFault will then suppress that error, but no further items will be processed.
  
  ```

###### **4.2.2.** Common Pattern: Providing **a Fallback for `ContinueOnFault`**

`ContinueOnFault` is designed to suppress an error and complete the stream. This is useful for resilience, but often you need to continue the chain with a default value. The standard and most idiomatic Rx pattern to handle this is to chain the `.DefaultIfEmpty()` operator. This operator ensures that a potentially empty stream will always emit at least one item.

* **Example: Fallback to an Empty List**
  A common use case is fetching data that might fail. If the fetch fails, you want to proceed with an empty list rather than terminating the entire operation.

  ```csharp
  // Fetches a list of items, but if the operation fails, proceeds with an empty list.
  IObservable<IList<string>> GetResilientItems() {
      return GetItemsFromDatabase() // Returns IObservable<IList<string>>
          .ContinueOnFault()      // If it fails, it becomes an empty stream that just completes.
          .DefaultIfEmpty(new List<string>()); // If the stream was empty, this emits a new empty list instead.
  }
  
  // The rest of the chain is now guaranteed to execute with a valid, non-null list.
  GetResilientItems()
      .SelectMany(list => /* ... process the list, which might be empty ... */)
      .Subscribe();
  
  ```

This pattern guarantees that the stream emits exactly one item—either the successful result or the default value—allowing subsequent operators like `SelectMany` to execute reliably.

###### 4.2.3. Interaction with Upstream Resilience

A critical design feature of the Item Resilience pattern is its behavior when encountering an exception that has *already* been enriched by an upstream resilience boundary (i.e., it is already a `FaultHubException`). In this scenario, the system prioritizes preserving the existing, rich context.

When an operator like `ContinueOnFault` catches an existing `FaultHubException`, it performs a **wrapping** operation:

1. It creates a new `FaultHubException` to represent its own resilience boundary, adding its call-site information to the logical stack.

2. The original, upstream `FaultHubException` is set as the `InnerException`, preserving its entire context.

3. Crucially, any new `context` data passed to the item resilience operator (e.g., `ContinueOnFault(context: ["New Data"])`) is **not added to the new wrapper's `UserContext`**. This is an intentional design choice to prioritize the richer, more detailed context from the upstream `FaultHubException`.

This ensures that the detailed story built by `ChainFaultContext` is never accidentally overwritten by a less specific context at the point of suppression. While the passed-in context data is still captured within the call-site's logical stack frame for detailed tracing, developers should be aware that it will not appear in the top-level context of the resulting error report if a richer context already exists.

##### 4.2.4. Completing the Pattern: Handling Subscription and Disposal Errors

A key aspect of the framework's robustness is its ability to handle errors that are normally invisible to standard Rx `Catch` operators. Exceptions can occur not just within the stream (as `OnError` notifications) but also during the subscription process itself or, more commonly, when a resource is being disposed of (e.g., in an `Observable.Using` block).

To close this gap, all framework `...ItemResilient` operators are internally composed with a final `SafeguardSubscription` step. This mechanism wraps the entire subscription lifecycle. If an exception is thrown during subscription or disposal, `SafeguardSubscription` ensures it is captured, enriched, and published to the `FaultHub`, providing a truly complete resilience pattern where no error can go unreported .

##### **4.3. Advanced Resilience Operators**

Beyond the primary resilience patterns, the framework provides several advanced operators for more specialized error-handling scenarios.

###### **4.3.1. SwitchOnFault**

The `SwitchOnFault` operator is a powerful hybrid that combines the resilience boundary of `ChainFaultContext` with the fallback mechanism of a `.Catch` block. Its primary purpose is to intercept a fully contextualized `FaultHubException` and, instead of just propagating or suppressing it, switch to an entirely different `IObservable` stream. This is ideal for scenarios where a failure in a primary operation should trigger a secondary or compensatory action.

**Example: Switching to a Fallback Data Source**
Imagine an operation that tries to fetch data from a primary, fast cache. If the cache fails, instead of terminating, you want to switch to fetching the data from a slower, more reliable database.

```csharp
private IObservable<string> GetDataWithFallback() {
    return GetDataFromFastCache()
        // The SwitchOnFault operator establishes a resilience boundary.
        .SwitchOnFault(fault => {
            // The fallback selector receives the fully enriched FaultHubException.
            // You can log it, analyze it, and then decide on the next action.
            fault.Publish();
            
            // Now, switch to a completely different stream of work.
            return GetDataFromDatabase();
        }, context: ["Data Retrieval Operation"]);
}

private IObservable<string> GetDataFromFastCache() 
    => Observable.Throw<string>(new InvalidOperationException("Cache unavailable"));

private IObservable<string> GetDataFromDatabase() 
    => Observable.Return("Data from database");

```

In this example, if `GetDataFromFastCache` fails, the `SwitchOnFault` operator catches the error, enables its publication, and then seamlessly subscribes to `GetDataFromDatabase`, allowing the application to self-heal and continue its work.

###### **4.3.2. PublishOnFault**

The `PublishOnFault` operator provides an explicit "log and continue" behavior. It is a variant of `CompleteOnFault` that guarantees the fault is published to the central `FaultHub.Bus` for global monitoring before the local stream is gracefully completed.

This contrasts with `CompleteOnFault`, which defaults to *muting* the exception (i.e., completing the stream without publishing the fault). `PublishOnFault` is for scenarios where a failure is not critical enough to stop the current operation but is significant enough to be logged system-wide.

**Example: Processing Non-Critical Notifications**

```csharp
var notificationStream = GetNotifications()
    .SelectMany(notification => ProcessNotification(notification)
        // If one notification fails, publish the error but continue.
        .PublishOnFault() 
    );

notificationStream.Subscribe();

```

In this scenario, if processing a single notification fails, the error will be sent to the `FaultHub.Bus`, but the parent `notificationStream` will not be terminated. It will continue to process the next available notification.

#### **5. XAF Integration and Configuration**

The `FaultHub.Bus` is a central message bus for errors that is XAF-agnostic. However, within an XAF application, the framework provides a standard pattern for integrating this bus with the application's lifecycle.

##### **5.1. Opting into the Resilience Framework**

It is critical to understand that the entire `FaultHub` resilience system is part of the framework's reactive layer. It is designed to be composed with `IObservable<T>` streams. The resilience framework will **not** automatically handle exceptions thrown from standard, imperative XAF code. For example, an unhandled exception in a traditional `SimpleAction.Execute` event handler will follow the standard .NET propagation path and will not be enriched with `FaultHub` context.

To gain the benefits of contextual errors and the resilience patterns described in this guide, developers must write their business logic within the framework's reactive streams. Recommended entry points include helpers like `WhenExecuted` for actions, and the resilient overload of `WhenFrame` for view-level logic. These high-level operators have **Item Resilience** built-in, making your streams "unbreakable" by default.

While both imperative and reactive errors may ultimately be passed to `XafApplication.HandleException`, only the reactive path provides the rich, contextual `FaultHubException` that tells the full story of the operation.

##### **5.2. Displaying Error Notifications with `Reactive.Logger`**

A powerful way to leverage the `FaultHub` is with the `Reactive.Logger` module. It can intercept exceptions from the bus and transform them into non-blocking notifications. With a simple configuration in your `Model.xafml`, you can display clean messages to the user and automatically store the full, enriched error details in your database for developer analysis.

**Example `Reactive.Logger` Configuration:**

```xml
<Notifications NotifySystemException="True" HandleSystemExceptions="True" DisableValidationResults="True">
    <ReactiveLoggerNotification Id="Errors"
        Criteria="[RXAction] = ##Enum#Xpand.XAF.Modules.Reactive.Logger.RXAction,OnError# And Not Contains([Message], 'ValidationException')"
        ObjectType="Xpand.Extensions.XAF.Xpo.BaseObjects.ErrorEvent"
        ShowXafMessage="True"
        XafMessageType="Error"
        MessageDisplayInterval="15000"
        IsNewNode="True" />
</Notifications>

```

#### **6. Architectural Patterns and Best Practices**

##### **6.1. Resilience at the Source**

Many reactive streams in the eXpandFramework originate from native .NET events. The framework provides a family of helpers for this: `ProcessEvent`. The lower-level `WhenEvent` operator is now considered obsolete. The `ProcessEvent` overloads that accept a `Func<TEventArgs, IObservable<T>> resilientSelector` are the foundational building blocks for many higher-level framework helpers (like `WhenExecuted`). They provide built-in **Item Resilience** for the logic inside the selector, a resilience that is inherited by any operator built on top of them.

While `ProcessEvent` is the foundational operator, the framework builds upon it to provide higher-level, domain-specific helpers that are more expressive and convenient. For the specific domain of XAF Actions, the primary recommended operator is `.When<TEvent, TSource>()`. This helper is designed to react to specific .NET events on `ActionBase` objects. Crucially, it is built on top of `ProcessEvent` and therefore inherits the same robust **Item Resilience** by default. This makes it the standard, "unbreakable" way to handle action events within the framework, as demonstrated in the test suite.

```csharp
// The selector logic is protected by Item Resilience by default.
source.ProcessEvent(eventName, args => {
    // An error thrown here will be caught, published to the FaultHub,
    // and passed to the XafApplication.HandleException method. If the
    // Reactive.Logger module is used, this can be displayed as a
    // non-blocking notification, effectively suppressing the stream crash.
    // The event subscription will remain active.
    return ProcessEventArgs(args);
});

```

For scenarios where the original, non-resilient behavior of `WhenEvent` was required (i.e., an error should propagate and terminate the stream), the recommended pattern is to compose `ProcessEvent` with the `RethrowOnFault` operator.

```csharp
// Simulating the old WhenEvent behavior
source.ProcessEvent(eventName, args => {
    return ProcessEventArgs(args).RethrowOnFault();
});

```

This resilience does not extend beyond the selector. Any operators chained after `ProcessEvent` are subject to the standard **Operator Resilience** rules.



##### **6.2. Building the Story: A Guide to Stack Tracing**

The logical stack trace is not just a convenience; it is a necessary replacement for the physical stack trace that is lost when work is handed off to a **Reactive Extensions (Rx) scheduler**. While modern `async/await` has mechanisms to preserve the call stack, classic Rx operators like `ObserveOn`, `SubscribeOn`, and `Timer` do not. An exception that occurs on a background scheduler thread provides a physical stack trace that is useless for diagnostics. The "full story" captured by the framework—including the chain of internal helpers like `WhenExecuted` and `When`—is a deliberate and essential feature. It provides the complete, unbroken logical path from your code to the source of the error, which is the only way to reliably debug scheduled Rx operations.

The choice between `PushStackFrame()` and `ChainFaultContext()` depends entirely on the **story you want the error to tell**.

###### **6.2.1. The Golden Rule: Compose or Reset?**

When implementing a resilient stream, you must decide:

* Is this operation **one chapter in a larger story?**
  Should it add its context to a trace built by other methods? If yes, use `PushStackFrame`.

* Is this operation the **definitive resilience boundary?** Should it capture a complete, low-level snapshot of what's happening right now, ignoring any previous story?
  If yes, use `ChainFaultContext`.

###### **6.2.2. Practical Examples**

**The** Correct Pattern: **Building a Story Upstream**
To ensure each part of a business operation contributes to the final error story, frames must be added *before* the resilience boundary is established. `PushStackFrame` must always be called **upstream** of `ChainFaultContext`. The former adds a chapter; the latter binds the book. This same principle is automated by the higher-level Transactional API, where operators like `BeginWorkflow` and `Then` automatically push stack frames for each step in the operation.

Consider the following operation, broken into helper methods. The composition ensures the story is built correctly before being finalized.

```csharp
// The user's top-level call, establishing the resilience boundary.
public void ProcessOrder() {
    _ = GetOrderDetails()
        // The consumer adds their business context, fulfilling the Responsibility Principle.
        .PushStackFrame() 
        // This is the resilience boundary. It catches the error propagated
        // from upstream and captures the complete, multi-level story.
        .ChainFaultContext() 
        .PublishFaults();
}

// This helper adds its chapter to the story.
private IObservable<Unit> GetOrderDetails() {
    return ValidateCustomer()
        .PushStackFrame(); // Enriches a propagating error and re-throws it.
}

// This is where the error originates and the first chapter is written.
private IObservable<Unit> ValidateCustomer() {
    return Observable.Throw<Unit>(new Exception("Invalid Customer"))
        .PushStackFrame(); // Catches error, adds frame, and re-throws.
}

```

In this pattern, the error from `ValidateCustomer` is enriched, propagates to `GetOrderDetails` where it is enriched again, and is finally caught and managed by `ChainFaultContext`. The final report tells a complete story: `ValidateCustomer` -> `GetOrderDetails` -> `ProcessOrder`.

**The Incorrect Pattern: Orphaned Frames**
Placing `PushStackFrame` **downstream** of `ChainFaultContext` results in an "orphaned" frame. The resilience boundary has already handled the error, and the story is finalized.

```csharp
// Incorrect: The "ProcessOrder_Orphaned" frame is never included in the report.
public void ProcessOrder_Incorrect() {
    _ = GetOrderDetails()
        // The story is finalized here. Any error is caught and handled.
        .ChainFaultContext()
        // This frame is downstream of the boundary. The error will never reach it.
        .PushStackFrame() 
        .PublishFaults();
}

// Helper method for the incorrect example.
private IObservable<Unit> GetOrderDetails() {
    return Observable.Throw<Unit>(new Exception("Invalid Customer"))
        .PushStackFrame(); // This frame *will* be captured by the ChainFaultContext.
}

```

Here, `ChainFaultContext` catches the error from `GetOrderDetails` and propagates a new, managed `FaultHubException`. The original exception's journey ends there. The downstream `PushStackFrame` in `ProcessOrder_Incorrect` never sees the original error and cannot contribute to its story.

###### **6.2.3.** Summary: **At a Glance**

| Feature | `PushStackFrame()` | `ChainFaultContext()` | 
 | ----- | ----- | ----- | 
| **Primary Goal** | Add a frame to a logical story | Manage a logical story and apply policy | 
| **Effect on Stack** | Preserves and adds to the stack | Provides a clean boundary and captures the full inner stack | 
| **Error Handling** | Snapshots the logical stack on error | Catches and propagates errors | 
| **Typical Use** | Inside composable helper methods | At a main resilience boundary | 

###### **6.2.4. The Responsibility Principle: A Collaborative Stack Trace**

To build a complete and accurate logical stack trace, the responsibility is shared between the framework's helper methods and the consumer's code. A full trace requires both a low-level frame from the library and a high-level frame from the consumer. Adhering to this principle is essential for correct diagnostics.

The process is as follows:

1. **The Library Adds Implementation Context**: A framework helper method (e.g., `WhenExecuted`, `WhenFrame`) is responsible for adding its own context to the logical stack. It does this by making a parameterless `.PushStackFrame()` call internally. The C# compiler automatically provides the method's name, file, and line number, adding a low-level, implementation-specific frame to the story (e.g., "WhenExecuted").

2. **The Consumer Adds Business Context**: The developer consuming the library is responsible for adding the high-level, business-logic context. When composing a meaningful operation, the consumer **must** add their own parameterless `.PushStackFrame()` call at the end of an observable chain that constitutes a single logical operation, and before establishing a new resilience boundary with `ChainFaultContext`.

**Example of a Complete Trace:**
By following this pattern, a complete, multi-level stack trace is naturally assembled. Consider this consumer code:

```csharp
// Consumer's method
public IObservable<Unit> MyFeature_UpdateTotals() {
    return GetSomeAction()
        .WhenExecuted(_ => /* logic */) // Library helper adds "WhenExecuted" frame
        .PushStackFrame();               // Consumer adds "MyFeature_UpdateTotals" frame
}

```

This collaboration results in a rich narrative from the top-level business operation down to the framework implementation details, providing a stack trace like: `MyFeature_UpdateTotals` -> `WhenExecuted`.

###### **6.2.5. Adding Dynamic Business Data**

While the parameterless `PushStackFrame()` is the primary tool for tracing the code's execution path, it is often necessary to include dynamic, business-specific data in the error report. For this purpose, the framework provides an overload that accepts an array of objects.

* **Parameterless `PushStackFrame()`:** Use this inside helper methods to add the method's name to the trace, building the "who called whom" story.

* **Parameterized `PushStackFrame(object[] context)`:** Use this at the lowest level of an operation to add specific, runtime data (e.g., record IDs, user names, state information) that is crucial for debugging the business logic.

By combining these two patterns, you can create a maximally informative error report that tells you not only **where** the code failed, but **what** it was working on.

**Example of a Complete Trace with Business Data:**

```csharp
// The top-level resilience boundary
.WhenExecuted(args => {
    var invoiceId = 42;
    var customerName = "ACME Corp";
    return SequentialTransaction(invoiceId, customerName)
        .ChainFaultContext(
            source => source.RetryWithBackoff(3, TimeSpan.FromMilliseconds(100)),
            ["Process Invoice Action"]
         );
})

private static IObservable<Unit> SequentialTransaction(int invoiceId, string customerName) {
    return ScheduleWork(invoiceId, customerName)
        .PushStackFrame(); // Parameterless: Captures "SequentialTransaction"
}

private static IObservable<Unit> ScheduleWork(int invoiceId, string customerName){
    return Observable.Timer(TimeSpan.FromSeconds(1))
        .SelectMany(l => WorkNow(invoiceId, customerName))
        .PushStackFrame(); // Parameterless: Captures "ScheduleWork"
}

private static IObservable<Unit> WorkNow(int invoiceId, string customerName){
    var @throw = Observable.Throw<Unit>(new Exception("Database disconnected"));
    return @throw
        // Parameterized: Adds dynamic business data to the story.
        .PushStackFrame([$"Processing Invoice ID: {invoiceId} for Customer: '{customerName}'"]);
}

```

**Resulting `FaultHubException`:**

* **Operation:** `Process` Invoice Action

* **Logical Stack:** `Processing Invoice ID: 42 for Customer: 'ACME Corp'` -> `WorkNow` -> `ScheduleWork` -> `SequentialTransaction`

* **Original Exception:** `Database disconnected`

###### **6.2.6. Best Practices for `PushStackFrame`**

To build a clean and meaningful diagnostic story, the decision to use `.PushStackFrame()` should be deliberate. While the framework includes safeguards against the most common forms of stack pollution, following a clear principle will produce the most readable reports.

**The "One Meaningful Step" Principle**
The guiding principle is to **add `.PushStackFrame()` to any method that encapsulates a distinct, meaningful logical operation.**

Before adding `.PushStackFrame()` to a method, ask yourself this question:
*"If* this operation failed and I saw this method's name in the error report, would it help me understand what part of the business process went wrong?"

If the answer is yes, add `.PushStackFrame()`. If the method is merely a trivial helper or a simple data transformation, adding a frame will likely create noise.

**DO** add `.PushStackFrame()` to methods representing business logic steps:

```csharp
// This method represents a clear business step: "Get Order Details"
private IObservable<Unit> GetOrderDetails() {
    return ValidateCustomer()
        .PushStackFrame(); // CORRECT: "GetOrderDetails" is a valuable frame.
}

```

**DO NOT** add `.PushStackFrame()` to trivial helpers:

```csharp
// This is just a simple, generic type conversion.
public IObservable<Unit> AsUnit<T>(this IObservable<T> source) {
    return source.Select(_ => Unit.Default);
    // INCORRECT: Adding .PushStackFrame() here would add a noisy "AsUnit" frame.
}

```

**Framework Safeguard: Automatic Consecutive Duplicate Prevention**
To prevent stack pollution from simple, consecutive, identical calls (e.g., in a recursive helper method), the `PushStackFrame` operator includes a safeguard. Before adding a new frame, it performs an equality check against the frame currently at the top of the logical stack. If the new frame is identical to the existing one, it is discarded.

**Note:** This equality check is comprehensive. It considers the full `LogicalStackFrame` record, which includes not only the **method name** but also the **file path** and any **dynamic context objects** passed to the operator. This means that two consecutive calls from the same method will *not* be considered duplicates if they provide different context data. This ensures that meaningful, state-changing recursive calls are fully traced, while preventing noise from simple, stateless recursion.

###### **6.2.7.** The **Diagnostic System: A Tightly Coupled Pair**

While `PushStackFrame` and `ChainFaultContext` are presented as operators with distinct roles, it is architecturally critical to understand that they form a single, stateful, and tightly coupled diagnostic system. Their collaboration is not merely compositional; it is a mandatory partnership. The `.PublishFaults()` operator, while a valid terminal operator for a stream, does not participate in this partnership and cannot capture the logical stack trace.

The mechanism that binds `PushStackFrame` and `ChainFaultContext` is an internal `FaultSnapshot` object:

1. **`ChainFaultContext` Initiates a "Recording Session":** When a stream is composed with `ChainFaultContext`, the operator creates a `FaultSnapshot` instance and stores it in an `AsyncLocal` variable. This effectively begins a "recording session" for that specific resilience boundary.

2. `PushStackFrame` Snapshots the **Stack on Error:** The `PushStackFrame` operator contains error-specific logic. It materializes the stream to inspect every notification. If it detects an `OnError` notification, it finds the active `FaultSnapshot` from the `AsyncLocal` and saves the current, complete logical stack into it.

3. **`ChainFaultContext` Finalizes the Report:** The `Catch` block within `ChainFaultContext` retrieves the stack from the `FaultSnapshot`, uses it to build the final, enriched `FaultHubException`, and then clears the snapshot.

This proves that `PushStackFrame` is not a passive metadata operator; it is an active **stack-snapshotter-on-error**. It acts as the "sensor" that captures the state at the moment of failure, but it relies on `ChainFaultContext` to provide the "recorder" (`FaultSnapshot`) to save that state into. Without an active `ChainFaultContext` boundary upstream, the work done by `PushStackFrame` is ephemeral and will be lost, which is why the composition of the two is a strict requirement for the system to function as designed.

##### **6.3. The Handler Precedence System: A Non-Linear Override Mechanism**

A core architectural pillar that enables the framework's declarative override capability (`Rule 4: Explicit Handlers Always Win`) is the Handler Precedence System. This system deviates from standard linear Rx operator composition, creating a powerful but non-obvious "spooky action at a distance" where a downstream operator can alter the behavior of an upstream operator. Understanding this mechanism is critical to correctly predicting the behavior of complex resilience chains.

The system has three components:

1. **The `HandlersContext`:** An `AsyncLocal` variable that serves as a side-channel for communication. It holds a list of error-handling functions (`Func<Exception, FaultAction?>`).

2. **Handler Registrar Operators:** Operators like `RethrowOnFault` and `CompleteOnFault` do not process the stream's items in the traditional sense. Their primary role is to register a specific handling instruction (e.g., `_ => FaultAction.Rethrow`) into the `HandlersContext` during the subscription phase.

3. **Handler-Aware Resilience Operators:** The core resilience operators, such as `ContinueOnFault` and `ChainFaultContext`, are "handler-aware." Before executing their default logic (e.g., suppression or propagation), their internal `Catch` blocks first inspect the `HandlersContext`.

**Execution** Flow Example: **`source.ContinueOnFault().RethrowOnFault()`**

A standard Rx interpretation would suggest this stream should never fail, as `ContinueOnFault` would suppress the error. However, the Handler Precedence System alters this flow:

1. **Subscription Phase:**

   * `RethrowOnFault` subscribes and registers a `_ => FaultAction.Rethrow` function into the `HandlersContext`.

   * `ContinueOnFault` subscribes.

2. **Error Propagation:**

   * An error occurs in `source` and propagates to `ContinueOnFault`.

   * The `Catch` block inside `ContinueOnFault` executes. Before applying its default suppression logic, it inspects the `HandlersContext`.

   * It finds the `Rethrow` handler registered by the downstream `RethrowOnFault` operator.

   * It executes this handler's instruction, bypassing its own suppression logic and re-throwing the exception.

This non-linear flow, mediated by the `AsyncLocal` context, is how a downstream declarative instruction can override the behavior of an upstream operator, fulfilling the "Explicit Handlers Always Win" principle and enabling seamless integration with services that require exceptions for failure signaling.

#### **7. Creating Custom Item Resilience Operators**

While the framework provides a suite of common `...ItemResilient` operators, you may occasionally need to create your own. To ensure performance and consistency, you should follow the **"Per-Item Resilience"** pattern. This pattern, used by the framework's built-in operators like `SelectManyItemResilient`, applies the full resilience logic—including `PushStackFrame` and `Catch`—to the inner stream that is created for each individual item. This is typically done within a `SelectMany` projection. This approach is superior because it allows the context of the specific item that caused the failure to be captured in the error report.

**Example: The `SelectManyItemResilient` Pattern**

```csharp
public static IObservable<TResult> SelectManyItemResilient<TSource, TResult>(this IObservable<TSource> source,
    Func<TSource, IObservable<TResult>> resilientSelector, Func<IObservable<TResult>, IObservable<TResult>> retryStrategy,
    object[] context = null, [CallerMemberName] string memberName = "", /*...caller info...*/)
{
    return source
        .SelectMany(sourceItem =>
            // The resilience logic is applied to the inner stream for each item.
            resilientSelector(sourceItem)
                .ApplyItemResilience(retryStrategy, context.AddToContext(sourceItem), memberName, filePath, lineNumber)
        )
        // This final step guarantees that errors occurring during subscription or disposal
        // (which are invisible to standard .Catch operators) are also captured and reported.
        .SafeguardSubscription((ex, s) => ex.ExceptionToPublish(context.NewFaultContext(FaultHub.LogicalStackContext.Value, s)).Publish(), memberName);
}

```

By following this pattern, you can extend the framework with new, performant, and correctly behaved Item Resilience operators that provide rich, item-specific context in your error reports.

#### **8. Advanced Scenarios and High-Risk Integrations**

##### **8.1. High-Risk Scenarios**

Applying these fault-handling rules has consequences that you must be aware of in certain contexts:

* ⚠️ **Crash to Continue:** Using item-resilience patterns will convert stream-terminating crashes into graceful completions for that item.

* ⚠️ **Supervised Services:** Services designed to be restarted on failure (like some background job processors) may enter a "zombie" state if their errors are handled with item resilience instead of being allowed to propagate.

* ⚠️ **API Responses:** Failed API requests may result in a `200 OK` instead of a `500` error if the top-level stream uses item resilience.

* ⚠️ **Non-Idempotent Retries:** Applying a retry strategy to a non-idempotent operation (e.g., creating a user) risks creating duplicate data.

##### **8.2. Guideline for Integration with Error-Sensitive Services (e.g., Hangfire)**

Some execution contexts, like the Hangfire job scheduler, require a stream to terminate with an actual exception to correctly mark a process as failed. If your stream uses an item-resilience pattern (e.g., `ContinueOnFault` or a `...ItemResilient` operator), the error would normally be suppressed. To override this suppression, compose the stream with the `RethrowOnFault` operator. This registers a high-priority handler that instructs the system to rethrow the exception, ensuring the stream terminates with an error as required.

```csharp
// In a Hangfire job, this stream will be awaited and is expected to throw.
await Should.ThrowAsync<FaultHubException>(async () =>
    await application.UseObjectSpaceProvider(provider => Observable.Throw<Unit>(new Exception("This would be suppressed.")))
        .ContinueOnFault(["My Hangfire Job"]) // Suppresses the error by default.
        .RethrowOnFault()                     // Overrides the suppression.
        .PublishFaults()                      // This is now bypassed.
);

```

#### **9. Reading the Report: A Practical Example**

The `FaultHubException` report is designed to be read like a story, telling you both *what* the application was doing at a high level and *how* it was doing it at a low level. This section will deconstruct a sample report generated from an asynchronous operation and then show the exact code that produced it.

##### The Report

First, let's look at a formatted report. This is the final output you would see in your logs or diagnostics system.

```csharp
Process Order completed with errors (Boundary) <Async Failure>
--- Invocation Stack ---
  at ValidateCustomerAsync in ...\Services\OrderService.cs:line 45
  at GetOrderDetails in ...\Services\OrderService.cs:line 38
  at ProcessOrder in ...\Services\OrderService.cs:line 31
--- Original Exception Details ---
  System.InvalidOperationException: Async Failure


```

##### Hiding Framework Internals in the Report

To improve readability and focus on application-specific logic, the report renderer, by default, hides stack frames that originate from the eXpandFramework's internal methods. This ensures that the `Invocation Stack` primarily reflects the developer's own code, telling a clear business-level story. This behavior is configurable for advanced debugging scenarios where viewing the full, unfiltered stack is necessary.

##### Simplifying the Tree: Automatic Node Collapsing

To further improve the readability of the report, the underlying `OperationTree` parser includes a mechanism to collapse redundant nodes. This process occurs when a parent operation's name is a fully qualified version of a direct child's name (e.g., a parent node named `scraperService.ExtractAndProcessLinks` and its child named `ExtractAndProcessLinks`).

When this pattern is detected, the two nodes are automatically merged into a single node under the parent's name. This new node inherits the combined children, context, and logical stack of both. The result is a more concise report that removes unnecessary structural duplication and presents a clearer, more direct logical flow of the operation.

##### Deconstructing the Report

This report provides a complete diagnostic picture in a few distinct parts:

* **Header (`Process Order completed...`):** This line represents the outermost resilience boundary, which was established by a `.ChainFaultContext()` call. "Process Order" is the name of the operation (inferred from the method name), and "(Boundary)" is the specific, user-provided context. The message `<Async Failure>` is taken directly from the original, root-cause exception.

* **Invocation Stack:** This is the logical "story" of the operation. It should be read from the bottom up, just like a traditional call stack: `ProcessOrder` called `GetOrderDetails`, which in turn called `ValidateCustomerAsync`, where the error occurred. This demonstrates that the entire logical call chain was preserved by `PushStackFrame` calls, even across an asynchronous boundary (`Observable.Timer`), which is a key feature of the framework.

* **Original Exception Details:** This section provides the raw .NET exception and its physical stack trace for deep, platform-specific debugging.

##### The Code That Built the Report

The report above was generated by the following simple, yet powerful, pattern of nested method calls.

```csharp
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

// The subscription that executes the operation.
_ = ProcessOrder().PublishFaults().Subscribe();


```

##### Connecting Code to the Report

The report is a direct reflection of the code's structure and the resilience operators used:

* Each call to `.PushStackFrame()` contributed one line to the `--- Invocation Stack ---`, building the story from the inside out.

* The `.ChainFaultContext(["Boundary"])` call in `ProcessOrder` defined the report's header, captured the complete story assembled by the `PushStackFrame` calls, and created the final `FaultHubException` that was published to the bus.

This pattern is the primary method for creating detailed, contextual error reports for any complex operation, fulfilling the core goal of making asynchronous Rx errors fully debuggable by telling a clear story.

The report rendering process is designed for maximum reliability. In the rare event that the renderer itself encounters an error while analyzing a complex exception, it will not crash the application. Instead, it will output a fallback message containing the internal rendering error and the original, unformatted exception details, ensuring that diagnostic information is never lost.

#### **10. Understanding the Contract Through Tests**

The unit tests serve as live, executable documentation and the authoritative source for the framework's intended behavior. They validate the core resilience and diagnostic features that underpin the entire reactive ecosystem. This section highlights key non-transactional tests from the `Xpand.Extensions.Tests.FaultHubTests` suite to demonstrate these principles in practice.

##### **`FaultHub.General.cs`: Core Behaviors**

These tests confirm the fundamental reliability of the diagnostic context system. They prove that context is correctly managed across asynchronous operations and concurrent streams, which is essential for debugging complex, multi-threaded applications.

* **Test:** Context is preserved across scheduler threads.
  This test validates that when an operation is subscribed on one thread and the error occurs on another (e.g., a `TaskPoolScheduler` thread), the diagnostic context established on the initial thread is correctly captured and attached to the final `FaultHubException`. This is a critical guarantee for any asynchronous Rx pipeline.

  ```csharp
  [Test]
  public async Task FaultHub_Context_Flows_Across_Schedulers() {
      var asyncStream = Observable.Throw<Unit>(new InvalidOperationException("Async Error"))
          .SubscribeOn(TaskPoolScheduler.Default);
  
      var streamWithContext = asyncStream.ChainFaultContext(["MainThreadContext"]);
  
      await streamWithContext.PublishFaults().Capture();
  
      BusEvents.Count.ShouldBe(1);
  
      var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
      fault.Context.UserContext.ShouldContain("MainThreadContext");
  }
  
  
  ```

##### **`ChainFaultContextTests.cs`: Building the Diagnostic Story**

This test suite is the specification for the logical stack trace mechanism. The tests prove how `PushStackFrame` and `ChainFaultContext` collaborate to build a complete "story" of an operation, effectively replacing the physical stack trace that is lost in scheduled Rx operations.

* **Test:** A complete logical stack is built from nested helper methods.
  This test demonstrates the primary story-building pattern. Each nested method call adds its own frame to the story with `.PushStackFrame()`. The top-level call establishes a resilience boundary with `.ChainFaultContext()`, which catches the error and captures the full, ordered narrative of the operation from the innermost call to the outermost.

  ```csharp
  [MethodImpl(MethodImplOptions.NoInlining)]
  private IObservable<int> Level3_DetailWork() => Observable
      .Throw<int>(new InvalidOperationException("Failure in DetailWork"))
      .PushStackFrame(["Saving database record"]);
  
  [MethodImpl(MethodImplOptions.NoInlining)]
  private IObservable<int> Level2_BusinessLogic() => Level3_DetailWork()
      .PushStackFrame(); 
  
  [Test]
  public async Task ChainFaultContext_Should_Capture_The_Upstream_Logical_Story_Within_Its_Boundary() {
      var stream = Level2_BusinessLogic()
          .ChainFaultContext(
              source => source.Retry(2),
              ["Level1_TransactionBoundary"]
          );
  
      await stream.PublishFaults().Capture();
  
      var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
      var logicalStack = fault.LogicalStackTrace.ToList();
  
      logicalStack.ShouldContain(f => f.MemberName == nameof(Level2_BusinessLogic));
      logicalStack.ShouldContain(f => f.MemberName == nameof(Level3_DetailWork));
  }
  
  
  ```

##### **`ItemResilientOperatorsTests.cs`: Item Resilience in Practice**

These tests define the suppression pattern, where a failure in a single unit of work does not terminate the parent stream.

* **Test:** Processing continues even when a single item fails in a `SelectMany`.
  This test validates the core use case for item resilience. `SelectManyItemResilient` processes a sequence of items. When one item's inner stream throws an error, the operator catches it, publishes a contextual `FaultHubException`, and completes that inner stream gracefully. The outer stream remains active and proceeds to the next item, ensuring the entire sequence is processed.

  ```csharp
  [Test]
  public async Task SelectManyItemResilient_Processes_All_Items_Despite_Inner_Failure() {
      var source = Observable.Range(1, 3);
  
      var result = await source.SelectManyItemResilient(i => {
          if (i == 2) {
              return Observable.Throw<int>(new InvalidOperationException("Failure on item 2"));
          }
          return Observable.Return(i * 10);
      }).Capture();
  
      result.Items.ShouldBe([10, 30]); // Item 2 failed, but 1 and 3 succeeded.
      result.IsCompleted.ShouldBe(true);
  
      BusEvents.Count.ShouldBe(1); // The single failure was published.
      var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
      fault.AllContexts.ShouldContain(2); // The fault context includes the item that failed.
  }
  
  
  ```

##### **`IntegrationsProcessEvent.cs`: Resilience at the Source**

These tests validate that framework helpers for event handling provide resilience by default, creating "unbreakable" subscriptions.

* **Test:** Event subscriptions survive errors in their handlers.
  This test proves that `ProcessEvent` provides built-in item resilience for the logic within its selector. When an event handler throws an exception, the error is published to the `FaultHub`, but the underlying event subscription remains active. This is critical for UI responsiveness, as it prevents a single error from "killing" an action or event listener.

  ```csharp
  [Test]
  public void ProceedEvent_Survives_Error_And_Continues() {
      var eventSource = new TestEventSource();
      var eventCounter = 0;
      var hasThrown = false;
  
      using var _ = eventSource.ProcessEvent<EventArgs,Unit>(nameof(TestEventSource.MyEvent), e => e.Observe().Do(_ => {
              eventCounter++;
              if (!hasThrown) {
                  hasThrown = true;
                  throw new InvalidOperationException("Handler failed");
              }
              })
              .ToUnit())
          .PublishFaults().Subscribe();
  
      eventSource.RaiseEvent(); // This one will throw and publish a fault.
      eventSource.RaiseEvent(); // This one will still execute because the subscription survived.
  
      BusEvents.Count.ShouldBe(1);
      eventCounter.ShouldBe(2);
  }
  ```

#### **11. Monitoring and Automation: Querying the FaultHub Bus**

While the primary `FaultHub` operators focus on handling errors *within* a reactive stream, the framework also provides a powerful API for processing faults *after* they have been published to the central `FaultHub.Bus`. This enables advanced, system-wide monitoring, alerting, and automated recovery scenarios that are decoupled from the original business logic.

The querying API is built upon the `OperationTree`, a structured representation of the failed operation that is generated from the `FaultHubException`. This allows for precise, predicate-based matching against specific failure conditions.

##### **11.1. Targeting Faults with Transactional Tags**

To enable precise filtering, the Transactional API automatically enriches every `FaultHubException` it creates with a set of descriptive tags in the `AmbientFaultContext`. These tags, which describe the role and behavior of the operation that failed, are the primary mechanism for writing robust predicates.

Key tags include:

* **`Transaction`**: The fault originated from a workflow created with `BeginWorkflow`.
* **`Step`**: The fault originated from a specific operation within a transaction (e.g., in a `.Then()` clause or as one of the initial observables in a batch).
* **`RunToEnd`**, **`RunFailFast`**, **`RunAndCollect`**: Indicates the terminal operator used for the transaction, signifying its completion strategy.
* **`Sequential`**, **`Concurrent`**: Indicates the execution mode of the transaction's steps.
* **`Nested`**: Indicates that the transaction was executed as a step inside another parent transaction.

By inspecting these tags in an `AlertRule` or `RecoveryRule` predicate, you can create automation that responds not just to *which* operation failed, but to *how* it was configured to run.

##### **11.1.1. Displayable vs. System Tags: The Underscore Convention**

To keep the final error report clean and focused on human-readable information, the framework distinguishes between two types of tags:

*   **Displayable Tags:** Standard string labels (e.g., `Transaction`, `Step`) that are intended to be seen by developers in the rendered report.
*   **System Tags:** Programmatic markers prefixed with an underscore (`_`), such as `_NonCriticalStepTag`. These tags are essential for the framework's internal logic but are considered noise in a human-readable report.

The rendering engine will automatically filter out and hide any tag that begins with an underscore. This allows you to query for both types of tags programmatically (e.g., `node.Tags.Contains("Step") && node.Tags.Contains("_NonCriticalStepTag")`) while ensuring the final report remains uncluttered.

##### 11.2. Creating Alerts with `ToAlert`

The `ToAlert` operator transforms the raw stream of `FaultHubException` events from the bus into a stream of structured `Alert` objects. This is achieved by defining one or more `AlertRule` records.

An `AlertRule` consists of:

* **`Name`**: A descriptive name for the rule.

* **`Severity`**: An `AlertSeverity` level (e.g., `Warning`, `Error`, `Critical`).

* **`Predicate`**: A function (`Func<OperationNode, bool>`) that inspects each node in the `OperationTree`. The rule matches if the predicate returns `true`.

* **`MessageSelector`**: An optional function to generate a custom, user-friendly alert message from the matched node.

**Example: Triggering an Alert for a Specific Primitive Operation**

This example demonstrates setting up a listener that creates a critical alert whenever a specific, non-transactional operation fails.

```csharp
// 1. Define the operation that will fail.
private IObservable<Unit> FirePrimitiveOperationError()
    => Observable.Throw<Unit>(new Exception("Primitive Failure"))
        .ChainFaultContext(["PrimitiveOperation"]);

// 2. Define the Alert Rule.
var alertRule = new AlertRule(
    Name: "Primitive Operation Failure",
    Severity: AlertSeverity.Error,
    // The predicate targets the operation by its BoundaryName.
    Predicate: node => node.Name == nameof(FirePrimitiveOperationError)
);

// 3. Subscribe to the FaultHub.Bus and apply the rule.
var alertStream = FaultHub.Bus.ToAlert(alertRule);

alertStream.Subscribe(alert => {
    // Send to PagerDuty, Slack, etc.
    Console.WriteLine($"ALERT [{alert.Severity}]: {alert.Message}");
});

// 4. Trigger the failure.
_ = FirePrimitiveOperationError().PublishFaults().Subscribe();


```

##### 11.3. Automated Recovery with `TriggerRecovery`

The `TriggerRecovery` operator allows you to define self-healing logic for specific, known failure modes. It uses `RecoveryRule` records to match faults and execute compensatory actions.

A `RecoveryRule` consists of:

* **`Name`**: A name for the recovery strategy.

* **`Predicate`**: A function (`Func<OperationNode, bool>`) to identify the specific fault to handle.

* **`RecoveryAction`**: A function that receives the full `FaultHubException` and the matched `OperationNode` and returns an `IObservable<Unit>` representing the compensation logic.

**Example: Recovering from an Error Suppressed by `ContinueOnFault`**

```csharp
// 1. Define a recoverable operation using an item resilience operator.
private IObservable<Unit> FireRecoverablePrimitiveError()
    => Observable.Throw<Unit>(new Exception("Suppressed Failure"))
        .ContinueOnFault(context: ["RecoverablePrimitiveOp"]);

// 2. Define the Recovery Rule.
var recoveryActionExecuted = false;
var recoveryRule = new RecoveryRule(
    Name: "Suppressed Error Recovery",
    Predicate: node => node.Name == nameof(FireRecoverablePrimitiveError),
    RecoveryAction: (_, _) => {
        // Execute compensation logic, e.g., clear a cache, retry with a different service.
        recoveryActionExecuted = true;
        return Observable.Return(Unit.Default);
    }
);

// 3. Subscribe to the bus to trigger recovery.
FaultHub.Bus.TriggerRecovery([recoveryRule]).Subscribe();

// 4. Trigger the operation. The error will be published by ContinueOnFault,
// which will be intercepted by TriggerRecovery.
_ = FireRecoverablePrimitiveError().Subscribe();

// `recoveryActionExecuted` will now be true.

```

##### 11.4. Generating Telemetry with `ToFailureMetrics`

For integration with monitoring and BI dashboards, the `ToFailureMetrics` operator converts the rich `FaultHubException` into a flattened `FailureMetric` record. This is ideal for logging to time-series databases or analytics platforms.

The `FailureMetric` record includes:

* `TransactionName` / `StepName`: Inferred from the operation tree. For primitive operations, these often resolve to the method name.

* `RootCauseType`: The full name of the original .NET exception type.

* `Tags`: Any tags from the fault context.

* `Timestamp`: The UTC time of the failure.

**Example: Converting a Primitive Fault to a Metric**

```csharp
// 1. Define the operation that will fail.
private IObservable<Unit> FireMetricsPrimitiveError()
    => Observable.Throw<Unit>(new InvalidOperationException("Primitive Metric Failure"))
        .ChainFaultContext(["MetricsPrimitiveOp"]);

// 2. Subscribe to the bus to generate metrics.
var metricsStream = FaultHub.Bus.ToFailureMetrics();

metricsStream.Subscribe(metric => {
    // Log to Application Insights, Prometheus, etc.
    Console.WriteLine($"METRIC: Tx='{metric.TransactionName}', Step='{metric.StepName}', Cause='{metric.RootCauseType}'");
});

// 3. Trigger the failure.
_ = FireMetricsPrimitiveError().PublishFaults().Subscribe();

```

##### **11.5. Advanced Querying with Metadata Tokens**

For advanced monitoring and correlation scenarios, the framework provides a mechanism to attach arbitrary, machine-readable key-value data to a fault's context using the `IMetadataToken` interface.

A critical feature of this system is that any object implementing `IMetadataToken` is **explicitly excluded** from all human-readable diagnostic reports. This ensures that correlation IDs, telemetry tags, and other programmatic data do not clutter the logical stack trace or operation tree, which are intended for developer analysis.

Metadata is added to a resilience boundary via the `.ToMetadataToken()` extension method, which is then passed into the `context` array of operators like `ChainFaultContext` or `ContinueOnFault`. It can be retrieved from a `FaultHubException` using the `.GetMetadata<T>()` extension. This enables precise, programmatic filtering of the global `FaultHub.Bus`.

**Example: Correlating a Fault to a Specific Operation ID**

```csharp
[Test]
public async Task Can_Correlate_Fault_Via_MetadataToken() {
    var correlationId = Guid.NewGuid();

    // 1. Add the correlationId as a metadata token to the ChainFaultContext.
    var operation = Observable.Throw<Unit>(new InvalidOperationException("Operation failure"))
        .ChainFaultContext(
            context: [correlationId.ToMetadataToken(nameof(correlationId))]
        );

    // 2. Listen to the global bus, but filter for exceptions that have
    //    the correct metadata token.
    var listener = FaultHub.Bus
        .Where(fault => fault.GetMetadata<Guid>(nameof(correlationId)) == correlationId)
        .Take(1);

    // 3. Execute the operation and the listener concurrently.
    await operation.PublishFaults()
        .Merge(listener.ToUnit())
        .Capture();
        
    // The listener will only complete if it observes a fault with the matching ID.
    BusEvents.Count.ShouldBe(1);
}
```