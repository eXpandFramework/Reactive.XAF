

using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Templates;
using DevExpress.ExpressApp.Validation;
using DevExpress.Persistent.Validation;
using DevExpress.Utils.Extensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ObjectExtensions;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Workflow.BusinessObjects;
using Xpand.XAF.Modules.Workflow.BusinessObjects.Commands;

namespace Xpand.XAF.Modules.Workflow.Services{
    public static class WorkflowService{
        static readonly ReplaySubject<SynchronizationContext> ContextSubject = new();
        internal static IObservable<Unit> WorkflowServiceConnect(this ApplicationModulesManager manager){
            return manager.WhenSetupComplete(application => application.MonitorCommandExecutions()
                    .MergeToUnit(application.DeferAction(() => application.Modules.OfType<ValidationModule>().First().InitializeRuleSet()))
                    .MergeToUnit(application.RefreshCommandSuiteDetailView())
                    .MergeToUnit(application.SynchronizeDashboardItem())
                    .MergeToUnit(application.WhenSynchronizationContext()
                        .Do(context => {
                            LogFast($"SynchronizationContext captured.");
                            ContextSubject.OnNext(context);
                        }))
                )
                .MergeToUnit(manager.ActivateCommand())
                .MergeToUnit(manager.DeActivateCommand())
                .MergeToUnit(manager.WhenSetupComplete(application => application.EnsureCommandsDeleted()
                    .MergeToUnit(application.Modules.OfType<ValidationModule>().ToNowObservable()
                        .SelectMany(module => module.ProcessEvent(nameof(module.RuleSetInitialized)).Take(1)
                            .SelectMany(_ => application.WhenExecuteCommands())))
                    .MergeToUnit(application.CleanupExecutions())
                ))
                .Finally(() => LogFast($"Exiting {nameof(WorkflowServiceConnect)}"))
                .ToUnit();
        }

        private static IObservable<Unit> CleanupExecutions(this XafApplication application) {
            var whenProviderObject = application.WhenProviderObject<CommandExecution>(ObjectModification.New);
            return whenProviderObject.BufferWithInactivity(5.ToSeconds(), maxBufferTime: 10.ToSeconds()).WhenNotEmpty()
                .SelectManyItemResilient(executions => application.UseProviderObjectSpace(space => {
                    var executionIds = executions.Select(execution => execution.Oid).ToArray();
                    executions = space.GetObjectsQuery<CommandExecution>()
                        .Where(execution => executionIds.Contains(execution.Oid)).ToArray()
                        .Where(execution => !execution.IsDeleted).ToArray();
                    space.Delete(executions.WhereDefault(execution => execution.WorkflowCommand).ToArray());
                    return executions.GroupBy(execution => execution.WorkflowCommand).ToNowObservable().WhenNotDefault(grouping => grouping.Key)
                        .Select(commandExecutionsGroup => {
                            var keyOid = commandExecutionsGroup.Key.Oid;
                            var count = space.Count<CommandExecution>(commandExecution => commandExecution.WorkflowCommand.Oid == keyOid);
                            if (count <= 10) return null;
                            var commandExecutions = commandExecutionsGroup.Key.Executions
                                .OrderByDescending(commandExecution => commandExecution.Oid).Take(10);
                            space.Delete(commandExecutionsGroup.Key.Executions.Except(commandExecutions).ToArray());
                            return commandExecutionsGroup.Key;
                        })
                        
                        .Commit();
                }))
                .ToUnit();
        }

