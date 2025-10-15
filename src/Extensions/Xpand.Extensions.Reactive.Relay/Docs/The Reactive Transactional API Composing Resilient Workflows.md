### **The Reactive Transactional API: Resilient Workflows Composition**


This document provides a formal reference for the eXpandFramework's transactional API, a fluent interface for composing complex, resilient, and observable workflows.

### 1. `BeginWorkflow`

**Purpose**: Initiates a new transactional workflow from a source observable or an enumerable of observables. This is the entry point for all transactions.

**Description**:
The `BeginWorkflow` operator establishes a resilience boundary and prepares a stream for a sequence of transactional steps. It has several overloads to handle different scenarios:
* **From a single `IObservable<T>`**: The source observable is buffered until it completes, and its results are collected into an array to be passed to the first `Then` step.
*   **From an `IEnumerable<IObservable<T>>`**: When starting a workflow from an `IEnumerable<IObservable<T>>`, `BeginWorkflow` offers two distinct modes of operation, determined by the specific overload used:

    **1. As a Batch Operation (using the `TransactionMode` parameter)**
    This overload treats the entire enumerable as a **single, multi-part logical step**. All observables in the collection are executed either sequentially or concurrently, based on the specified `TransactionMode`. The results from all observables are aggregated. This mode is typically used with `RunAndCollect` to gather all results or with `RunToEnd` to process a batch where individual failures are tolerable.

    ```csharp
    // All operations are part of a single step, executed concurrently.
    var operations = new[] { "A".Observe(), "B".Observe() };
    var transaction = operations
        .BeginWorkflow("Concurrent-Batch-Tx", mode: TransactionMode.Concurrent);
    ```

    **2. As a Sequential Workflow Shortcut (without the `TransactionMode` parameter)**
    This overload provides a powerful compositional shortcut for building a **linear, multi-step sequential transaction**. It is functionally equivalent to chaining multiple `.Then()` calls. The operator takes the first observable to begin the workflow, then chains each subsequent observable as a new `.Then()` step.

    **Crucial Data Flow Behavior:** This shortcut uses a version of `.Then()` where the results from the previous step are **ignored**. Each observable in the enumerable is executed in sequence, but it does not receive input from its predecessor. This pattern is ideal for orchestrating a sequence of independent operations where the order matters but the output of one is not the input for the next.

    ```csharp
    // This creates a transaction equivalent to:
    // step1.BeginWorkflow().Then(_ => step2).Then(_ => step3)
    var step1 = "Connect".Observe();
    var step2 = "Authenticate".Observe();
    var step3 = "FetchData".Observe();
    
    var transaction = new[] { step1, step2, step3 }
        .BeginWorkflow("Sequential-Shortcut-Tx");
    ```
* **Transaction Naming**: The transaction can be explicitly named by passing a `string`. If the name is omitted, it is automatically inferred from the name of the calling method. This name is used for context in error reports.
* **Initial Step Creation**: `BeginWorkflow` automatically treats its source observable(s) as the first step of the transaction. If the initial source fails, its context—inferred.

**Example**:

```csharp
// Initiating a transaction with an explicit name from a single source.
var transaction = "http://example.com".Observe()
    .BeginWorkflow("WebScraping-Tx");

// Initiating a transaction where the name ('MyMethodName') will be inferred.
private void MyMethodName() {
    var transaction = new [] { 1.Observe(), 2.Observe() }
        .BeginWorkflow();
}

// Initiating a concurrent transaction from multiple sources.
var operations = new[] { "A".Observe(), "B".Observe() };
var transaction = operations
    .BeginWorkflow("Concurrent-Tx", mode: TransactionMode.Concurrent);

```

### 2. `Then`

**Purpose**: Chains a new sequential operation to the transaction.

**Description**:
The `Then` operator is the primary method for building a sequential workflow. Each `Then` step receives the buffered results from the preceding step as an array. It also features a powerful overload for in-line error handling.

* **Standard Usage**: The selector function receives an array of results from the previous step and returns a new `IObservable` to be executed.

* **Fallback Selector**: An optional `fallbackSelector` can be provided. If the primary selector throws an exception, this fallback is invoked with the exception and the original input data, allowing the transaction to recover and continue. This feature is only active when using the `RunToEnd` terminal operator; in `RunFailFast` mode, the fallback is ignored, and the transaction aborts immediately.

*   **Non-Critical Predicate**: An optional `isNonCritical` predicate (`Func<Exception, bool>`) can be provided to classify specific exceptions from this step as tolerable. When used with `RunFailFast`, a non-critical failure will be collected without aborting the transaction, allowing subsequent steps to execute.

**Data Salvage Override**:
The `Then` operator accepts an optional `dataSalvageStrategy` parameter that allows you to **override** the global strategy set by `RunToEnd` for this specific step.

