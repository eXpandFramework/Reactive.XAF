using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub {
    public static partial class Transaction {
        private const string NonCriticalAggregateTag = "NonCriticalAggregate";
        public static IObservable<TFinal[]> RunFailFast<TFinal>(this ITransactionBuilder<TFinal> builder, Func<Exception, bool> isNonCritical=null) 
            => builder.Run(failFast: true, isNonCritical: isNonCritical);
        
        
        public static IObservable<TFinal> RunAndCollect<TFinal>(this ITransactionBuilder<object> builder, Func<object[], IObservable<TFinal>> resultSelector)
            => builder.Run(false,true).SelectMany(objects => resultSelector(objects.SelectMany(o => o as IEnumerable<object> ?? [o]).ToArray()));

        public static IObservable<TFinal[]> Run<TFinal>(this ITransactionBuilder<TFinal> builder, bool failFast = true, bool collectAllResults = false, Func<Exception, bool> isNonCritical = null)
            => Observable.Defer(() => {
                var ib = (TransactionBuilder<TFinal>)builder;
                LogFast($"[Tx:{ib.TransactionName}] Run called. Mode={ib.Mode}, FailFast={failFast}, CollectAllResults={collectAllResults}");
                var scheduledLogic = ib.ScheduledLogic(failFast, collectAllResults, isNonCritical);
                return failFast ? scheduledLogic.RunFailFast(ib)
                    : scheduledLogic.ChainFaultContext(ib.Context, null, ib.TransactionName, ib.CallerMemberPath, ib.CallerMemberLine, ib.UpdateRunTags(collectAllResults));
            });

        private static IObservable<TFinal[]> RunFailFast<TFinal>(this IObservable<TFinal[]> source, TransactionBuilder<TFinal> ib) 
            => source.Catch((FaultHubException ex) => {
                if (ex.Context.Tags.Contains(NonCriticalAggregateTag)) {
                    return Observable.Throw<TFinal[]>(ex);
                }
                LogFast($"[INSTRUMENTATION][Transaction.Run] Creating TransactionAbortedException for transaction '{ib.TransactionName}'.");
                return new TransactionAbortedException($"{ib.TransactionName} failed", (Current != null && !Current.Failures.IsEmpty) ? Current.Failures.First() : ex,
                    ib.Context,ib.Tags.Concat([TransactionNodeTag, ib.Mode.ToString(), nameof(RunFailFast)])
                        .Distinct().ToList(),ib.TransactionName).Throw<TFinal[]>();
            });

        private static IObservable<TFinal[]> ScheduledLogic<TFinal>(this TransactionBuilder<TFinal> builder,bool failFast, bool collectAllResults, Func<Exception, bool> isNonCritical = null){
            var finalLogic = (builder.Mode == TransactionMode.Concurrent && builder.BatchedSources != null?builder.TransactionLogic(failFast):builder.TransactionLogic(failFast, collectAllResults, isNonCritical))
                .Select(CreateInputArray<TFinal>);
            return builder.Scheduler == null ? finalLogic : finalLogic.SubscribeOn(builder.Scheduler);
        }
        
        private static IObservable<object> TransactionLogic<TFinal>(this TransactionBuilder<TFinal> builder,bool failFast, bool collectAllResults, Func<Exception, bool> isNonCritical = null){
            var isNested = TransactionNestingLevel.Value > 0;
            TransactionNestingLevel.Value++;
            var allSteps = new List<StepDefinition>();
            if (builder.InitialStep != null) {
                allSteps.Add(new StepDefinition { Selector = _ => builder.InitialStep, Name = builder.InitialStepName, FilePath = builder.CallerMemberPath, LineNumber = builder.CallerMemberLine });
            }
            else if (builder.Mode == TransactionMode.Sequential && builder.BatchedSources != null) {
                allSteps.AddRange(builder.BatchedSources.Select(bs => new StepDefinition { Selector = _ => bs.Source, Name = bs.Name, FilePath = builder.CallerMemberPath, LineNumber = builder.CallerMemberLine }));
            }
            allSteps.AddRange(builder.SubsequentSteps);
            return (failFast ? allSteps.FailFast(builder, isNonCritical) : builder.RunToEnd(allSteps, isNested, collectAllResults)).Finally(() => TransactionNestingLevel.Value--);
        }

        private static IObservable<object> TransactionLogic<TFinal>(this TransactionBuilder<TFinal> builder,bool failFast){
            var concurrentSources = builder.BatchedSources.ToObservable(builder.Scheduler ?? Scheduler.Default);
            return failFast? concurrentSources.ConcurrentFailFast(builder.TransactionName, 0, builder.Context, builder.CallerMemberPath, builder.CallerMemberLine)
                .ToList().Select(list => (object)list):concurrentSources.ConcurrentRunToEnd(builder.TransactionName, 0, builder.Context, builder.CallerMemberPath, builder.CallerMemberLine)
                .SelectMany(resultTuple => {
                    var resultStream = Observable.Return(resultTuple.Results);
                    return resultTuple.Fault == null ? resultStream : resultStream.Concat(Observable.Throw<object>(resultTuple.Fault));
        
                });
        }
        private static IObservable<(List<object> Results, FaultHubException Fault)> ConcurrentRunToEnd(this IObservable<(string Name, IObservable<object> Source)> source, string transactionName, int maxConcurrency, object[] context,string filePath,int lineNumber) 
            => source.Select(op => op.Source.PushStackFrame(op.Name, filePath, lineNumber)
                    .ChainFaultContext(context:context.AddToContext($"{transactionName} - {op.Name}"),null, op.Name).Materialize())
                .Merge(maxConcurrency > 0 ? maxConcurrency : int.MaxValue).BufferUntilCompleted()
                .Select(notifications => {
                    var exceptions = notifications.Where(n => n.Kind == NotificationKind.OnError).Select(n => n.Exception).ToList();
                    return (Results: notifications.Where(n => n.Kind == NotificationKind.OnNext).Select(n => n.Value).ToList(), Fault: exceptions.Any()
                        ? new FaultHubException($"{transactionName} completed with errors", new AggregateException(exceptions),context.AddToContext(transactionName),boundaryName:transactionName) : null);
                });
        
        private static IObservable<object> ConcurrentFailFast(this IObservable<(string Name, IObservable<object> Source)> source, string transactionName, int maxConcurrency, object[] context,string filePath,int lineNumber) 
            => source.Select(op => op.Source.PushStackFrame(op.Name, filePath, lineNumber)
                    .ChainFaultContext(context:context.AddToContext($"{transactionName} - {op.Name}"),null, op.Name))
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
                        return step.ExecuteStep(acc, failFast)
                            .PushFrameConditionally(!string.IsNullOrEmpty(step.Name) ? step.Name : $"Part {allSteps.IndexOf(step) + 1}",step.FilePath,step.LineNumber)
                            .Materialize().BufferUntilCompleted()
                            .Select(notifications => allSteps.CollectStepErrors( builder, notifications, step, acc));                                        
                    }))
                .Select(acc => (acc.results, acc.failures, acc.allResults));

        private static bool HasCriticalFailure(this  List<Exception> failures,Func<Exception, bool> isNonCritical){
            var nonCriticalCheck = isNonCritical ?? (_ => false);
            return failures.Any(f => {
                var rootCause = (f as FaultHubException)?.FindRootCauses().FirstOrDefault() ?? f;
                return !nonCriticalCheck(rootCause);
            });
        }

        private static (object results, List<Exception> failures, List<object> allResults) CollectStepErrors<TFinal>(this List<StepDefinition> allSteps,
            TransactionBuilder<TFinal> builder, IReadOnlyList<Notification<object>> notifications, StepDefinition step,
            (object results, List<Exception> failures, List<object> allResults) acc){
            var errorNotifications = notifications.Where(n => n.Kind == NotificationKind.OnError).ToList();
            if (errorNotifications.Any()) {
                var stepErrors = string.Join(", ", errorNotifications.Select(n => n.Exception?.GetType().Name));
                LogFast($"[Tx-FORNSC:{builder.TransactionName}][StepChain] -- CAPTURED {errorNotifications.Count} error(s) from step '{step.Name}': [{stepErrors}]");
            }
            var results = notifications.Where(n => n.Kind == NotificationKind.OnNext).Select(n => n.Value).ToList();
            var allResults = acc.allResults.Concat(results).ToList();
            var failures = acc.failures.Concat(allSteps.CollectErrors(builder, errorNotifications, step)).ToList();
            var exitFailures = failures.Any() ? string.Join(", ", failures.Select(f => f.GetType().Name)) : "empty";
            LogFast($"[Tx-FORNSC:{builder.TransactionName}][StepChain] <== EXITING step '{step.Name}'. Accumulator now has {failures.Count} failure(s): [{exitFailures}]");
            return (results,  failures, allResults);
        }
        public static IObservable<TFinal[]> RunToEnd<TFinal>(this ITransactionBuilder<TFinal> builder) => builder.Run(false);
        private static IObservable<object> RunToEnd<TFinal>(this TransactionBuilder<TFinal> builder, List<StepDefinition> allSteps, bool isNested,bool collectAllResults)
            => allSteps.ExecuteStepChain(builder, false)
                .SelectMany(t => {
                    if (!t.allFailures.Any()) {
                        LogFast($"[Tx:{builder.TransactionName}] RunToEnd: No failures. CollectAllResults={collectAllResults}, IsNested={isNested}");
                        return Observable.Return(collectAllResults ? t.allResults : (List<object>)t.finalStepResult);
                    }
                    LogFast($"[Tx:{builder.TransactionName}] RunToEnd: Completed with {t.allFailures.Count} failure(s). Creating final aggregate exception.");
                    var aggregateException = new AggregateException(t.allFailures);
                    if (!isNested) return Observable.Throw<object>(aggregateException);
                    var finalTypedResults = t.allResults.OfType<TFinal>().Cast<object>().ToList();
                    return Observable.Return((object)finalTypedResults).Concat(Observable.Throw<object>(aggregateException));
                });

        private static IEnumerable<FaultHubException> CollectErrors<TFinal>(this List<StepDefinition> allSteps, TransactionBuilder<TFinal> builder, List<Notification<object>> errors, StepDefinition step) {
            if (!errors.Any()) return [];
            LogFast($"[Tx:{builder.TransactionName}][CollectErrors] Collecting {errors.Count} error(s) for step '{step.Name}'.");
            var stepNameForContext = !string.IsNullOrEmpty(step.Name) ? step.Name : $"Part {allSteps.IndexOf(step) + 1}";
            return errors.Select(e => e.Exception.ExceptionToPublish( builder.Context.AddToContext(builder.TransactionName,
                $"{builder.TransactionName} - {stepNameForContext}"), [StepNodeTag], stepNameForContext));
        }
        
        private static IObservable<object> FailFast<TFinal>(this  List<StepDefinition> allSteps,TransactionBuilder<TFinal> builder, Func<Exception, bool> isNonCritical = null) 
            => allSteps.ExecuteStepChain(builder,true, isNonCritical)
                .SelectMany(t => {
                    if (!t.allFailures.Any()) return Observable.Return(t.finalStepResult);
                    if (isNonCritical == null) return Observable.Throw<object>(t.allFailures.First());
                    var anyCritical = t.allFailures.Any(f => !isNonCritical((f as FaultHubException)?.FindRootCauses().FirstOrDefault() ?? f));
                    if (anyCritical) return t.allFailures.First().Throw<object>();
                    LogFast($"[Tx:{builder.TransactionName}] FailFast with predicate: All failures are non-critical. Aggregating.");
                    return new FaultHubException($"{builder.TransactionName} completed with non-critical errors", new AggregateException(t.allFailures), builder.Context,
                        builder.UpdateRunTags(false).Concat([NonCriticalAggregateTag]).ToArray(),builder.TransactionName, builder.CallerMemberPath, builder.CallerMemberLine)
                        .Throw<object>();
                });
    }

    }