        private static IObservable<Unit> EnsureCommandsDeleted(this XafApplication application){
            LogFast($"Entering {nameof(EnsureCommandsDeleted)}");
            return application.WhenFrame(frame => {
                    LogFast($"Frame for {nameof(WorkflowCommand)} ListView found. Monitoring for deletions.");
                    return frame.View.ObjectSpace
                        .WhenCommitingDetailed<WorkflowCommand>(ObjectModification.Deleted, false)
                        .TakeUntil(frame.WhenDisposedFrame()).ToObjectsGroup()
                        .SelectMany(commands => {
                            LogFast($"{commands.Length} commands are being deleted. Synchronizing StartAction references.");
                            var parentSuite = frame.ParentObject<CommandSuite>();
                            LogFast($"Parent CommandSuite not found. Skipping synchronization.");
                            return parentSuite?.Commands
                                .Where(command => commands.Contains(command.StartAction))
                                .Execute(command => {
                                    LogFast($"Nullifying StartAction for command '{command}' because its target was deleted.");
                                    command.StartAction = null;
                                }) ?? [];
                        });
                },typeof(WorkflowCommand), ViewType.ListView, Nesting.Nested)
                .ToUnit()
                .Finally(() => LogFast($"Exiting {nameof(EnsureCommandsDeleted)}"));
        }

        private static IObservable<IList<View>> SynchronizeDashboardItem(this XafApplication application){
            LogFast($"Entering {nameof(SynchronizeDashboardItem)}");
            return application.WhenFrame(SynchronizeDashboardViewItem ,typeof(CommandSuite), ViewType.DetailView) ;
        }

        private static IObservable<IList<View>> SynchronizeDashboardViewItem(Frame frame){
            LogFast($"Dashboard frame for {nameof(CommandSuite)} found. Setting up synchronization.");
            return frame.View.ToDetailView().NestedFrameContainers(typeof(WorkflowCommand))
                .Where(container => container.Frame.View is DetailView)
                .CombineLatest(frame.View.ToDetailView().NestedFrameContainers(typeof(WorkflowCommand))
                        .Where(container => container.Frame.View is ListView),
                    (detailView, listView) => (detailView, listView))
                .SelectMany(t => t.listView.Frame.View.WhenSelectedObjectsChanged()
                    .Where(view => view.SelectedObjects.Cast<object>().Count() == 1)
                    .BufferUntilInactive(1. Seconds()).ObserveOnContext().WhenNotEmpty()
                    .Do(_ => {
                        var selectedObject = t.listView.Frame.View.SelectedObjects.Cast<object>().First();
                        LogFast($"Selection changed in ListView. Synchronizing DetailView with selected object: {selectedObject}");
                        var objectInDetailViewSpace = t.detailView.Frame.View.ObjectSpace.GetObject(selectedObject);
                        t.detailView.Frame.View.CurrentObject = t.detailView.Frame.View.ObjectSpace.ReloadObject(objectInDetailViewSpace);
                        LogFast($"DetailView synchronized.");
                    }))
                .Finally(() => LogFast($"Exiting {nameof(SynchronizeDashboardItem)}"));
        }

        private static IObservable<Unit> MonitorCommandExecutions(this XafApplication application){
            LogFast($"Entering {nameof(MonitorCommandExecutions)}");
            return application.WhenProviderObjects<WorkflowCommand>(ObjectModification.New,command => command.Active).SelectMany()
                .SelectManyItemResilient(command => {
                    LogFast($"New command '{command.FullName}' detected. Monitoring for execution.");
                    return command.WhenExecuted()
                        .SelectManyItemResilient(objects => {
                            LogFast($"Command '{command.FullName}' executed with {objects.Length} objects. Notifying and logging.");
                            return application.NewCommandExecution(command.YieldArray());
                        });
                })
                .ToUnit()
                .ContinueOnFault(context:[nameof(MonitorCommandExecutions)])
                .Finally(() => LogFast($"Exiting {nameof(MonitorCommandExecutions)}"));
        }
        
        private static IObservable<Unit> RefreshCommandSuiteDetailView(this XafApplication application){
            LogFast($"Entering {nameof(RefreshCommandSuiteDetailView)}");
            return application.RefreshDetailViewWhenObjectCommitted<WorkflowCommand>(typeof(CommandSuite), (frame, objects)
                    => objects.Any(o => ((CommandSuite)frame.View?.CurrentObject)?.Commands.Any(command => command.Index == o.Index) ?? false))
                .MergeToUnit(application.RefreshDetailViewWhenObjectCommitted<CommandExecution>(typeof(CommandSuite),
                    (frame, objects) => objects.Any(o => o.WorkflowCommand != null && o.WorkflowCommand.CommandSuite.Oid ==
                        (Guid?)frame.View?.ObjectSpace.GetKeyValue(frame.View.CurrentObject))))
                .ContinueOnFault(context:[nameof(RefreshCommandSuiteDetailView)])
                .Finally(() => LogFast($"Exiting {nameof(RefreshCommandSuiteDetailView)}"));
        }