*   **`DataSalvageStrategy.Inherit` (Default)**: The step will use the global strategy defined on the `RunToEnd` operator.
*   **`DataSalvageStrategy.EmitPartialResults`**: This step will emit partial results on failure, regardless of the global setting.
*   **`DataSalvageStrategy.EmitEmpty`**: This step will emit an empty collection on failure, regardless of the global setting.

**Example**:

```csharp
// Standard chaining
var transaction = "Initial Data".Observe()
    .BeginWorkflow("Sequential-Tx")
    .Then(inputArray => Step2_GetHomePage(inputArray.Single()))
    .Then(homePageArray => Step3_ExtractUrls(homePageArray.Single()));

// Using a fallback to recover from a failure
var transactionWithFallback = 123.Observe()
    .BeginWorkflow("Fallback-Tx")
    .Then(
        FailingStep,
        fallbackSelector: (ex, originalInput) => {
            // Log the exception ex, then return a default value
            return "Fallback Data".Observe();
        }
    )
    // The global default for this transaction is to emit nothing on failure.
    .RunToEnd(dataSalvageStrategy: DataSalvageStrategy.EmitEmpty);

```

### 3. `RunFailFast`

**Purpose**: A terminal operator that executes the transaction and immediately aborts upon the first failure.

**Description**:
`RunFailFast` is used for workflows where every step must succeed for the entire operation to be valid (e.g., setup or validation logic). If any step in the chain produces an `OnError` notification, `RunFailFast` immediately stops processing any subsequent steps. It then wraps the original `FaultHubException` from the failing step in a `TransactionAbortedException` and propagates it to the subscriber.

A key feature of `RunFailFast` is its ability to emit salvaged data from the critically failing step *before* terminating the stream. This allows a subscriber to receive partial results for forensic logging or post-mortem recovery even when the transaction aborts. The behavior is controlled by the `dataSalvageStrategy` parameter, which defaults to `EmitEmpty` (the classic abort behavior).

**Example**:

```csharp
var transaction = Unit.Default.Observe()
    .BeginWorkflow("FailFastTransaction")
    .Then(_ => new InvalidOperationException("Failing Operation").Throw<string>())
    .Then(_ => "This step will not execute".Observe())
    .RunFailFast();

// Subscribing to this transaction will result in an OnError notification
// containing a TransactionAbortedException.

```
**Hybrid Behavior with `isNonCritical` Predicate**
This operator's behavior can be modified by providing an `isNonCritical` predicate, either at the transaction level (in the `RunFailFast` call) or at the step level (in a `.Then()` call). Instead of a hierarchical precedence, the two predicates are combined: a failure is treated as non-critical if **either** the step-level predicate **or** the transaction-level predicate returns `true`. This means a step's failure can be deemed non-critical by the global transaction rule, even if a more specific step-level predicate is defined and returns `false`.

*   **If a failure is deemed CRITICAL** (the `isNonCritical` predicate returns `false`):
    1.  The transaction **aborts** immediately.
    2.  The effective `DataSalvageStrategy` (step-level or the global strategy from the `RunFailFast` call) is evaluated.
    3.  Any salvaged data is emitted to the final subscriber via `OnNext`.
    4.  The terminal `OnError` notification with the `TransactionAbortedException` is emitted.
*   **If a failure is deemed NON-CRITICAL** (the `isNonCritical` predicate returns `true`):
    1.  The transaction **continues** to the next step.
    2.  The effective `DataSalvageStrategy` determines what data is passed as input to that next step.
    3.  If the transaction completes having only collected non-critical failures, the final result is a standard `FaultHubException` containing an `AggregateException`, identical to the report from a failed `RunToEnd` transaction.

This powerful combination allows for workflows that can tolerate certain failures by salvaging their data and continuing, while still aborting immediately on unexpected, critical errors, potentially salvaging data from that critical failure as well.

**Example of Salvaging Data from a Critical Failure:**

```csharp
var transaction = Observable.Return("start")
    .BeginWorkflow("Salvage-Tx")
    .Then(_ => Observable.Return("Partial Data")
        .Concat(Observable.Throw<string>(new InvalidOperationException("Critical Failure"))))
    .RunFailFast(dataSalvageStrategy: DataSalvageStrategy.EmitPartialResults);

var result = await transaction.Capture();

// The subscriber receives the salvaged data before the error.
result.Items.ShouldHaveSingleItem();
result.Items.Single().ShouldBe(new[] { "Partial Data" });

// The transaction still terminates with an abort exception.
result.Error.ShouldBeOfType<TransactionAbortedException>();
```

### 4. `RunToEnd`

**Purpose**: A terminal operator that executes all steps in the transaction, even if some fail, and aggregates all errors.

**Description**:
`RunToEnd` is designed for batch processing or workflows where the failure of one part should not prevent others from running. It guarantees that every step in the transaction is attempted.

