![](https://img.shields.io/nuget/v/Xpand.XAF.Modules.Workflow.svg?&style=flat) ![](https://img.shields.io/nuget/dt/Xpand.XAF.Modules.Workflow.svg?&style=flat)

[![GitHub issues](https://img.shields.io/github/issues/eXpandFramework/expand/Workflow.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AWorkflow) [![GitHub close issues](https://img.shields.io/github/issues-closed/eXpandFramework/eXpand/Workflow.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AWorkflow)
# About

The `Workflow` module provides a reactive, controller-less engine for orchestrating complex, long-running, and scheduled business processes.

## Details
This is a `windows` module that allows you to design and execute workflows by chaining together `WorkflowCommand` objects within a `CommandSuite`. A `CommandSuite` acts as a container for a workflow and can be activated or deactivated as a whole.

The module provides several types of commands out of the box, which can be linked together to create powerful automations:
*   **`TimeIntervalWorkflowCommand`**: Triggers execution based on time. It supports recurring intervals from startup, persistent schedules that resume after restarts, random initial delays, and duration-based triggers from a comma-separated list of hours.
*   **`ObjectExistWorkflowCommand`**: Triggers based on data queries, reacting to new, updated, or existing objects that match a specific criteria.
*   **`MessageWorkflowCommand`**: Displays on-screen notifications or sends messages via Telegram.
*   **`ActionOperationWorkflowCommand`**: Listens for and reacts to the execution of standard XAF Actions.

Commands are chained by setting the `StartAction` or `StartCommands` properties, allowing you to define the order and logic of your workflow. The output of one command is passed as the input to the next, enabling data to flow through the process.

<twitter>

The following example shows a simple workflow defined in code. This workflow will display a "Hello World" message every 10 seconds.

```csharp


    
    var suite = ObjectSpace.CreateObject<CommandSuite>();
    suite.Name = "My First Workflow";

    var ticker = ObjectSpace.CreateObject<TimeIntervalWorkflowCommand>();
    ticker.Interval = TimeSpan.FromSeconds(10);
    ticker.CommandSuite = suite;

    var messenger = ObjectSpace.CreateObject<MessageWorkflowCommand>();
    messenger.Message = "Hello World";
    messenger.StartAction = ticker; // Chain the message to the timer
    messenger.CommandSuite = suite;

    ObjectSpace.CommitChanges();
    

```

</twitter>

The `Workflow` module's behavior is guaranteed by a comprehensive suite of automated tests. The following sections detail the module's functionality.

#### Core Execution & Dependency Logic
*   **Root Command Execution:** Simple, active commands without dependencies in an active `CommandSuite` are automatically executed when the application starts.
*   **Dependency Chain Execution (`StartAction`):** In a two-command chain (A → B), the dependent command (`B`) executes only after its prerequisite (`A`) has successfully completed.
*   **Fan-In Dependency Execution (`StartCommands`):** A command with multiple prerequisites (A and B → C) executes once for each prerequisite that completes.
*   **Data Flow:** The object array returned by a prerequisite command's `Execute` method is passed as the input to the `Execute` method of its dependent command.

#### ActionOperationWorkflowCommand
*   **Action Execution Trigger:** The command executes when its configured target XAF Action is triggered.
*   **View Filtering:** The command can be configured to execute only if the action is triggered within a specific `View` (identified by its `Id`).
*   **Output Emission Modes:** The command's output is controlled by the `Emission` property:
    *   **`SelectedObjects`:** Outputs the objects that were selected in the View.
    *   **`ViewObjects`:** Outputs all objects present in the View's collection source.
    *   **`Action`:** Outputs the `ActionBase` instance that was executed.
*   **Output Projection (`OutputProperty`):**
    *   **Property Projection:** When `OutputProperty` is set, the command projects the specified property's value from the source objects.
    *   **Duplicate Removal:** The final output contains only unique values.
    *   **Resilience:** The command handles invalid property names or properties that return `null` gracefully by producing an empty output.

#### ObjectExistWorkflowCommand
*   **Search Modes:** The command's query behavior is controlled by the `SearchMode` property:
    *   **`Default`:** Emits objects that currently exist in the database and also listens for and emits newly committed objects that match the criteria.
    *   **`Existing`:** Emits only the objects that exist in the database at the time of execution, ignoring any subsequent commits.
    *   **`Commits`:** Ignores all pre-existing objects and only emits objects that are newly created or updated after the workflow starts.
*   **Filtering Logic:**
    *   **`Criteria`:** A static filter (in XAF criteria format) that is always applied to the query.
    *   **`InputFilterProperty`:** A dynamic filter that is applied only when the command receives input from a preceding command. It filters for objects where the specified property's value is contained in the input data.
    *   **Combined Filtering:** When both `Criteria` and `InputFilterProperty` (with input) are used, they are combined with a logical `AND`, requiring objects to satisfy both conditions.
*   **Output Projection (`OutputProperty`):**
    *   **Simple Projection:** When set to a single property name (e.g., `Oid`), the output is an array of that property's values.
    *   **Formatted String Projection:** When set to a composite string (e.g., `"ID: {Oid}, Status: {Status}"`), the output is an array of formatted strings.
*   **Paging and Sorting:**
    *   **`TopReturnObjects`:** Limits the query to return only the specified number of objects.
    *   **`SkipTopReturnObjects`:** Skips the specified number of objects from the beginning of the result set. This requires at least one `SortProperties` entry to ensure a deterministic order.
    *   **`SortProperties`:** Defines the sort order for the query, enabling stable paging.

#### ObjectModifiedWorkflowCommand
*   **Execution on Modification:** The command executes when a change to a specific, monitored property of a configured business object type is committed to the database.
*   **Filtering and Specificity:**
    *   **`Criteria`:** The command only triggers for objects that satisfy the specified criteria at the time of the commit.
    *   **`Member` Specificity:** The command only triggers if the change was made to the specific property it is configured to monitor. Changes to other properties on the same object are ignored.
    *   **`Object` Specificity:** The command only triggers for modifications to the specific business object type it is configured to monitor.
*   **Reactive Processing:**
    *   **Batching:** Rapid, sequential modifications to multiple objects are batched into a single execution, with the output containing all modified objects from that batch.
    *   **De-duplication:** If the same object is modified multiple times within the batching window, only its final, most recent state is included in the output.

#### MessageWorkflowCommand
*   **Purpose:** Displays on-screen notifications to the user, using the application's standard notification mechanism.
*   **Input Handling:** The command takes the array of objects from the previous step and formats them into a string for display.
*   **Configuration Properties:**
    *   **`MsgType`**: Controls the notification's appearance and icon (`Info`, `Success`, `Warning`, `Error`).
    *   **`Position`**: Determines where on the screen the notification appears (e.g., `Right`).
    *   **`DisplayFor`**: A `TimeSpan` value that sets how long the notification remains visible.
    *   **`VerboseNotification`**: If `true`, the notification message is automatically prefixed with contextual information, including the name of the `CommandSuite` and the `WorkflowCommand` that triggered it.

#### Command & Suite Lifecycle Management
*   **`ExecuteOnce` Behavior:** A command with `ExecuteOnce = true` executes exactly one time and is then automatically set to `Active = false`.
*   **Inactive Root Command:** A root command with `Active = false` is ignored by the scheduler.
*   **Inactive Suite:** If a `CommandSuite` has `Active = false`, none of the commands within it are executed.
*   **Inactive Chain Link:** An inactive command in the middle of a dependency chain (A → B(inactive) → C) acts as a "circuit breaker," allowing the final command (`C`) to be triggered directly by the first (`A`).
*   **Dynamic Suite Activation:** If an inactive `CommandSuite` is programmatically activated while the application is running, the scheduler detects this change and executes the suite's eligible root commands.
*   **Dynamic Suite Deactivation:** If a running `CommandSuite` is programmatically deactivated, the scheduler immediately terminates the execution of all commands within that suite.

#### Validation, Resilience & Error Handling
*   **Pre-Execution Validation:** The scheduler will not execute a command that requires a subscription (`NeedSubscription = true`) but lacks a `StartAction`, instead raising a `ValidationException`.
*   **Circular Dependency Validation:** An attempt to save a `CommandSuite` containing commands with a circular dependency (e.g., A → B → A) is blocked by a `ValidationException`.
*   **`DisableOnError` Behavior:** If a command with `DisableOnError = true` fails during execution, the error is published to the `FaultHub` and the command is automatically set to `Active = false`.
*   **Default Failure Behavior:** If a command without `DisableOnError` fails, the error is published, but the command remains `Active = true`.
*   **Suite-Level Resilience:** A catastrophic failure in one `CommandSuite` is isolated and does not prevent other independent `CommandSuite` objects from executing.
*   **Auditing Resilience:** If a command executes successfully but a failure occurs while committing its `CommandExecution` audit record, the error is published, but the primary workflow is not interrupted.

#### Auditing & Maintenance
*   **Execution Logging:** A `CommandExecution` audit record is created only for commands with `LogExecutions = true`.
*   **Execution Cleanup:** When a new execution causes the total number of audit records for a command to exceed 10, the oldest records are automatically deleted.
---

**Possible future improvements:**

1.  Any other need you may have.

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.

## Installation
1.  First you need the nuget package so issue this command to the `VS Nuget package console`

    `Install-Package Xpand.XAF.Modules.Workflow`.

    The above only references the dependencies and nexts steps are mandatory.

2.  [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
    or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.Workflow.WorkflowModule));
    ```
## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: net10.0-windows`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp.Win**|**Any**
 |**DevExpress.ExpressApp.Validation**|**Any**
 |**DevExpress.ExpressApp.CloneObject**|**Any**
 |**DevExpress.ExpressApp.Xpo**|**Any**
 |**DevExpress.Persistent.BaseImpl.Xpo**|**Any**
 |**DevExpress.ExpressApp.Validation.Win**|**Any**
|Xpand.Extensions.Reactive|4.251.8
 |Xpand.Extensions.XAF|4.251.8
 |Xpand.Extensions.XAF.Xpo|4.251.8
 |Xpand.Extensions|4.251.8
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|4.251.8
 |[Xpand.XAF.Modules.ViewItemValue](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.ViewItemValue)|4.251.8
 |[Xpand.XAF.Modules.CloneModelView](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.CloneModelView)|4.251.8
 |[Xpand.XAF.Modules.Windows](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Windows)|4.251.8
 |[Xpand.XAF.Modules.ModelViewInheritance](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.ModelViewInheritance)|4.251.8
 |Xpand.Extensions.Reactive.Relay|4.251.8
 |[Xpand.XAF.Modules.BulkObjectUpdate](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.BulkObjectUpdate)|4.251.8
 |[Xpand.XAF.Modules.HideToolBar](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.HideToolBar)|4.251.8
 |System.Reactive|6.0.1
 |[Fasterflect.Xpand](https://github.com/eXpandFramework/Fasterflect)|2.0.7
 |System.Text.Json|9.0.8
 |Enums.NET|4.0.0
 |Lib.Harmony|9.0.0
 |Microsoft.Extensions.DependencyInjection.Abstractions|9.0.8
 |Microsoft.CodeAnalysis|4.12.0
 |Microsoft.Extensions.Options|9.0.8
 |Microsoft.Extensions.Configuration.Abstractions|9.0.8
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.251.8
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.251.8

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.Workflow.WorkflowModule))
```

### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.Workflow.Workflow).
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)