        private static IObservable<CommandExecution> NewCommandExecution(this XafApplication application, IList<WorkflowCommand> commands) 
            => commands.All(command => !command.ShouldLogExecutions()) ? Observable.Empty<CommandExecution>() : application.UseProviderObjectSpace(space => 
                        commands.ToNowObservable()
                        .Select(space.GetObject).Where(command => command.ShouldLogExecutions())
                        .SelectMany(o => {
                            var execution = o.ObjectSpace.CreateObject<CommandExecution>();
                            execution.WorkflowCommand = o;
                            execution.Created = DateTime.Now;
                            space.CommitChanges();
                            return execution.Observe();
                        }))
                    .ToConsole(execution => $"{execution.Created}, {execution.WorkflowCommand}");

        private static IObservable<Unit> DeActivateCommand(this ApplicationModulesManager manager){
            LogFast($"Entering {nameof(DeActivateCommand)}");
            return manager.RegisterViewSimpleAction(nameof(DeActivateCommand), action => {
                    action.TargetObjectType = typeof(IActiveWorkflowObject);
                    action.TargetViewType = ViewType.ListView;
                    action.SelectionDependencyType = SelectionDependencyType.RequireMultipleObjects;
                    action.PaintStyle = ActionItemPaintStyle.Image;
                    action.SetImage(CommonImage.Stop);
                    action.QuickAccess = true;
                })
                .WhenConcatExecution(e => e.SelectedObjects.Cast<IActiveWorkflowObject>().ToNowObservable().Do(o => o.Active = false)
                    .Finally(() => e.Action.Frame().ParentObject<CommandSuite>().ObjectSpace.CommitChanges()))
                .ToUnit()
                .ContinueOnFault(context:[nameof(DeActivateCommand)])
                .Finally(() => LogFast($"Exiting {nameof(DeActivateCommand)}"));
        }

        private static IObservable<Unit> ActivateCommand(this ApplicationModulesManager manager){
            LogFast($"Entering {nameof(ActivateCommand)}");
            return manager.RegisterViewSimpleAction(nameof(ActivateCommand), action => {
                    action.TargetObjectType = typeof(IActiveWorkflowObject);
                    action.TargetViewType = ViewType.ListView;
                    action.SelectionDependencyType = SelectionDependencyType.RequireMultipleObjects;
                    action.PaintStyle = ActionItemPaintStyle.Image;
                    action.SetImage(CommonImage.Start);
                    action.QuickAccess = true;
                })
                .WhenConcatExecution(e => e.SelectedObjects.Cast<IActiveWorkflowObject>().ToNowObservable()
                    .Do(o => o.Active = true)
                    .Finally(() => {
                        var suite = e.Action.Frame().ParentObject<CommandSuite>();
                        if (suite == null){
                            e.View().ObjectSpace.CommitChanges();
                            return;
                        }

                        suite.ObjectSpace.Delete(suite.Commands.SelectMany(command => command.Executions).ToArray());
                        suite.ObjectSpace.CommitChanges();
                    }))
                .ToUnit()
                .ContinueOnFault(context:[nameof(ActivateCommand)])
                .Finally(() => LogFast($"Exiting {nameof(ActivateCommand)}"));
        }

        internal static IObservable<Unit> WhenExecuteCommands(this XafApplication application){
            LogFast($"Entering {nameof(WhenExecuteCommands)}");
            return application.DeactivateNotUsedModuleCommands()
                .SelectMany(_ => application.WhenProviderObject<CommandSuite>(suite =>
                        suite.Active && suite.Commands.Any(command => command.Active))
                    .SelectManyItemResilient(suite => {
                        LogFast($"Executing active command suite '{suite.Name}'.");
                        return suite.Execute(application);
                    }))
                .ToUnit()
                .ContinueOnFault(context:[nameof(WhenExecuteCommands)])
                .Finally(() => LogFast($"Exiting {nameof(WhenExecuteCommands)}"));
        }