*   If a step fails, its error is collected. The `dataSalvageStrategy` determines what data (either partial results or an empty collection) is passed to the next step.

*   After all steps have been executed, if any errors were collected, the transaction emits its final notifications. First, if the `DataSalvageStrategy` allows for it, a single `OnNext` notification is emitted containing an array of all salvaged results from all successful and partially successful steps. Immediately following this, the stream terminates with a single `OnError` notification. This notification contains a `FaultHubException` that wraps an `AggregateException` holding the individual exceptions from all failing steps.

*   If all steps succeed, it completes and returns an array of results from the *final* step only.

**Data Salvage Configuration**:
`RunToEnd` accepts an optional `dataSalvageStrategy` parameter that sets the **global default** for how failing steps within the transaction handle partial results.

*   **`DataSalvageStrategy.EmitPartialResults` (Default)**: If a step fails, any results it successfully emitted before the error are passed to the next step. All such salvaged data from across the entire transaction is collected and emitted in a final `OnNext` notification before the stream terminates with an error.
*   **`DataSalvageStrategy.EmitEmpty`**: If a step fails, it emits nothing. The next step receives an empty array, and no data is emitted in the final `OnNext` notification.

**Example**:

```csharp
var transaction = "http://example.com".Observe()
    .BeginWorkflow("RunToCompletion-Tx")
    .Then(_ => Observable.Return("Partial Data").Concat(new InvalidOperationException("Homepage lookup failed").Throw<string>()))
    .Then(partialDataArray => Step3ProcessPartials(partialDataArray)) // This step will run with "Partial Data"
    .Then(_ => new InvalidOperationException("URL processing failed").Throw<Unit>())
    .RunToEnd(dataSalvageStrategy: DataSalvageStrategy.EmitPartialResults);

var result = await transaction.Capture();

// The subscriber receives the salvaged data before the error.
result.Items.ShouldHaveSingleItem();
result.Items.Single().ShouldBe(new[] { "Partial Data" });

// The transaction then terminates with an aggregate exception.
result.Error.ShouldBeOfType<FaultHubException>();
result.Error.InnerException.ShouldBeOfType<AggregateException>()
    .InnerExceptions.Count.ShouldBe(2);
```

### 5. `ThenConcurrent`

**Purpose**: Chains a batch of concurrent operations to a sequential transaction.

**Description**:
This operator allows you to introduce a parallel processing stage within a larger sequential workflow. The selector function receives the results from the previous step and must return an `IEnumerable` of named observables (`IEnumerable<(string Name, IObservable<object> Source)>`).

* **`maxConcurrency`**: An optional parameter to limit how many observables run in parallel at any given time.

*   **`failFast`**: This parameter controls the error-handling strategy for the concurrent batch.
    *   **If `true`**: The first failure in any of the concurrent operations will immediately cancel all other running operations in the batch and propagate the error, causing a `RunFailFast` parent transaction to abort.
    *   **If `false`**: All operations will run to completion. If any failures occurred, they are aggregated into a single `FaultHubException`. The `ThenConcurrent` step will then first emit an `OnNext` notification containing an array of results from all the **successful** operations in the batch. Immediately after, it will terminate with an `OnError` notification containing the aggregated fault. This allows a `RunToEnd` parent transaction to pass the partial, successful results from the concurrent step to the next step in the chain, while still collecting the aggregated failure for the final report.

**Example**:

```csharp
var transaction = Unit.Default.Observe()
    .BeginWorkflow("Concurrent-Batch-Tx")
    .ThenConcurrent(
        _ => new[] {
            (Name: "Op 1", Source: Observable.Timer(150.Milliseconds()).Select(_ => (object)"Op 1")),
            (Name: "Op 2", Source: Observable.Timer(50.Milliseconds()).Select(_ => (object)"Op 2"))
        },
        maxConcurrency: 2
    )
    .RunToEnd();

// The result will contain ["Op 1", "Op 2"], and the operation
// will take approximately 150ms to complete, not 200ms.

```

**Example with `failFast`:**
When `failFast` is set to `true`, the first error from any concurrent operation will immediately cancel all other running operations in that batch and propagate the failure.

 ```csharp
 var transaction = Unit.Default.Observe()
     .BeginWorkflow("Concurrent-FailFast-Tx")
     .ThenConcurrent(
         _ => new[] {
             (Name: "Slow Operation", Source: Observable.Timer(200.illiseconds()).Select(_ => (object)"This will be cancelled")),
             (Name: "Fast Failure", Source: Observable.Timer(50.illiseconds()).SelectMany(_ => new InvalidOperationException("Failure").Throw<object>()))
         },
         failFast: true
     )
     .RunFailFast();
//The "Slow Operation" will be cancelled after \~50ms when "Fast failure" throws.
//The entire transaction will fail immediately.
 ```




