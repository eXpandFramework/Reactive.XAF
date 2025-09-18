using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Xpand.Extensions.ExceptionExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;

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
                        return Observable.Return(typedData).Concat(CreateAbortedException(ex, ib).Throw<TFinal[]>());
                    }
                }
                return CreateAbortedException(ex, ib).Throw<TFinal[]>();
            });

        
        public static IObservable<TFinal> RunAndCollect<TFinal>(this ITransactionBuilder<object> builder, Func<object[], IObservable<TFinal>> resultSelector)
            => builder.Run(false,true).SelectMany(objects => resultSelector(objects.SelectMany(o => o as IEnumerable<object> ?? [o]).ToArray()));

        public static IObservable<TFinal[]> Run<TFinal>(this ITransactionBuilder<TFinal> builder, bool failFast = true,
            bool collectAllResults = false, DataSalvageStrategy dataSalvageStrategy = DataSalvageStrategy.EmitPartialResults, Func<Exception, bool> isNonCritical = null)
            => Observable.Defer(() => {
                var ib = (TransactionBuilder<TFinal>)builder;
                ib.DataSalvageStrategy = dataSalvageStrategy;
                return Observable.Return(Unit.Default).ContextualSource(ib.TransactionName)
                    .SelectMany(_ => {
                        LogFast($"[Tx:{ib.TransactionName}] Run called. Mode={ib.Mode}, FailFast={failFast}, CollectAllResults={collectAllResults}");
                        return ib.Mode == TransactionMode.Concurrent ? ib.RunConcurrent( failFast) : ib.RunSequential( failFast, collectAllResults, isNonCritical);
                    });
            });        
        
        private static IObservable<TFinal[]> RunSequential<TFinal>(this TransactionBuilder<TFinal> builder, bool failFast, bool collectAllResults, Func<Exception, bool> isNonCritical) {
            var logic = builder.TransactionLogic(failFast, collectAllResults, isNonCritical)
                .Select(o => o.AsArray<TFinal>());
            return failFast ? logic.RunFailFast(builder)
                : logic.ChainFaultContext( builder.Context, null, builder.TransactionName, builder.CallerMemberPath, builder.CallerMemberLine, builder.UpdateRunTags(collectAllResults));
        }
        
        private static FaultHubException CreateAbortedException<TFinal>(FaultHubException ex, TransactionBuilder<TFinal> ib) {
            if (ex.Context.Tags.Contains(NonCriticalAggregateTag)) {
                return ex;
            }
            LogFast($"[INSTRUMENTATION][Transaction.Run] Creating TransactionAbortedException for transaction '{ib.TransactionName}'.");
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
            return (failFast ? allSteps.FailFast(builder, isNonCritical) : builder.RunToEnd(allSteps,  collectAllResults)).Finally(() => TransactionNestingLevel.Value--);
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
            (object results, List<Exception> failures, List<object> allResults) acc, bool failFast) {
            var primaryBus = Observable.Defer(() => step.Selector(acc.results));
            return failFast || step.FallbackSelector == null ? primaryBus
                : primaryBus.Catch((Exception ex) => step.FallbackSelector(ex, acc.results));
        }
        
        private static IObservable<(object finalStepResult, List<Exception> allFailures, List<object> allResults)> ExecuteStepChain<TFinal>(this List<StepDefinition> allSteps, TransactionBuilder<TFinal> builder, bool failFast, Func<Exception, bool> isNonCritical = null)  
            => allSteps.Aggregate(Observable.Return((results: (object)new List<object>(), failures: new List<Exception>(), allResults: new List<object>())), 
                    (accObservable, step) => accObservable.SelectMany(acc => {
                        if (failFast && acc.failures.HasCriticalFailure(isNonCritical)) {
                            LogFast($"[Tx-FORNSC:{builder.TransactionName}][StepChain] ==> SKIPPING step '{step.Name}' due to prior failure in FailFast mode.");
                            return Observable.Return(acc);
                        }
                        var accFailures = acc.failures.Any() ? string.Join(", ", acc.failures.Select(f => f.GetType().Name)) : "empty";
                        LogFast($"[Tx-FORNSC:{builder.TransactionName}][StepChain] ==> ENTERING step '{step.Name}'. Accumulator has {acc.failures.Count} failure(s): [{accFailures}]");
                        
                        var resultsCount = acc.results.IsEnumerable() ? ((IEnumerable)acc.results).Cast<object>().Count() : 1;
                        LogFast($"[ACCUMULATOR-IN] For Step '{step.Name}': Results Count = {resultsCount}, Failures Count = {acc.failures.Count} ({accFailures})");
                        var lastFailure = acc.failures.LastOrDefault();
                        if (lastFailure != null && lastFailure.Data.Contains(SalvagedDataKey)) {
                            var salvaged = lastFailure.Data[SalvagedDataKey];
                            var salvagedCount = salvaged.IsEnumerable() ? ((IEnumerable)salvaged)!.Cast<object>().Count() : 1;
                            LogFast($"[ACCUMULATOR-IN]   - Last failure ('{lastFailure.GetType().Name}') contains salvaged data. Type: {salvaged?.GetType().Name}, Count: {salvagedCount}");
                        }
                        return step.ExecuteStep(acc, failFast)
                            .PushFrameConditionally(!string.IsNullOrEmpty(step.Name) ? step.Name : $"Part {allSteps.IndexOf(step) + 1}",step.FilePath,step.LineNumber)
                            .Materialize().BufferUntilCompleted()
                            .Select(notifications => {
                                LogFast($"[ExecuteStepChain] Step '{step.Name}' completed. Received {notifications.Length} notifications ({notifications.Count(n => n.Kind == NotificationKind.OnNext)} OnNext, {notifications.Count(n => n.Kind == NotificationKind.OnError)} OnError).");
                                var newAcc = allSteps.CollectStepErrors( builder, notifications, step, acc, isNonCritical);
                                var suppressedFailures = Current?.Failures.Where(f => !newAcc.failures.Contains(f)).ToList();
                                if (suppressedFailures?.Any() ?? false) {
                                    return newAcc with { failures = newAcc.failures.Concat(suppressedFailures).ToList() };
                                }
                                return newAcc;
                            });                                        
                    }))
                .Select(acc => (acc.results, acc.failures, acc.allResults));

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
            (object results, List<Exception> failures, List<object> allResults) acc, Func<Exception, bool> isNonCritical = null){
            LogFast($"[CollectStepErrors] PRE-PROCESSING for step '{step.Name}': acc.allResults.Count = {acc.allResults.Count}");
            var errorNotifications = notifications.Where(n => n.Kind == NotificationKind.OnError).ToList();
            if (errorNotifications.Any()) {
                var stepErrors = string.Join(", ", errorNotifications.Select(n => n.Exception?.GetType().Name));
                LogFast($"[Tx-FORNSC:{builder.TransactionName}][StepChain] -- CAPTURED {errorNotifications.Count} error(s) from step '{step.Name}': [{stepErrors}]");
            }
            var results = notifications.Where(n => n.Kind == NotificationKind.OnNext).Select(n => n.Value).ToList();
            var effectiveStrategy = step.DataSalvageStrategy == DataSalvageStrategy.Inherit ? builder.DataSalvageStrategy : step.DataSalvageStrategy;
            if (errorNotifications.Any() && effectiveStrategy == DataSalvageStrategy.EmitEmpty) {
                LogFast($"[Tx-FORNSC:{builder.TransactionName}][StepChain] -- Step '{step.Name}' failed with EmitEmpty strategy. Discarding {results.Count} partial results.");
                results.Clear();
            }
            var allResults = acc.allResults.Concat(results).ToList();
            var failures = acc.failures.Concat(allSteps.CollectErrors(builder, errorNotifications, step, results, isNonCritical)).ToList();
            LogFast($"[CollectStepErrors] POST-PROCESSING for step '{step.Name}': allResults.Count = {allResults.Count}, failures.Count = {failures.Count}");
            var exitFailures = failures.Any() ? string.Join(", ", failures.Select(f => f.GetType().Name)) : "empty";
            LogFast($"[Tx-FORNSC:{builder.TransactionName}][StepChain] <== EXITING step '{step.Name}'. Accumulator now has {failures.Count} failure(s): [{exitFailures}]");
            return (results,  failures, allResults);
        }
        public static IObservable<TFinal[]> RunToEnd<TFinal>(this ITransactionBuilder<TFinal> builder, DataSalvageStrategy dataSalvageStrategy = DataSalvageStrategy.EmitPartialResults) 
            => builder.Run(false,dataSalvageStrategy:dataSalvageStrategy);
        private static IObservable<object> RunToEnd<TFinal>(this TransactionBuilder<TFinal> builder, List<StepDefinition> allSteps, bool collectAllResults)
            => allSteps.ExecuteStepChain(builder, false)
                .SelectMany(t => {
                    if (!t.allFailures.Any()) {
                        LogFast($"[Tx:{builder.TransactionName}] RunToEnd: No failures. CollectAllResults={collectAllResults}, IsNested={TransactionNestingLevel.Value}");
                        return Observable.Return(collectAllResults ? t.allResults : (List<object>)t.finalStepResult);
                    }
                    LogFast($"[Tx:{builder.TransactionName}] RunToEnd: Completed with {t.allFailures.Count} failure(s). Creating final aggregate exception.");
                    var aggregateException = new AggregateException(t.allFailures);
                    if ((TransactionNestingLevel.Value ==1)) {
                        LogFast($"[Tx:{builder.TransactionName}] Path: Non-nested failure. isNested = {TransactionNestingLevel.Value}. Throwing.");
                    }
                    LogFast($"[Tx:{builder.TransactionName}] Path: Nested failure. isNested = {TransactionNestingLevel.Value}. Proceeding to salvage.");
                    
                    var finalTypedResults =builder.DataSalvageStrategy==DataSalvageStrategy.EmitPartialResults? t.allResults.OfType<TFinal>().Cast<object>().ToList():null;
                    return Observable.Return((object)finalTypedResults).WhenNotDefault()
                        .Concat(Observable.Throw<object>(aggregateException));
                });

        private static IEnumerable<FaultHubException> CollectErrors<TFinal>(this List<StepDefinition> allSteps,
            TransactionBuilder<TFinal> builder, List<Notification<object>> errors, StepDefinition step, List<object> stepResults, Func<Exception, bool> isNonCritical = null) {
            if (!errors.Any()) return [];
            LogFast($"[Tx:{builder.TransactionName}][CollectErrors] Collecting {errors.Count} error(s) for step '{step.Name}'.");
            var stepNameForContext = !string.IsNullOrEmpty(step.Name) ? step.Name : $"Part {allSteps.IndexOf(step) + 1}";
            var isNonCriticalCheck = step.IsNonCritical ?? (_ => false);
            return errors.Select(e => {
                LogFast($"[DIAGNOSTIC][CollectErrors] Processing exception of type '{e.Exception?.GetType().Name}'.");
                var effectiveStrategy = step.DataSalvageStrategy == DataSalvageStrategy.Inherit ? builder.DataSalvageStrategy : step.DataSalvageStrategy;
                if (effectiveStrategy is DataSalvageStrategy.EmitPartialResults or DataSalvageStrategy.EmitEmpty) {
                    var salvagedData = effectiveStrategy == DataSalvageStrategy.EmitPartialResults ? stepResults : new List<object>();
                    e.Exception!.Data[SalvagedDataKey] = salvagedData;
                }

                if (e.Exception is FaultHubException fhEx && (fhEx.Context.Tags?.Contains(AsStepOriginTag) ?? false)) {
                    LogFast($"[DIAGNOSTIC][CollectErrors] Found '{AsStepOriginTag}'. Preserving existing FaultHubException for step '{fhEx.Context.BoundaryName}'.");
                    return fhEx;
                }

                var rootCause = e.Exception.SelectMany().LastOrDefault(ex => ex is not AggregateException and not FaultHubException) ?? e.Exception;
                var isNonCriticalResult = isNonCriticalCheck(rootCause) || (isNonCritical?.Invoke(rootCause) ?? false);
                var tags = new List<string> { StepNodeTag };
                if (isNonCriticalResult) {
                    tags.Add(NonCriticalStepTag);
                }

                LogFast($"[DIAGNOSTIC][CollectErrors] Wrapping exception. Applying tags: [{string.Join(", ", tags)}]. Step context: '{stepNameForContext}'.");
                return e.Exception.ExceptionToPublish(builder.Context.AddToContext(builder.TransactionName,
                    $"{builder.TransactionName} - {stepNameForContext}"), tags, stepNameForContext);
            });
        }        private static IObservable<object> FailFast<TFinal>(this  List<StepDefinition> allSteps,TransactionBuilder<TFinal> builder, Func<Exception, bool> isNonCritical = null) 
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

                    LogFast($"[Tx:{builder.TransactionName}] FailFast with predicate: All failures are non-critical. Aggregating.");
                    return new FaultHubException($"{builder.TransactionName} completed with non-critical errors", new AggregateException(t.allFailures), builder.Context,
                            builder.UpdateRunTags(false).Concat([NonCriticalAggregateTag]).ToArray(),builder.TransactionName, builder.CallerMemberPath, builder.CallerMemberLine)
                        .Throw<object>();
                });
    }

    }