        public static IObservable<object[]> Execute(this CommandSuite suite,XafApplication application,Action<WorkflowCommand> executing=null){
            LogFast($"Entering {nameof(Execute)} for suite '{suite.Name}'.");
            if (!suite.Active&&!suite.Commands.Any(command => command.Active)){
                LogFast($"Suite '{suite.Name}' is not active, returning empty.");
                return Observable.Empty<object[]>();
            }
            var suiteDeactivated = application.WhenSuiteDeactivated(suite);
            var suiteModified = application.WhenSuiteModified(suite);
            var suiteChanged = suiteDeactivated.Merge(suiteModified);
            return Observable.Defer(() => application.UseObject(suite,commandSuite => {
                    var commands = commandSuite.Commands.Where(command => command.Active && !command.IsSource).ToArray();
                    LogFast($"Found {commands.Length} active commands in suite '{commandSuite.Name}'.");
                    return commands.ToNowObservable()
                        .SelectManyItemResilient(command => {
                            LogFast($"Executing command '{command}' from suite '{commandSuite.Name}'.");
                            return application.ExecuteCommand(command, executing).TakeUntil(suiteChanged);
                        });
                }))
                .RepeatWhen(_ => suiteModified.ToUnit())
                .TakeUntil(suiteDeactivated)
                .Finally(() => LogFast($"Exiting {nameof(Execute)} for suite '{suite.Name}'."));
        }
        
        private static IObservable<Unit> WhenSuiteModified(this XafApplication application, CommandSuite commandSuite) 
            => application.WhenProviderCommittedDetailed<WorkflowCommand>(ObjectModification.All, cmd => cmd.CommandSuite?.Oid == commandSuite.Oid )
                .Select(t => t)
                .MergeToUnit(commandSuite.ObjectSpace.WhenCommitted<CommandSuite>().ToObjects().Where(suite => suite.Oid==commandSuite.Oid))
                .MergeToUnit(application.WhenProviderCommittedDetailed<CommandSuite>(ObjectModification.All, suite => suite?.Oid == commandSuite.Oid ).Select(t => t))
                .ToConsole(_ => commandSuite)
            ;

        private static IObservable<Unit> WhenSuiteDeactivated(this XafApplication application, CommandSuite commandSuite)
            => application.WhenProviderCommittedDetailed<CommandSuite>(ObjectModification.All, [nameof(CommandSuite.Active)],suite1 => suite1.Oid == commandSuite.Oid && !suite1.Active)
                .MergeToUnit(commandSuite.ObjectSpace.WhenCommittedDetailed<CommandSuite>(ObjectModification.All,[nameof(CommandSuite.Active)],suite => suite.Oid==commandSuite.Oid&&!suite.Active).ToObjects() )
                .ToUnit();

        private static IObservable<CommandSuite[][]> DeactivateNotUsedModuleCommands(this XafApplication application) 
            => application.CommitChangesSequential(space => space.GetObjectsQuery<WorkflowCommand>().ToArray()
                .GroupBy(command => command.GetTypeInfo())
                .Where(commands => !commands.Key.IsDomainComponent)
                .SelectMany(commands => commands.Select(command => command.CommandSuite)).Distinct()
                .Execute(suite => suite.Active = false)
                .ToNowObservable().BufferUntilCompleted()
            );