### 6. `RunAndCollect`

**Purpose**: A terminal operator that executes all steps and aggregates the *results* from all successful operations for final processing.

**Description**:
Unlike `RunToEnd`, which only forwards results from the final step, `RunAndCollect` captures the results from *every* successful step in the transaction. In a successful workflow, it provides the complete, flattened collection of all results to a final `resultSelector` function. This is useful when the goal of the transaction is to gather data from multiple stages into a single collection.

In the event of a failure, `RunAndCollect` inherits the error-handling and data-salvage behavior of `RunToEnd`. It will continue to execute all subsequent steps and aggregate any errors. However, its interaction with the `resultSelector` is a crucial distinction. The underlying transaction will first emit an `OnNext` notification containing any salvaged data from all successful and partially successful steps. This `OnNext` emission **will cause the `resultSelector` to be invoked** with this partial, salvaged data. Immediately after the `resultSelector` is invoked and its returned observable is subscribed to, the stream terminates with the final `OnError` notification containing the aggregated `FaultHubException`.

**Important Behavioral Note and Best Practice:**

Because the `resultSelector` is invoked just before the stream fails, placing critical side-effects within it can lead to an inconsistent application state. The logic inside the selector may begin execution with partial data under the false assumption that the transaction was successful, only for the entire operation to be immediately terminated by the error.

Therefore, the `resultSelector` should be considered a **pure function for data transformation only**. Any side-effects or final processing based on the transaction's outcome should be handled by the final `Subscribe` block, where the `OnNext` (for salvaged data on failure, or the selector's result on success) and `OnError` (for the failure report) handlers can be managed separately and explicitly.

**Example**:

```csharp
var step1 = "A".Observe();
var step2 = Observable.Range(1, 2).Select(i => i.ToString());
var step3 = "B".Observe();
var operations = new IObservable<object>[] { step1, step2, step3 };

var transactionResult = await operations
    .BeginWorkflow("BatchRunAndCollect-Tx", TransactionMode.Sequential)
    .RunAndCollect(allItems => allItems.JoinComma().Observe())
    .Capture();

// The single emitted item will be the string "A,1,2,B".
```

### **Summary of Terminal Operators: Choosing a Strategy**

The transactional API provides three distinct terminal operators: `RunFailFast`, `RunToEnd`, and `RunAndCollect`. While they all conclude a transaction, their behavior upon encountering an error is fundamentally different. Choosing the correct operator is critical for implementing the desired resilience strategy for your workflow. The following table provides a side-by-side comparison to guide this decision.

| Behavior | `RunToEnd` / `RunAndCollect` | `RunFailFast` |
| :--- | :--- | :--- |
| **Execution on Error** | **Continues.** Executes all subsequent steps in the transaction. | **Aborts.** Immediately stops and does not execute any subsequent steps (unless the error is marked as non-critical). |
| **Failure Report** | **Aggregates.** The final `OnError` contains an `AggregateException` with the failures from **all** failing steps. | **Singular.** The final `OnError` contains a `TransactionAbortedException` wrapping the failure from **only the first critical step** that failed. |
| **Primary Use Case** | **Batch Processing.** Ideal for workflows where the failure of one item should not prevent others from being processed (e.g., processing 100 URLs). | **Sequential Dependencies.** Ideal for workflows where each step is a prerequisite for the next (e.g., connect to database, authenticate user, fetch data). |


### **2. Advanced Behavior: Logical Stack Management**

Beyond just executing a series of operations, the transactional API actively manages the diagnostic `Logical Stack Trace` to build a rich, contextual story of the workflow's execution. This behavior differs depending on the operators used within the transaction's steps.

#### **2.1. Story Initiation with `BeginWorkflow`**

The diagnostic story starts with `BeginWorkflow`. The operator treats its source observable(s) as the first logical step. If an error occurs within this initial source, a stack frame is automatically pushed using the source's expression name (e.g., `GetUserOrders()`). This guarantees that even the earliest failures are captured with meaningful context.


#### **2.2. Stack Accumulation in `RunToEnd`**

By default, when using the `RunToEnd` terminal operator, the framework accumulates context from successfully completed steps. The logical stack frames from each successful step are preserved and prepended to the stack of any subsequent step that fails.

**Example:**
Consider a transaction with three steps:
1.  `StepA` succeeds and has an internal `PushStackFrame("Frame_A")`.
2.  `StepB` succeeds and has an internal `PushStackFrame("Frame_B")`.
3.  `StepC` fails.

The resulting error report for `StepC` will contain a logical stack trace that looks like: `StepC -> Frame_B -> Frame_A`. This provides a complete history of the successful operations that led up to the point of failure, which is invaluable for debugging complex sequences.

#### **2.3. Stack Truncation with `ChainFaultContext`**

