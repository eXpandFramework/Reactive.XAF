using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Transform;
using static Xpand.Extensions.Reactive.ErrorHandling.FaultHub.FaultHubLogger;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub {
    public static partial class Transaction {
        public static IObservable<TFinal[]> RunFailFast<TFinal>(this ITransactionBuilder<TFinal> builder) => builder.Run();
        
        public static IObservable<TFinal[]> RunToEnd<TFinal>(this ITransactionBuilder<TFinal> builder) => builder.Run(false);

        public static IObservable<TFinal> RunAndCollect<TFinal>(this ITransactionBuilder<object> builder, Func<object[], IObservable<TFinal>> resultSelector)
            => builder.Run(false,true).SelectMany(objects => resultSelector(objects.SelectMany(o => o as IEnumerable<object> ?? [o]).ToArray()));

        public static IObservable<TFinal[]> Run<TFinal>(this ITransactionBuilder<TFinal> builder, bool failFast = true, bool collectAllResults = false)
            => Observable.Defer(() => {
                var ib = (TransactionBuilder<TFinal>)builder;
                Log(() => $"[Tx:{ib.TransactionName}] Run called. Mode={ib.Mode}, FailFast={failFast}, CollectAllResults={collectAllResults}");
                var scheduledLogic = ib.ScheduledLogic(failFast, collectAllResults);
                return failFast?scheduledLogic.Catch((FaultHubException ex) => {
                    Log(() => $"[INSTRUMENTATION][Transaction.Run] Creating TransactionAbortedException for transaction '{ib.TransactionName}'.");
                    return Observable.Throw<TFinal[]>(new TransactionAbortedException($"{ib.TransactionName} failed", ex, new AmbientFaultContext {
                        BoundaryName = ib.TransactionName, UserContext = ib.Context, InnerContext = ex.Context, Tags = ib.Tags.Concat([TransactionNodeTag,ib.Mode.ToString(),nameof(RunFailFast)]).Distinct().ToList()
                    }));
                }):scheduledLogic.ChainFaultContext(ib.Context, null, ib.CallerMemberName, ib.CallerMemberPath, ib.CallerMemberLine,
                    ib.UpdateTags(collectAllResults));
            });
        
        private static IObservable<TFinal[]> ScheduledLogic<TFinal>(this TransactionBuilder<TFinal> builder,bool failFast, bool collectAllResults){
            var finalLogic = (builder.Mode == TransactionMode.Concurrent && builder.BatchedSources != null?builder.TransactionLogic(failFast):builder.TransactionLogic(failFast, collectAllResults))
                .Select(CreateInputArray<TFinal>);
            return builder.Scheduler == null ? finalLogic : finalLogic.SubscribeOn(builder.Scheduler);
        }
        
        private static IObservable<object> TransactionLogic<TFinal>(this TransactionBuilder<TFinal> builder,bool failFast, bool collectAllResults){
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
            return (failFast ? allSteps.FailFast(builder) : builder.RunToEnd(allSteps, isNested, collectAllResults)).Finally(() => TransactionNestingLevel.Value--);
        }

        private static IObservable<object> TransactionLogic<TFinal>(this TransactionBuilder<TFinal> builder,bool failFast){
            var concurrentSources = builder.BatchedSources.ToObservable(builder.Scheduler ?? Scheduler.Default);
            return failFast? concurrentSources.ConcurrentFailFast(builder.TransactionName, 0, builder.Context)
                .ToList().Select(list => (object)list):concurrentSources.ConcurrentRunToEnd(builder.TransactionName, 0, builder.Context)
                .SelectMany(resultTuple => {
                    IObservable<object> resultStream = Observable.Return(resultTuple.Results);
                    if (resultTuple.Fault == null) return resultStream;
                    resultStream = resultStream.Concat(Observable.Throw<object>(resultTuple.Fault));
                    return resultStream;
                });
        }
        private static IObservable<(List<object> Results, FaultHubException Fault)> ConcurrentRunToEnd(this IObservable<(string Name, IObservable<object> Source)> source, string transactionName, int maxConcurrency, object[] context) 
            => source.Select(op => op.Source.PushStackFrame(op.Name)
                    .ChainFaultContext(context:context.AddToContext($"{transactionName} - {op.Name}"),null, op.Name).Materialize())
                .Merge(maxConcurrency > 0 ? maxConcurrency : int.MaxValue).BufferUntilCompleted()
                .Select(notifications => {
                    var exceptions = notifications.Where(n => n.Kind == NotificationKind.OnError).Select(n => n.Exception).ToList();
                    return (Results: notifications.Where(n => n.Kind == NotificationKind.OnNext).Select(n => n.Value).ToList(), Fault: exceptions.Any()
                        ? new FaultHubException($"{transactionName} completed with errors", new AggregateException(exceptions),
                            FaultHub.LogicalStackContext.Value.NewFaultContext(context.AddToContext(transactionName), memberName:transactionName)) : null);
                });
        
        private static IObservable<object> ConcurrentFailFast(this IObservable<(string Name, IObservable<object> Source)> source, string transactionName, int maxConcurrency, object[] context) 
            => source.Select(op => op.Source.PushStackFrame(op.Name)
                    .ChainFaultContext(context:context.AddToContext($"{transactionName} - {op.Name}"),null, op.Name))
                .Merge(maxConcurrency > 0 ? maxConcurrency : int.MaxValue);
        
        private static IObservable<object> ResilientBus(this StepDefinition step,
            (object results, List<Exception> failures, List<object> allResults) acc){
            var primaryBus = Observable.Defer(() => step.Selector(acc.results));
            return step.FallbackSelector == null ? primaryBus
                : primaryBus.Catch((Exception ex) => step.FallbackSelector(ex, acc.results));
        }

        private static IObservable<(object finalStepResult, List<Exception> allFailures, List<object> allResults)> StepChain<TFinal>(this List<StepDefinition> allSteps, TransactionBuilder<TFinal> builder) 
            => allSteps.Aggregate(Observable.Return((results: (object)new List<object>(), failures: new List<Exception>(), allResults: new List<object>())), (accObservable, step) =>
                    accObservable.SelectMany(acc => step.ResilientBus(acc)
                        .PushFrameConditionally(!string.IsNullOrEmpty(step.Name) ? step.Name : $"Part {allSteps.IndexOf(step) + 1}",step.FilePath,step.LineNumber)
                        .Materialize().BufferUntilCompleted()
                        .Select(notifications => {
                            var errorNotifications = notifications.Where(n => n.Kind == NotificationKind.OnError).ToList();
                            if (errorNotifications.Any()) {
                                Log(() => $"[INSTRUMENTATION][StepChain] Step '{step.Name}' failed. Captured {errorNotifications.Count} error notifications. First error type: {errorNotifications.First().Exception!.GetType().Name}");
                            }
                            var results = notifications.Where(n => n.Kind == NotificationKind.OnNext).Select(n => n.Value).ToList();
                            acc.allResults.AddRange(results);
                            acc.failures.AddRange(allSteps.CollectErrors(builder, notifications.Where(n => n.Kind == NotificationKind.OnError).ToList(), step));
                            return acc with { results = results };
                        })))
                .Select(acc => (acc.results, acc.failures, acc.allResults));
        
        private static IEnumerable<FaultHubException> CollectErrors<TFinal>(this List<StepDefinition> allSteps, TransactionBuilder<TFinal> builder, List<Notification<object>> errors, StepDefinition step) {
            if (!errors.Any()) return [];
    
            var stepNameForContext = !string.IsNullOrEmpty(step.Name) 
                ? step.Name 
                : $"Part {allSteps.IndexOf(step) + 1}";

            return errors.Select(e => {
                var stack = e.Exception.CapturedStack();
                var capturedStack = stack ?? FaultHub.LogicalStackContext.Value;
                var contextForStep = capturedStack.NewFaultContext(
                    builder.Context.AddToContext(builder.TransactionName,
                        $"{builder.TransactionName} - {stepNameForContext}"), tags: [StepNodeTag], memberName: stepNameForContext);
        
                Log(() => $"[INSTRUMENTATION][CollectErrors] Creating context for step '{stepNameForContext}'. Tags are: [{string.Join(", ", contextForStep.Tags)}]");
                return e.Exception.ExceptionToPublish(contextForStep);
            });
        }


        private static IObservable<object> RunToEnd<TFinal>(this TransactionBuilder<TFinal> builder, List<StepDefinition> allSteps, bool isNested,bool collectAllResults)
            => allSteps.StepChain(builder)
                .SelectMany(t => {
                    if (!t.allFailures.Any()) {
                        Log(() => $"[Tx:{builder.TransactionName}] RunToEnd: No failures. CollectAllResults={collectAllResults}, IsNested={isNested}");
                        return Observable.Return(collectAllResults ? t.allResults : (List<object>)t.finalStepResult);
                    }
                    var aggregateException = new AggregateException(t.allFailures);
                    var message = $"{builder.TransactionName} completed with errors";
                    var finalContext = (builder.Context ?? []).ToList();
                    finalContext.Add(builder.TransactionName);
                    var faultContext = FaultHub.LogicalStackContext.Value.NewFaultContext(finalContext.ToArray(), memberName:builder.CallerMemberName,filePath: builder.CallerMemberPath, lineNumber:builder.CallerMemberLine);
                    var faultException = new FaultHubException(message, aggregateException, faultContext);
                    if (!isNested) return Observable.Throw<object>(faultException);
                    var finalTypedResults = t.allResults.OfType<TFinal>().Cast<object>().ToList();
                    return Observable.Return((object)finalTypedResults).Concat(Observable.Throw<object>(faultException));
                });

        private static IObservable<object> FailFast<TFinal>(this  List<StepDefinition> allSteps,TransactionBuilder<TFinal> builder) 
            => allSteps.Aggregate(Observable.Return((object)null), (currentObservable, step) => currentObservable
                    .SelectMany(currentResult => {
                        var stepName = !string.IsNullOrEmpty(step.Name) ? step.Name : $"Part {allSteps.IndexOf(step) + 1}";
                        return Observable.Defer(() => step.Selector(currentResult))
                            .PushFrameConditionally(stepName,step.FilePath,step.LineNumber)
                            .Catch((Exception ex) => {
                                Log(() => $"[Tx:{builder.TransactionName}][Step:{stepName}] FailFast: Catch block executed for exception {ex.GetType().Name}.");
                                if (ex is TransactionAbortedException) return Observable.Throw<object>(ex);
                                var faultContext = FaultHub.LogicalStackContext.Value
                                    .NewFaultContext(builder.Context.AddToContext(builder.TransactionName,$"{builder.TransactionName} - {stepName}"),[StepNodeTag], memberName:stepName);
                                var newFaultHubException = ex.ExceptionToPublish(faultContext);
                                return Observable.Throw<object>(newFaultHubException);
                            });
                    }))
                .SelectMany(CreateInputArray<object>)
                .BufferUntilCompleted();

    }



}