        private static IObservable<object[]> ExecuteCommand(this XafApplication application, WorkflowCommand workflowCommand,Action<WorkflowCommand> executing){
            LogFast($"Entering {nameof(ExecuteCommand)} for command '{workflowCommand.FullName}'.");
            return workflowCommand.Validate().FromHierarchyAll(cmd => new[]{ cmd.StartAction?.Validate() }.WhereNotDefault().Where(command => command.Active)
                    .Concat(cmd.StartCommands.WhereNotDefault().Do(command => command.Validate()).Where(command => command.Active))).ToNowObservable()
                .SelectManyItemResilient(path => {
                    LogFast($"Executing path: {string.Join(" -> ", path.Select(c => c.FullName))}");
                    return path.Reverse().ToNowObservable()
                        .Aggregate(Array.Empty<object>().Observe(), (previousSource, currentCommand) => previousSource
                            .ObserveOnDefault()
                            .SelectManySequentialItemResilient(prevResult => {
                                LogFast($"Executing step '{currentCommand.FullName}' in path.");
                                executing?.Invoke(currentCommand);
                                return application.Execute(currentCommand, prevResult);
                            }))
                        .Select(innerResilientObservable => innerResilientObservable.AsObservable())
                        .Concat();
                })
                .ToConsole(_ => $"{workflowCommand.FullName}: {workflowCommand}")
                .Finally(() => LogFast($"Exiting {nameof(ExecuteCommand)} for command '{workflowCommand.FullName}'."));
        }

        private static readonly ISubject<(WorkflowCommand command, object[] result)> ExecutedSubject = Subject.Synchronize(new Subject<(WorkflowCommand actionObject, object[] result)>());

        public static IObservable<object[]> WhenExecuted(this WorkflowCommand workflowCommand) 
            => ExecutedSubject.Where(t => t.command.Oid == workflowCommand?.Oid).ToSecond();

        private static IObservable<object[]> Execute(this XafApplication application, WorkflowCommand workflowCommand, object[] objects){
            LogFast($"Entering low-level {nameof(Execute)} for command '{workflowCommand.FullName}'.");
            return application.UseProviderObjectSpace(space => {
                    workflowCommand = space.GetObject(workflowCommand);
                    if (!workflowCommand.Active){
                        LogFast($"Command '{workflowCommand.FullName}' is not active. Skipping execution.");
                        return Observable.Empty<object[]>();
                    }
                    LogFast($"Execute: {workflowCommand.FullName}, {workflowCommand}");
                    return workflowCommand.Defer(() => workflowCommand.Validate().Execute(application, objects))
                        .Do(objects1 => {
                            LogFast($"Command '{workflowCommand.FullName}' executed successfully. Emitting results to ExecutedSubject.");
                            ExecutedSubject.OnNext((workflowCommand, objects1??[]));
                        })
                        .TakeUntilModified(application, workflowCommand)
                        .TakeUntil(_ => !workflowCommand.Active)
                        .SelectMany(objects1 => !workflowCommand.ExecuteOnce ? objects1.Observe() : application.Disable(workflowCommand).To(objects1))
                        .ContinueOnFault(publishWhen: e => {
                            LogFast($"Error during execution of command '{workflowCommand.FullName}'. Exception: {e.GetType().Name}");
                            var shouldDisable = workflowCommand.DisableOnError || e is ValidationException;
                            if (shouldDisable) {
                                LogFast($"Disabling command '{workflowCommand.FullName}' due to error.");
                                return application.Disable(workflowCommand).To(true);
                            }
                            return Observable.Return(true);
                        },context:[workflowCommand.CommandSuite,workflowCommand])
                        .ToConsole(_ => $"{workflowCommand}")
                        .Finally(() => LogFast($"Exiting low-level {nameof(Execute)} for command '{workflowCommand.FullName}'."));
                }, typeof(WorkflowCommand))
                .TakeOrOriginal(workflowCommand.ExecuteOnce ? 1 : 0);
        }


        private static IObservable<WorkflowCommand> Disable(this XafApplication application, WorkflowCommand workflowCommand) 
            => application.UseObject(workflowCommand, command1 => {
                command1.Active = false;
                command1.CommitChanges();
                return workflowCommand.Observe();
            });

        private static IObservable<T> TakeUntilModified<T,TCommand>(this IObservable<T> source,
            XafApplication application, TCommand command,Func<TCommand,bool> filter=null) where TCommand : WorkflowCommand
            => source.TakeUntil(application.Defer(() => application.WhenObjectUpdatedOrDeleted(command)
                    .Where(arg => filter?.Invoke(arg)??true)
                .MergeToUnit(application.WhenDisposed()))
                .Select(unit => unit));
    }
}