If a step within a transaction defines its own resilience boundary by using the `.ChainFaultContext()` operator, it effectively "seals" the logical stack for that portion of the workflow.

This has two effects:
1.  The step's own internal logical stack is captured and processed by its `ChainFaultContext` boundary.
2.  The accumulated stack from *prior* steps in the parent transaction is discarded for any steps that execute *after* the boundary step.

This mechanism allows you to create modular, self-contained transactional components whose internal call stacks do not "leak" into the diagnostic reports of later, unrelated components within the same parent transaction. It provides a clean reset point for the diagnostic story.

-----

### 3\. Integrating External Logic and Inner Selectors with `AsStep()`

A common architectural pattern involves service methods that accept delegates (`Func` or `Action`) to allow for custom logic. While this is a flexible pattern, it presents a challenge for transactional systems: how can an operation defined deep inside a delegate be aware of, and participate in, a transaction that was started in an outer scope? The FaultHub Transactional API solves this problem with the `AsStep()` operator.

#### 3.1 The Core Challenge: Disconnected Context

Consider an external service that orchestrates a workflow by accepting custom logic via selectors:

```csharp
public class ExternalService {
    public IObservable<Unit> ExecuteWorkflow(
        Func<string, IObservable<string>> step1Selector,
        Func<string, IObservable<Unit>> step2Selector) {
        
        return "start".Observe()
            .SelectMany(step1Selector)
            .SelectMany(step2Selector);
    }
}
```

If we use this service within a transaction, the logic inside `step1Selector` and `step2Selector` has no intrinsic connection to the workflow started by `BeginWorkflow`.

```csharp
// How does the transaction know about a failure inside the selector?
var transaction = service.ExecuteWorkflow(
        input => Step1_Fails(input), // Fails internally
        result => Step2_Succeeds(result)
    )
    .BeginWorkflow("MyWorkflow")
    .RunFailFast();
```

Without a linking mechanism, a failure inside `Step1_Fails` would be a generic exception, lacking the rich transactional context needed for a precise error report.

#### 3.2 The Solution: The `AsStep()` Bridge

The `AsStep()` operator is a declarative bridge that links any observable sequence to the currently active ambient transaction. When `BeginWorkflow` is called, it establishes a `TransactionContext` in the current asynchronous context (`AsyncLocal`). The `AsStep()` operator taps into this ambient context.

**How it works:**

1.  `AsStep()` is applied to an observable *inside* the selector.
2.  It finds the ambient `TransactionContext` established by `BeginWorkflow`.
3.  It wraps the source observable. If an error occurs, `AsStep` intercepts it, enriches it into a `FaultHubException` that includes the step's name (inferred from the source code expression), and registers this fault with the ambient transaction.
4.  It then re-throws the enriched exception, allowing the parent transaction's terminal operator (`RunFailFast`, `RunToEnd`, etc.) to handle it according to its rules.

This ensures that any operation, no matter how deeply nested in a delegate, becomes a first-class citizen of the transaction, with full support for contextual error reporting.

#### 3.3. The Diagnostic Role of `AsStep`: A Reporter, Not a Recorder

While `AsStep` is a critical bridge for integrating logic into a transaction, it is essential to understand its specific diagnostic role: it is a **reporter**, not a full **recorder**.

When an observable wrapped by `AsStep` fails, the operator's primary responsibilities are to:
1.  Intercept the raw exception.
2.  Find the active ambient `TransactionContext`.
3.  Create a "thin" `FaultHubException` that contains the **name** of the failing step (inferred from the source code expression) and the appropriate tags.
4.  Propagate this enriched exception so the parent transaction can handle it.

Crucially, `AsStep` does **not** capture the ambient `LogicalStackContext` at the moment of failure. The `FaultHubException` it creates contains the step's identity but intentionally omits the detailed invocation story.

The full diagnostic snapshot is taken by the transaction's main execution logic—specifically, the `.Then()` or terminal (`Run...`) operator that contains the `AsStep` call. This higher-level operator catches the "thin" fault reported by `AsStep` and performs the final enrichment, capturing the complete logical stack trace at that point.

**Practical Implication:**
In a final error report, this means the detailed `Invocation Stack` will be associated with the broader transactional step boundary (e.g., the `.Then()` block), while the `AsStep` operator's contribution is providing the precise **name** of the sub-operation that failed within that step.

#### 3.4 Behavior in `RunFailFast` Mode

In a `RunFailFast` transaction, the first correlated failure will abort the entire workflow. `AsStep()` ensures that failures from within selectors are correctly correlated.

**Example:**

