using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Xpand.Extensions.ExceptionExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Reactive.Relay.Transaction {
    public static partial class Transaction {
        public static IObservable<TFinal[]> RunFailFast<TFinal>(this ITransactionBuilder<TFinal> builder, Func<Exception, bool> isNonCritical = null,
            DataSalvageStrategy dataSalvageStrategy = DataSalvageStrategy.EmitEmpty) 
            => builder.Run(failFast: true, collectAllResults: false, dataSalvageStrategy,  isNonCritical);
        
        private static IObservable<TFinal[]> RunFailFast<TFinal>(this IObservable<TFinal[]> source, TransactionBuilder<TFinal> ib) 
            => source.Catch((FaultHubException ex) => {
                if (ex.Data.Contains(SalvagedDataKey) && ex.Data[SalvagedDataKey] is IEnumerable<object> salvagedData) {
                    var typedData = salvagedData.AsArray<TFinal>();
                    if (typedData.Any()) {
                        return Observable.Return(typedData).Concat(ex.CreateAbortedException( ib).Throw<TFinal[]>());
                    }
                }

                var abortedException = ex.CreateAbortedException( ib);
                LogFast($"Throwing exception of type: {abortedException.GetType().FullName}");
                return abortedException.Throw<TFinal[]>();
            }).FlowContext(context:FaultHub.LogicalStackContext.Wrap());

        
        public static IObservable<TFinal> RunAndCollect<TFinal>(this ITransactionBuilder<object> builder, Func<object[], IObservable<TFinal>> resultSelector)
            => builder.Run(false,true).SelectMany(objects => resultSelector(objects.SelectMany(o => o as IEnumerable<object> ?? [o]).ToArray()));

        public static IObservable<TFinal[]> Run<TFinal>(this ITransactionBuilder<TFinal> builder, bool failFast = true,
            bool collectAllResults = false, DataSalvageStrategy dataSalvageStrategy = DataSalvageStrategy.EmitPartialResults, Func<Exception, bool> isNonCritical = null)
            => Observable.Defer(() => {
                var ib = (TransactionBuilder<TFinal>)builder;
                ib.DataSalvageStrategy = dataSalvageStrategy;
                return Observable.Return(Unit.Default).ContextualSource(ib.TransactionName)
                    .SelectMany(_ => {
                        LogFast($"{ib.TransactionName}: Run called. Mode={ib.Mode}, FailFast={failFast}, CollectAllResults={collectAllResults}");
                        return ib.Mode == TransactionMode.Concurrent ? ib.RunConcurrent( failFast) : ib.RunSequential( failFast, collectAllResults, isNonCritical);
                    });
            }).UseContext(true, FaultHub.IsChainingActive.Wrap());         
        
        private static IObservable<TFinal[]> RunSequential<TFinal>(this TransactionBuilder<TFinal> builder, bool failFast, bool collectAllResults, Func<Exception, bool> isNonCritical) {
            var logic = builder.TransactionLogic(failFast, collectAllResults, isNonCritical)
                .Select(o => o.AsArray<TFinal>());
            return failFast ? logic.RunFailFast(builder)
                : logic.ChainFaultContext( builder.Context, null, builder.TransactionName, builder.CallerMemberPath, builder.CallerMemberLine, builder.UpdateRunTags(collectAllResults));
        }
        
        private static FaultHubException CreateAbortedException<TFinal>(this FaultHubException ex, TransactionBuilder<TFinal> ib) {
            if (ex.Context.Tags.Contains(NonCriticalAggregateTag)) {
                return ex;
            }
            LogFast($"Creating TransactionAbortedException for transaction '{ib.TransactionName}'.");
            return new TransactionAbortedException($"{ib.TransactionName} failed", (Current != null && !Current.Failures.IsEmpty) ? Current.Failures.First() : ex,
                ib.Context,ib.Tags.Concat([TransactionNodeTag, ib.Mode.ToString(), nameof(RunFailFast)])
                    .Distinct().ToList(),ib.TransactionName);
        }
        
        private static IObservable<TFinal[]> RunConcurrent<TFinal>(this TransactionBuilder<TFinal> builder, bool failFast) {
            var logic = (builder.BatchedSources.ToObservable(builder.Scheduler ?? Scheduler.Default)
                    .TransactionLogic(builder, failFast))
                .Select(o => o.AsArray<TFinal>());

            return failFast ? logic.RunFailFast(builder)
                : logic.ChainFaultContext( builder.Context, null, builder.TransactionName, builder.CallerMemberPath, builder.CallerMemberLine, builder.UpdateRunTags(false));
        }
        
        private static IObservable<object> TransactionLogic<TFinal>(this IObservable<(string Name, IObservable<object> Source)> source, TransactionBuilder<TFinal> builder, bool failFast){
            return failFast
                ? source.ConcurrentFailFast(builder.TransactionName, 0, builder.Context, builder.CallerMemberPath, builder.CallerMemberLine).ToList().Select(list => (object)list)
                : source.ConcurrentRunToEnd(builder.TransactionName, 0, builder.Context, builder.CallerMemberPath, builder.CallerMemberLine)
                    .SelectMany(resultTuple => {
                        var resultStream = Observable.Return((object)resultTuple.Results);
                        return resultTuple.Fault == null ? resultStream : resultStream.Concat(Observable.Throw<object>(resultTuple.Fault));
                    });
        }
        
        
        private static IObservable<object> TransactionLogic<TFinal>(this TransactionBuilder<TFinal> builder,bool failFast, bool collectAllResults, Func<Exception, bool> isNonCritical = null){
            
            TransactionNestingLevel.Value++;
            var allSteps = new List<StepDefinition>();
            if (builder.InitialStep != null) {
                allSteps.Add(new StepDefinition { Selector = _ => builder.InitialStep, Name = builder.InitialStepName, FilePath = builder.CallerMemberPath, LineNumber = builder.CallerMemberLine });
            }
            else if (builder.Mode == TransactionMode.Sequential && builder.BatchedSources != null) {
                allSteps.AddRange(builder.BatchedSources.Select(bs => new StepDefinition { Selector = _ => bs.Source, Name = bs.Name, FilePath = builder.CallerMemberPath, LineNumber = builder.CallerMemberLine }));
            }
            allSteps.AddRange(builder.SubsequentSteps);
            return (failFast ? allSteps.FailFast(builder, isNonCritical) : builder.RunToEnd(allSteps,  collectAllResults)).Finally(() => {
                TransactionNestingLevel.Value--;
                var transactionBuilder = builder;
                FaultHub.LogicalStackContext.Value = null;
            });
        }
        private static IObservable<(List<object> Results, FaultHubException Fault)> ConcurrentRunToEnd(this IObservable<(string Name, IObservable<object> Source)> source, string transactionName, int maxConcurrency, object[] context,string filePath,int lineNumber) 
            => source.Select(op => op.Source.PushStackFrame( op.Name, filePath, lineNumber).ChainFaultContext(context:context.AddToContext($"{transactionName} - {op.Name}"),null, op.Name).Materialize())
                .Merge(maxConcurrency > 0 ? maxConcurrency : int.MaxValue).BufferUntilCompleted()
                .Select(notifications => {
                    var exceptions = notifications.Where(n => n.Kind == NotificationKind.OnError).Select(n => n.Exception).ToList();
                    return (Results: notifications.Where(n => n.Kind == NotificationKind.OnNext).Select(n => n.Value).ToList(), Fault: exceptions.Any()
                        ? new FaultHubException($"{transactionName} completed with errors", new AggregateException(exceptions),context.AddToContext(transactionName),boundaryName:transactionName) : null);
                });
        
        private static IObservable<object> ConcurrentFailFast(this IObservable<(string Name, IObservable<object> Source)> source, string transactionName, int maxConcurrency, object[] context,string filePath,int lineNumber) 
            => source.Select(op => op.Source.PushStackFrame(op.Name, filePath, lineNumber).ChainFaultContext(context:context.AddToContext($"{transactionName} - {op.Name}"),null, op.Name))
                .Merge(maxConcurrency > 0 ? maxConcurrency : int.MaxValue);
        
        private static IObservable<object> ExecuteStep(this StepDefinition step,
            (object results, List<Exception> failures, List<object> allResults,IReadOnlyList<LogicalStackFrame> logicalStack) acc, bool failFast) {
            var primaryBus = Observable.Defer(() => step.Selector(acc.results));
            return failFast || step.FallbackSelector == null ? primaryBus
                : primaryBus.Catch((Exception ex) => step.FallbackSelector(ex, acc.results));
        }
        
         private static IObservable<object> Execute(this StepDefinition step, (object results, List<Exception> failures, List<object> allResults, IReadOnlyList<LogicalStackFrame> logicalStack) acc, bool failFast) {
            var stepStream = step.ExecuteStep(acc, failFast);
            if (!failFast) {
                stepStream = stepStream.UseContext(true, FaultHub.PreserveLogicalStack.Wrap());
            }
            return stepStream.PushFrameConditionally(!string.IsNullOrEmpty(step.Name) ? step.Name : $"Part {step.GetHashCode()}", step.FilePath, step.LineNumber, preserveContext: true);
        }

        private static (object results, List<Exception> failures, List<object> allResults, IReadOnlyList<LogicalStackFrame> logicalStack) ProcessStepCompletion<TFinal>(
            IReadOnlyList<Notification<object>> notifications, StepDefinition step, (object results, List<Exception> failures, List<object> allResults, IReadOnlyList<LogicalStackFrame> logicalStack) acc,
            TransactionBuilder<TFinal> builder, List<StepDefinition> allSteps, bool failFast, Func<Exception, bool> isNonCritical) {

            LogFast($"Step '{step.Name}' completed. Received {notifications.Count} notifications ({notifications.Count(n => n.Kind == NotificationKind.OnNext)} OnNext, {notifications.Count(n => n.Kind == NotificationKind.OnError)} OnError).");

            IReadOnlyList<LogicalStackFrame> finalStackForStep;
            if (!failFast) {
                var errorNotification = notifications.FirstOrDefault(n => n.Kind == NotificationKind.OnError);
                if (errorNotification?.Exception is ExceptionWithLogicalContext contextException) {
                    finalStackForStep = contextException.ContextPath;
                }
                else {
                    finalStackForStep = FaultHub.LogicalStackContext.Value;
                }
            }
            else {
                finalStackForStep = ImmutableList<LogicalStackFrame>.Empty;
            }

            var (stepResults, stepFailures, stepAllResults) = allSteps.CollectStepErrors(builder, notifications, step, acc, isNonCritical);
            var suppressedFailures = Current?.Failures.Where(f => !stepFailures.Contains(f)).ToList();
            var combinedFailures = !(suppressedFailures?.Any() ?? false) ? stepFailures : stepFailures.Concat(suppressedFailures).ToList();
            var stackForNextStep = stepFailures.Any() ? acc.logicalStack : finalStackForStep;
            return (results: stepResults, failures: combinedFailures, allResults: stepAllResults, logicalStack: stackForNextStep);
        }
        
        private static IObservable<(object finalStepResult, List<Exception> allFailures, List<object> allResults)> ExecuteStepChain<TFinal>(this List<StepDefinition> allSteps, TransactionBuilder<TFinal> builder, bool failFast, Func<Exception, bool> isNonCritical = null)  
            => allSteps.Aggregate(Observable.Return((results: (object)new List<object>(), failures: new List<Exception>(), allResults: new List<object>(), logicalStack: (IReadOnlyList<LogicalStackFrame>)ImmutableList<LogicalStackFrame>.Empty)), 
                    (accObservable, step) => accObservable.SelectMany(acc => {
                        if (failFast && acc.failures.HasCriticalFailure(isNonCritical)) {
                            LogFast($"{builder.TransactionName}: SKIPPING step '{step.Name}' due to prior failure in FailFast mode.");
                            return Observable.Return(acc);
                        }

                        if (!failFast) {
                            FaultHub.LogicalStackContext.Value = acc.logicalStack as ImmutableList<LogicalStackFrame> ?? acc.logicalStack?.ToImmutableList();
                        }
                        
                        LogFast($"{builder.TransactionName}: ENTERING step '{step.Name}'.");

                        return step.Execute( acc, failFast)
                            .Materialize()
                            .BufferUntilCompleted()
                            .Select(notifications => ProcessStepCompletion(notifications, step, acc, builder, allSteps, failFast, isNonCritical));
                        
                    }))
                .Select(acc => (acc.results, acc.failures, acc.allResults))
                .FlowContext(context:FaultHub.LogicalStackContext.Wrap());

        private static bool HasCriticalFailure(this  List<Exception> failures,Func<Exception, bool> isNonCritical){
            var globalNonCriticalCheck = isNonCritical ?? (_ => false);
            return failures.Any(f => {
                if (f is FaultHubException fhEx && fhEx.Context.Tags.Contains(NonCriticalStepTag)) {
                    return false;
                }
                var rootCause = (f as FaultHubException)?.FindRootCauses().FirstOrDefault() ?? f;
                return !globalNonCriticalCheck(rootCause);
            });
        }

        private static (object results, List<Exception> failures, List<object> allResults) CollectStepErrors<TFinal>(this List<StepDefinition> allSteps,
            TransactionBuilder<TFinal> builder, IReadOnlyList<Notification<object>> notifications, StepDefinition step,
            (object results, List<Exception> failures, List<object> allResults, IReadOnlyList<LogicalStackFrame> logicalStack) acc, Func<Exception, bool> isNonCritical = null){
            var errorNotifications = notifications.Where(n => n.Kind == NotificationKind.OnError).ToList();
            if (errorNotifications.Any()) {
                var stepErrors = string.Join(", ", errorNotifications.Select(n => n.Exception?.GetType().Name));
                LogFast($"{builder.TransactionName}: CAPTURED {errorNotifications.Count} error(s) from step '{step.Name}': [{stepErrors}]");
            }
            var results = notifications.Where(n => n.Kind == NotificationKind.OnNext).Select(n => n.Value).ToList();
            var effectiveStrategy = step.DataSalvageStrategy == DataSalvageStrategy.Inherit ? builder.DataSalvageStrategy : step.DataSalvageStrategy;
            if (errorNotifications.Any() && effectiveStrategy == DataSalvageStrategy.EmitEmpty) {
                LogFast($"{builder.TransactionName}: Step '{step.Name}' failed with EmitEmpty strategy. Discarding {results.Count} partial results.");
                results.Clear();
            }
            var allResults = acc.allResults.Concat(results).ToList();
            var failures = acc.failures.Concat(allSteps.CollectErrors(builder, errorNotifications, step, results, isNonCritical)).ToList();
            return (results,  failures, allResults);
        }
        public static IObservable<TFinal[]> RunToEnd<TFinal>(this ITransactionBuilder<TFinal> builder, DataSalvageStrategy dataSalvageStrategy = DataSalvageStrategy.EmitPartialResults) 
            => builder.Run(false,dataSalvageStrategy:dataSalvageStrategy);
        private static IObservable<object> RunToEnd<TFinal>(this TransactionBuilder<TFinal> builder, List<StepDefinition> allSteps, bool collectAllResults)
            => allSteps.ExecuteStepChain(builder, false)
                .SelectMany(t => {
                    if (!t.allFailures.Any()) {
                        LogFast($"{builder.TransactionName}: RunToEnd: No failures. CollectAllResults={collectAllResults}, IsNested={TransactionNestingLevel.Value}");
                        return Observable.Return(collectAllResults ? t.allResults : (List<object>)t.finalStepResult);
                    }
                    LogFast($"{builder.TransactionName}: RunToEnd: Completed with {t.allFailures.Count} failure(s). Creating final aggregate exception.");
                    var aggregateException = new AggregateException(t.allFailures);
                    if (TransactionNestingLevel.Value ==1) {
                        LogFast($"{builder.TransactionName}: Path: Non-nested failure. isNested = {TransactionNestingLevel.Value}. Throwing.");
                    }
                    LogFast($"{builder.TransactionName}: Path: Nested failure. isNested = {TransactionNestingLevel.Value}. Proceeding to salvage.");
                    var finalTypedResults =builder.DataSalvageStrategy==DataSalvageStrategy.EmitPartialResults? t.allResults.OfType<TFinal>().Cast<object>().ToList():null;
                    return Observable.Return((object)finalTypedResults).WhenNotDefault()
                        .Concat(Observable.Throw<object>(aggregateException));
                });

        private static IEnumerable<FaultHubException> CollectErrors<TFinal>(this List<StepDefinition> allSteps,
            TransactionBuilder<TFinal> builder, List<Notification<object>> errors, StepDefinition step, List<object> stepResults, Func<Exception, bool> isNonCritical = null) {
            if (!errors.Any()) return [];
            LogFast($"{builder.TransactionName}: Collecting {errors.Count} error(s) for step '{step.Name}'.");
            var stepNameForContext = !string.IsNullOrEmpty(step.Name) ? step.Name : $"Part {allSteps.IndexOf(step) + 1}";
            var isNonCriticalCheck = step.IsNonCritical ?? (_ => false);
            return errors.Select(e => {
                var effectiveStrategy = step.DataSalvageStrategy == DataSalvageStrategy.Inherit ? builder.DataSalvageStrategy : step.DataSalvageStrategy;
                if (effectiveStrategy is DataSalvageStrategy.EmitPartialResults or DataSalvageStrategy.EmitEmpty) {
                    var salvagedData = effectiveStrategy == DataSalvageStrategy.EmitPartialResults ? stepResults : new List<object>();
                    e.Exception!.Data[SalvagedDataKey] = salvagedData;
                }

                if (e.Exception is ExceptionWithLogicalContext { OriginalException: FaultHubException faultHubException } && (faultHubException.Context.Tags?.Contains(AsStepOriginTag) ?? false)) {
                    LogFast($"Found wrapped '{AsStepOriginTag}'. Promoting inner FaultHubException for step '{faultHubException.Context.BoundaryName}'.");
                    return faultHubException;
                }
                if (e.Exception is FaultHubException fhEx && (fhEx.Context.Tags?.Contains(AsStepOriginTag) ?? false)) {
                    LogFast($"Found '{AsStepOriginTag}'. Preserving existing FaultHubException for step '{fhEx.Context.BoundaryName}'.");
                    return fhEx;
                }

                var exceptionToCheck = e.Exception;
                if (exceptionToCheck is ExceptionWithLogicalContext contextEx) {
                    exceptionToCheck = contextEx.OriginalException;
                }

                if (exceptionToCheck is TransactionAbortedException abortedException) {
                    LogFast($"Found '{nameof(TransactionAbortedException)}'. Preserving existing exception for transaction '{abortedException.Context.BoundaryName}'.");
                    return abortedException;
                }
                var rootCause = e.Exception.SelectMany().LastOrDefault(ex => ex is not AggregateException and not FaultHubException) ?? e.Exception;
                var isNonCriticalResult = isNonCriticalCheck(rootCause) || (isNonCritical?.Invoke(rootCause) ?? false);
                var tags = new List<string> { StepNodeTag };
                if (isNonCriticalResult) {
                    tags.Add(NonCriticalStepTag);
                }

                if (e.Exception is ExceptionWithLogicalContext exceptionWithContext) {
                    foreach (DictionaryEntry entry in exceptionWithContext.Data) {
                        if (!exceptionWithContext.OriginalException.Data.Contains(entry.Key)) {
                            exceptionWithContext.OriginalException.Data[entry.Key] = entry.Value;
                        }
                    }
                    var completeContext = new AmbientFaultContext {
                        LogicalStackTrace = exceptionWithContext.ContextPath,
                        BoundaryName = stepNameForContext,
                        UserContext = builder.Context.AddToContext(builder.TransactionName, $"{builder.TransactionName} - {stepNameForContext}"),
                        Tags = tags
                    };
                    return exceptionWithContext.OriginalException.ExceptionToPublish(completeContext);
                }
                var fallbackContext = new AmbientFaultContext {
                    LogicalStackTrace = FaultHub.LogicalStackContext.Value,
                    BoundaryName = stepNameForContext,
                    UserContext = builder.Context.AddToContext(builder.TransactionName, $"{builder.TransactionName} - {stepNameForContext}"),
                    Tags = tags
                };
                return e.Exception.ExceptionToPublish(fallbackContext);
            });
        }        
        private static IObservable<object> FailFast<TFinal>(this  List<StepDefinition> allSteps,TransactionBuilder<TFinal> builder, Func<Exception, bool> isNonCritical = null) 
            => allSteps.ExecuteStepChain(builder,true, isNonCritical)
                .SelectMany(t => {
                    if (!t.allFailures.Any()) return Observable.Return(t.finalStepResult);
                    var criticalFailure = t.allFailures.FirstOrDefault(f => {
                        if (f is FaultHubException fhEx && fhEx.Context.Tags.Contains(NonCriticalStepTag)) {
                            return false;
                        }
                        var rootCause = (f as FaultHubException)?.FindRootCauses().FirstOrDefault() ?? f;
                        return !(isNonCritical?.Invoke(rootCause) ?? false);
                    });

                    if (criticalFailure != null) {
                        return Observable.Throw<object>(criticalFailure);                    }

                    LogFast($"{builder.TransactionName}: FailFast with predicate: All failures are non-critical. Aggregating.");
                    return new FaultHubException($"{builder.TransactionName} completed with non-critical errors", new AggregateException(t.allFailures), builder.Context,
                            builder.UpdateRunTags(false).Concat([NonCriticalAggregateTag]).ToArray(),builder.TransactionName, builder.CallerMemberPath, builder.CallerMemberLine)
                        .Throw<object>();
                });
    }

    }