```csharp
// Service and step definitions
private IObservable<string> Step1_Fails(string input) =>
    new InvalidOperationException("Failure in Step 1").Throw<string>();

private IObservable<Unit> Step2_Succeeds(string input) => Unit.Default.Observe();

// Transaction composition
var service = new ExternalService();

var transaction = service.ExecuteWorkflow(
        input => Step1_Fails(input).AsStep(), // Correlate this step
        result => Step2_Succeeds(result).AsStep()
    )
    .BeginWorkflow("ServiceWorkflow")
    .RunFailFast();

// Execution and Assertion
await transaction.PublishFaults().Capture();
var finalReport = BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();
var stepFault = finalReport.InnerException.ShouldBeOfType<FaultHubException>();

// The report correctly identifies the failing step by name
stepFault.Context.BoundaryName.ShouldBe(nameof(Step1_Fails));
```

When `Step1_Fails` throws, `AsStep()` catches the exception, correlates it to the `ServiceWorkflow` transaction, and re-throws the enriched fault. The `RunFailFast` operator receives this fault and immediately terminates the transaction, producing a `TransactionAbortedException` that pinpoints `Step1_Fails` as the root cause.

#### 3.5 Behavior in Aggregating Modes (`RunToEnd` & `RunAndCollect`)

The `AsStep()` operator works seamlessly with error-aggregating modes. It registers the failure with the transaction, but allows the terminal operator to decide whether to continue.

**Example with `RunToEnd`:**

```csharp
var step2WasExecuted = false;

var transaction = "start".Observe()
    .BeginWorkflow("AggregatingWorkflow")
    .Then(_ => Step1_Fails("start").AsStep())
    .Then(results => { // This step will still run
        step2WasExecuted = true;
        return Step2_Succeeds(results.FirstOrDefault()).AsStep().Select(u => (object)u);
    })
    .RunToEnd();

await transaction.PublishFaults().Capture();

step2WasExecuted.ShouldBeTrue(); // Confirms the transaction continued
var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
var aggregate = finalFault.InnerException.ShouldBeOfType<AggregateException>();
var stepFault = aggregate.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();

// The aggregated report still correctly identifies the failing step
stepFault.Context.BoundaryName.ShouldBe(nameof(Step1_Fails));
```

In this case, when `Step1_Fails` throws, `AsStep()` reports the failure to the `AggregatingWorkflow` transaction. Because the transaction uses `RunToEnd`, it proceeds to execute the second `Then` block. At the conclusion, `RunToEnd` aggregates all reported failures, and the final `FaultHubException` contains a report that correctly attributes the failure to the `Step1_Fails` step. This same behavior applies to `RunAndCollect`.

#### 3.6 Ambient Context Management in Nested Workflows

The system robustly manages the ambient `TransactionContext`, even in complex nested scenarios. When a new workflow is started with `BeginWorkflow`, it pushes a new context onto the stack. When that workflow completes (either by success or failure), its context is popped, restoring the parent's context.

This guarantees that `AsStep()` always correlates a failure to the correct, most immediate workflow, preventing context leakage between sibling or nested transactions and ensuring diagnostic accuracy.

#### 3.7 Advanced Error Handling with `onFault`

To provide a unified and explicit model for error handling, the `AsStep` operator consolidates all resilience logic into a single, optional `onFault` selector. This selector is  making the developer's intent unambiguous and prevents invalid parameter combinations.

The `onFault` selector is a function that receives the exception and must return a `ResilienceAction` enum value, which dictates the step's behavior.

##### `ResilienceAction` Enum

*   **`Critical` (Default):** The failure is considered critical. The fault is wrapped in a `FaultHubException` and **propagated**. This will cause a `RunFailFast` transaction to abort immediately.
*   **`Tolerate`**: The failure is considered non-critical. The fault is wrapped, tagged as non-critical, and **propagated**. This allows a `RunFailFast` transaction to continue execution while ensuring the failure is included in the final report.

*   **`Suppress`**: The failure is a tolerable, item-level issue. The operator completes the local data stream (`Observable.Empty()`), allowing the transaction to proceed to the next step as if the failing step had succeeded but produced no data.

    **Crucial Distinction: Suppression is Local, Failure is Global**
    The term "Suppress" applies only to the immediate data stream of the step. The fault is **not discarded**. Instead, it is wrapped, tagged as non-critical, and **reported** to the transaction's internal failure collection **via a side-channel**.

    This means that even though subsequent steps will execute, the transaction has been marked as failed. When the transaction's terminal operator (`RunToEnd` or `RunFailFast`) executes, it will still produce an `OnError` notification containing an aggregate of all failures, including the suppressed ones.

    **When to Use `Suppress`:**
    Use this action when a step performs a non-essential side-effect and its output is not required by subsequent steps. You want to ensure the rest of the transaction runs, but you still need the final report to reflect that a partial failure occurred.

    **Example: A Suppressed Failure Still Fails the Transaction**
    ```csharp
    var step2WasExecuted = false;

    var transaction = Observable.Return("start")
        .BeginWorkflow()
        .Then(_ => Observable.Throw<string>(new InvalidOperationException("Suppressed Failure"))
            .AsStep(onFault: _ => ResilienceAction.Suppress))
        .Then(results => {
            // This step executes because the previous failure was suppressed locally.
            step2WasExecuted = true;
            results.ShouldBeEmpty(); // No data is passed from the suppressed step.
            return Observable.Return("Final Step");
        })
        .RunToEnd();

    var result = await transaction.Capture();

    step2WasExecuted.ShouldBeTrue();
    
    // The transaction still terminates with an error.
    result.Error.ShouldBeOfType<FaultHubException>();
    var aggregate = result.Error.InnerException.ShouldBeOfType<AggregateException>();
    aggregate.InnerExceptions.Single().InnerException.Message.ShouldBe("Suppressed Failure");
    ```
    
##### Example: Using `onFault` to Control Transaction Flow

```csharp
var transaction = "start".Observe()
    .BeginWorkflow()
    .Then(_ => Observable.Throw<string>(new TimeoutException("Tolerable network timeout"))
        // Tolerate this specific error, but let others be critical.
        .AsStep(onFault: ex => ex is TimeoutException ? ResilienceAction.Tolerate : ResilienceAction.Critical))
    .Then(results => { /* This step will execute if the error was a TimeoutException */ })
    .RunFailFast();
```

#### 3.8 Interaction with `DataSalvageStrategy`

A common question is how to control data salvage for logic wrapped by `AsStep`, especially in a `RunFailFast` transaction. The key architectural principle is a **separation of concerns**:

*   **`AsStep()` is the Reporter:** Its sole responsibility is to report a failure from the observable it wraps, ensuring the failure is correctly correlated with the ambient transaction.
*   **`.Then()` is the Policy Holder:** The parent `.Then()` operator that defines the step boundary is responsible for declaring the policy for how to handle a failure reported by its inner logic.

Therefore, the `DataSalvageStrategy` is **not** a parameter on `AsStep` itself. Instead, you configure it on the `.Then()` operator that contains the `AsStep` call.

When a critical failure occurs within logic wrapped by `AsStep`, the transaction machinery correctly identifies the `DataSalvageStrategy` from the parent `.Then()` step and uses it to determine what data, if any, should be emitted to the final subscriber before the transaction aborts.

**Example: Salvaging Data from a Critically Failing `AsStep`**

```csharp
// This is the inner logic that will be correlated.
// It emits one piece of data before throwing a critical error.
private IObservable<string> InnerLogic_EmitsPartial_Then_Fails_Critically() =>
    Observable.Return("Partial Data from AsStep")
        .Concat(Observable.Throw<string>(new InvalidOperationException("Critical Inner Logic Failure")));

// The transaction is configured to salvage data from the step.
var transaction = Observable.Return("start")
    .BeginWorkflow("AsStep-Salvage-Tx")
    .Then(
        // The .Then() operator defines the policy for the step.
        _ => InnerLogic_EmitsPartial_Then_Fails_Critically().AsStep(),
        dataSalvageStrategy: DataSalvageStrategy.EmitPartialResults
    )
    .RunFailFast();

var result = await transaction.Capture();

// The final subscriber receives the salvaged data...
result.Items.ShouldHaveSingleItem();
result.Items.Single().ShouldHaveSingleItem().ShouldBe("Partial Data from AsStep");

// ...before the transaction terminates with the critical error.
result.Error.ShouldBeOfType<TransactionAbortedException>();
```

This pattern ensures that the responsibility for defining failure policy remains cleanly at the transaction step level, while `AsStep` remains focused on its single responsibility of correlating failures.

#### 3.9 Defining Atomic Steps with the `IEnumerable` Overload

Beyond correlating a single observable, `AsStep` provides a powerful overload for `IEnumerable<IObservable<T>>` that allows you to group multiple sub-operations into a single, atomic transactional step. This is essential for scenarios where a logical step consists of several parallel or sequential tasks that must be treated as a single unit of work.

This overload enables a "deferred failure" pattern. It executes all observables in the collection, waits for them all to complete, and only then determines the outcome of the step.

**Key Behaviors:**

1.  **Atomic Execution**: All observables in the enumerable are executed. The failure of one does not prevent its siblings from running.
2.  **Deferred Failure Reporting**: The `AsStep` operator waits until all sub-operations have terminated.
3.  **Aggregate Reporting**: If one or more sub-operations failed, `AsStep` aggregates all of their exceptions into a single `AggregateException`. It then wraps this in a single `FaultHubException` that represents the failure of the entire step.
4.  **Success Aggregation**: If all sub-operations succeed, their emissions are collected and passed on as the successful result of the step.

This pattern is invaluable when you need to ensure that certain essential, parallel tasks (like logging or cleanup) are attempted even if the primary task within the same step fails.

**Naming the Atomic Step**
The `AsStep` overload for `IEnumerable` follows the same naming convention as `BeginWorkflow`. The name for the aggregated atomic step, which appears in the final error report, can be provided as an optional string parameter. If this parameter is omitted, the name is automatically inferred from the name of the calling method. This ensures that the aggregated step has a meaningful and predictable identity in your diagnostic reports.

**Example: Ensuring Sibling Operations Complete Before Failing**

Consider a service that performs several actions as part of a single logical step. One action is flaky, but another is essential and must always be attempted.

```csharp
public class TestableExternalService {
    public bool EssentialStepWasExecuted { get; private set; }

    // This method defines a single logical step composed of multiple sub-operations.
    public IObservable<Unit> WhenVisitPage(Uri uri) {
        // The sub-operations are returned as an enumerable of observables.
        var subOperations = new[] {
            MockParsePage().AsStep(),
            FlakyOperationThatCanFail().AsStep(), // This will fail.
            AnotherEssentialStep().AsStep()      // This must still run.
        };
        
        // AsStep() is applied to the entire collection, with an explicit name for the atomic step.
        return subOperations.AsStep("ExecutingAllSteps");
    }

    // A sub-operation that will fail.
    internal IObservable<Unit> FlakyOperationThatCanFail() 
        => Observable.Throw<Unit>(new InvalidOperationException("This is a deferred failure."));

    // An essential sub-operation that must be attempted.
    public IObservable<Unit> AnotherEssentialStep() {
        return Observable.FromAsync(() => {
            EssentialStepWasExecuted = true;
            return Task.CompletedTask;
        });
    }
    
    private IObservable<Unit> MockParsePage() => Observable.Return(Unit.Default);
}

// Transaction composition
var service = new TestableExternalService();
await service.WhenVisitPage(new Uri("http://example.com"))
    .BeginWorkflow()
    .RunFailFast()
    .PublishFaults()
    .Capture();

// Assertion
service.EssentialStepWasExecuted.ShouldBe(true); // Proves the sibling operation ran.

var finalException = BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();
// The name "ExecutingAllSteps" is used for the boundary of the aggregated fault.
var aggregateException = finalException.InnerException.ShouldBeOfType<FaultHubException>()
    .InnerException.ShouldBeOfType<AggregateException>();

// The final report contains the single failure from the flaky operation.
var deferredException = aggregateException.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();
deferredException.Context.Name.ShouldBe(nameof(TestableExternalService.FlakyOperationThatCanFail));
```

In this example, even though `FlakyOperationThatCanFail` throws an exception, the `AsStep` overload ensures that `AnotherEssentialStep` is also executed. Only after all three sub-operations are complete does `AsStep` fail the entire "ExecutingAllSteps" step, providing an aggregate report of what went wrong. This allows for robust, multi-part steps within a larger transaction.

##### Behavior Outside a Transaction

The `AsStep` operator is designed for use within a transactional context. If it is called when no transaction is active, it becomes inert for safety. It will catch any exception, but then **re-throw the original, unwrapped exception as-is**. The `onFault` selector is ignored in this scenario.

### **4. Advanced: Programmatic Correlation and Monitoring**

In complex systems with many concurrent workflows, it becomes essential to programmatically track and correlate specific transaction failures for monitoring, telemetry, or automated recovery. The Transactional API provides a streamlined mechanism for this via a dedicated `correlationId` parameter.

When you provide a `Guid` to the `correlationId` parameter of the `BeginWorkflow` operator, the framework automatically attaches it to the transaction's context as a special `IMetadataToken`. A key feature of this token is that it is **invisible** in all human-readable reports (like the console output or logged string representation) to avoid cluttering the diagnostic view. Its sole purpose is for programmatic querying.

To filter the global `FaultHub.Bus` for failures originating from a specific transaction, the framework provides the `TransactionFault(Guid transactionId)` extension method. This allows you to create precise listeners that react only to the failures you care about.

**Example: Correlating a Fault to a Specific Transaction ID**

```csharp
[Test]
public async Task Can_Correlate_Fault_To_Transaction_Via_MetadataToken() {
    var transactionId = Guid.NewGuid();

    // 1. Assign a correlation ID when starting the workflow.
    var workflow = Observable.Throw<Unit>(new InvalidOperationException("Workflow failure"))
        .BeginWorkflow("MyCorrelatedWorkflow", correlationId: transactionId);

    // 2. Create a listener on the global bus that filters for that specific ID.
    var listener = FaultHub.Bus
        .TransactionFault(transactionId)
        .Take(1);

    // 3. Execute the workflow, which will fail and publish to the bus.
    await workflow.RunFailFast().PublishFaults().Capture();

    // 4. Concurrently, await the listener, which will only complete
    //    if it observes the fault with the matching ID.
    await listener.Timeout(1.Seconds());
}
```