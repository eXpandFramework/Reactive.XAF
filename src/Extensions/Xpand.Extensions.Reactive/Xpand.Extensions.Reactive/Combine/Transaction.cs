using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;
using static Xpand.Extensions.Reactive.ErrorHandling.FaultHub.FaultHubLogger;

namespace Xpand.Extensions.Reactive.Combine {
    public static partial class Combine {
        internal class StepDefinition {
            public Func<object, IObservable<object>> Selector { get; init; }
            public Func<Exception, object, IObservable<object>> FallbackSelector { get; init; }
        }

        private static readonly AsyncLocal<int> TransactionNestingLevel = new();
        public static IObservable<T> ConcurrentTransaction<T>(this IEnumerable<IObservable<T>> source,
            string transactionName, bool failFast = false, int maxConcurrency = 0, object[] context = null, IScheduler scheduler = null)
            => Observable.Defer(() => {
                    var scheduledSource = source.ToObservable(scheduler ?? Scheduler.Default);
                    return failFast ? scheduledSource.ConcurrentFailFast(transactionName, maxConcurrency, context)
                        : scheduledSource.ConcurrentRunToEnd(transactionName, maxConcurrency, context);
                })
                .ChainFaultContext(context, null, transactionName);

        private static IObservable<T> ConcurrentRunToEnd<T>(this IObservable<IObservable<T>> source,string transactionName, int maxConcurrency, object[] context) 
            => source.Select((obs, i) => obs
                    .ChainFaultContext(context.AddToContext($"{transactionName} - Op:{i + 1}"))
                    .Materialize())
                .Merge(maxConcurrency > 0 ? maxConcurrency : int.MaxValue)
                .BufferUntilCompleted()
                .SelectMany(notifications => {
                    var exceptions = notifications.Where(n => n.Kind == NotificationKind.OnError).Select(n => n.Exception).ToList();
                    var results = notifications.Where(n => n.Kind == NotificationKind.OnNext).Select(n => n.Value).ToList();
                    return exceptions.Any() ? results.ToObservable().Concat(Observable.Throw<T>(new AggregateException(exceptions))) : results.ToObservable();
                });

        private static IObservable<T> ConcurrentFailFast<T>(this IObservable<IObservable<T>> source,string transactionName, int maxConcurrency, object[] context) 
            => source.Select((obs, i) => obs.PushStackFrame(context.AddToContext($"{transactionName} - Op:{i + 1}")))
                .Merge(maxConcurrency > 0 ? maxConcurrency : int.MaxValue);

        public static IObservable<Unit> SequentialTransaction(this IEnumerable<object> source, bool failFast = false,
            Func<IObservable<object>, IObservable<object>> resiliencePolicy = null, object[] context = null,
            [CallerMemberName] string transactionName = null, IScheduler scheduler = null)
            => source.ToObjectStreams()
                .SequentialTransaction(failFast, resiliencePolicy, context, transactionName, scheduler);

        public static IObservable<Unit> SequentialTransaction<TSource>(this IEnumerable<IObservable<TSource>> source, bool failFast = false,
            Func<IObservable<TSource>, IObservable<TSource>> resiliencePolicy = null, object[] context = null,
            [CallerMemberName] string transactionName = null, IScheduler scheduler = null) {
            var transaction = source
                .Operations(resiliencePolicy, context, failFast, transactionName, scheduler: scheduler)
                .SequentialTransaction(context.AddToContext(transactionName.PrefixCallerWhenDefault()));
            return failFast
                ? transaction.Catch((Exception ex)
                    => Observable.Throw<Unit>(new InvalidOperationException($"{transactionName} failed", ex)))
                : transaction;
        }

        public static IObservable<TResult> SequentialTransaction<TSource, TResult>(
            this IEnumerable<IObservable<TSource>> source, Func<TSource[], IObservable<TResult>> resultSelector,
            Func<IObservable<TSource>, IObservable<TSource>> resiliencePolicy = null, object[] context = null,
            [CallerMemberName] string transactionName = null, IScheduler scheduler = null)
            => source.Select((obs, i) => {
                    var operation = resiliencePolicy != null ? resiliencePolicy(obs) : obs;
                    return operation.SubscribeOn(scheduler ?? Scheduler.Default)
                        .ChainFaultContext((context ?? []).AddToArray($"{transactionName} - Op:{i + 1}"))
                        .Select(item => (object)item)
                        .Catch((FaultHubException ex) => Observable.Return((object)ex));
                })
                .ToNowObservable().Concat().BufferUntilCompleted()
                .SelectMany(results => {
                    var exceptions = results.OfType<Exception>().ToList();
                    return !exceptions.Any()
                        ? Observable.Return(results.Cast<TSource>().ToArray())
                        : Observable.Throw<TSource[]>(new InvalidOperationException($"{transactionName} failed",
                            new AggregateException(exceptions)));
                })
                .SelectMany(resultSelector)
                .PushStackFrame(context.AddToContext(transactionName));

        static IObservable<Unit> SequentialTransaction(this IEnumerable<IObservable<object>> source, object[] context)
            => source.ToNowObservable().SelectManySequential(obs => obs.DefaultIfEmpty(new object()))
                .BufferUntilCompleted()
                .Select(results => results.OfType<Exception>().ToList())
                .SelectMany(allFailures => !allFailures.Any()
                    ? Unit.Default.Observe()
                    : Observable.Throw<Unit>(new AggregateException(allFailures)))
                .PushStackFrame(context);

        private static IEnumerable<IObservable<object>> Operations<TSource>(this IEnumerable<IObservable<TSource>> source,
            Func<IObservable<TSource>, IObservable<TSource>> resiliencePolicy, object[] context, bool failFast,
            string transactionName, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0, IScheduler scheduler = null)
            => source.Select((obs, i) => (resiliencePolicy?.Invoke(obs) ?? obs)
                    .SubscribeOn(scheduler ?? Scheduler.Default)
                    .ChainFaultContext(context.AddToContext($"{transactionName} - Op:{i + 1}"), null, memberName, filePath, lineNumber)
                    .Select(t => (object)t))
                .Select(operation => failFast ? operation : operation.Catch((FaultHubException ex) => Observable.Return<object>(ex)));
        
        [SuppressMessage("ReSharper", "UnusedTypeParameter")]
        public interface ITransactionBuilder<out TCurrentResult> { }

        internal class TransactionBuilder<TCurrentResult>(IObservable<object> initialStep, string transactionName, object[] context, IScheduler scheduler,
            List<StepDefinition> subsequentSteps) : ITransactionBuilder<TCurrentResult> {
            internal readonly IObservable<object> InitialStep = initialStep;
            internal readonly List<StepDefinition> SubsequentSteps = subsequentSteps;
            internal readonly string TransactionName = transactionName;
            internal readonly object[] Context = context;
            internal readonly IScheduler Scheduler = scheduler;
            internal string CallerMemberName;
            internal string CallerMemberPath;
            internal int CallerMemberLine;
            public TransactionBuilder(IObservable<object> initialStep, string transactionName, object[] context,
                IScheduler scheduler, string callerMemberName,string callerMemberPath,int callerMemberLine) : this(initialStep, transactionName, context, scheduler, new List<StepDefinition>()) {
                CallerMemberName = callerMemberName;
                CallerMemberPath=callerMemberPath;
                CallerMemberLine = callerMemberLine;
            }
        }
        public static ITransactionBuilder<TSource> BeginTransaction<TSource>(this IObservable<TSource> source, object[] context = null,
                string transactionName = null, IScheduler scheduler = null, [CallerMemberName] string memberName = "",
                [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => new TransactionBuilder<TSource>(source.BufferUntilCompleted().Select(list => (object)list), transactionName??memberName, context, scheduler, memberName, filePath, lineNumber);

        public static ITransactionBuilder<TNext> Then<TCurrent, TNext>(
            this ITransactionBuilder<TCurrent> builder, Func<TCurrent[], IObservable<TNext>> nextSelector)
            => builder.Then(nextSelector, fallbackSelector: null);

        public static ITransactionBuilder<TNext> Then<TCurrent, TNext>(
            this ITransactionBuilder<TCurrent> builder, 
            Func<TCurrent[], IObservable<TNext>> nextSelector,
            Func<Exception, TCurrent[], IObservable<TNext>> fallbackSelector) {
    
            var ib = (TransactionBuilder<TCurrent>)builder;
            TCurrent[] CreateInputArray(object currentResult) => currentResult switch {
                null => [],
                IEnumerable<TCurrent> collection => collection.ToArray(),
                _ => [(TCurrent)currentResult]
            };
            var step = new StepDefinition {
                Selector = currentResult => {
                    // MODIFICATION: Added deep logging to trace the input and execution path.
                    Log(() => $"[Tx.Then.DEBUG] Selector invoked. Received currentResult of type: '{currentResult?.GetType().FullName ?? "null"}' with value: '{currentResult}'");
                    var inputArray = CreateInputArray(currentResult);
                    Log(() => $"[Tx.Then.DEBUG] CreateInputArray produced an array with {inputArray.Length} items.");
                    Log(() => "[Tx.Then.DEBUG] Proceeding to call the primary nextSelector.");
                    return nextSelector(inputArray).Select(res => (object)res);
                },
                FallbackSelector = fallbackSelector == null ? null 
                    : (ex, currentResult) => {
                        // MODIFICATION: Added logging to the fallback path.
                        Log(() => $"[Tx.Then.DEBUG] FallbackSelector invoked due to error: {ex.GetType().Name}. Received currentResult of type: {currentResult?.GetType().Name ?? "null"}");
                        var inputArray = CreateInputArray(currentResult);
                        Log(() => $"[Tx.Then.DEBUG] CreateInputArray in fallback produced an array with {inputArray.Length} items.");
                        Log(() => "[Tx.Then.DEBUG] Proceeding to call the fallbackSelector.");
                        return fallbackSelector(ex, inputArray).Select(res => (object)res);
                    }
            };

            return new TransactionBuilder<TNext>(ib.InitialStep, ib.TransactionName, ib.Context, ib.Scheduler,
                    [..ib.SubsequentSteps, step]) 
                { CallerMemberName = ib.CallerMemberName, CallerMemberPath = ib.CallerMemberPath, CallerMemberLine = ib.CallerMemberLine };

        }
        public static IObservable<TFinal[]> RunFailFast<TFinal>(this ITransactionBuilder<TFinal> builder)
            => builder.Run();

        public static IObservable<TFinal[]> RunToEnd<TFinal>(this ITransactionBuilder<TFinal> builder)
            => builder.Run(false);

        private static IObservable<TFinal[]>
            Run<TFinal>(this ITransactionBuilder<TFinal> builder, bool failFast = true) {
            return Observable.Defer(() => {
                var ib = (TransactionBuilder<TFinal>)builder;
                Log(() => $"[Tx.DEBUG][Run] Called for '{ib.TransactionName}'.");

                var isNested = TransactionNestingLevel.Value > 0;
                TransactionNestingLevel.Value++;
                Log(()
                    => $"[Tx.DEBUG][Run] isNested: {isNested}. Nesting level incremented to: {TransactionNestingLevel.Value}");

                var allSteps = new List<StepDefinition> { new() { Selector = _ => ib.InitialStep } };
                allSteps.AddRange(ib.SubsequentSteps);

                var transactionLogic = failFast ? ib.FailFast(allSteps) : ib.RunToCompletion(allSteps, isNested);
                Log(() => $"[Tx.DEBUG][Run] Runner selected: {(failFast ? "FailFast" : "RunToCompletion")}");

                var finalLogic = transactionLogic.Select(result => {
                    Log(() => "[Tx.DEBUG][Run] Casting final result to array.");
                    return ((IEnumerable<object>)result).Cast<TFinal>().ToArray();
                });

                var scheduledTransaction = ib.Scheduler == null ? finalLogic : finalLogic.SubscribeOn(ib.Scheduler);

                var finalContext = ib.Context.AddToContext(ib.TransactionName);

                var tracedScheduledTransaction = scheduledTransaction.Do(
                    val => Log(()
                        => $"[Tx.DEBUG][Run] <<-- NOTIFICATION TO CHAINFAULTCONTEXT: OnNext. Item count: {val?.Length ?? 0}"),
                    err => Log(()
                        => $"[Tx.DEBUG][Run] <<-- NOTIFICATION TO CHAINFAULTCONTEXT: OnError: {err.GetType().Name} - {err.Message}"),
                    () => Log(() => "[Tx.DEBUG][Run] <<-- NOTIFICATION TO CHAINFAULTCONTEXT: OnCompleted.")
                );

                return tracedScheduledTransaction
                    .ChainFaultContext(finalContext, null, ib.CallerMemberName, ib.CallerMemberPath,
                        ib.CallerMemberLine)
                    .Finally(() => {
                        TransactionNestingLevel.Value--;
                        Log(()
                            => $"[Tx.DEBUG][Run] Finally block. Nesting level decremented to: {TransactionNestingLevel.Value}");
                    });
            });
        }


        private static IObservable<object> RunToCompletion<TFinal>(this TransactionBuilder<TFinal> builder,
            List<StepDefinition> allSteps, bool isNested) {
            Log(() => $"[Tx.DEBUG][RunToCompletion] Entered for '{builder.TransactionName}'. isNested: {isNested}");
            var allFailures = new List<Exception>();
            var allResults = new List<object>();
            var stepChain = Observable.Return((object)null);

            for (var i = 0; i < allSteps.Count; i++) {
                var step = allSteps[i];
                var partNumber = i + 1;
                stepChain = stepChain.SelectMany(currentResult => {
// MODIFICATION: Added log to show the input for the current step.
                    Log(()
                        => $"[Tx.DEBUG][RunToCompletion] Executing Part {partNumber} of '{builder.TransactionName}'. Input is: {currentResult?.GetType().Name ?? "null"}");
                    var primaryObservable = Observable.Defer(() => step.Selector(currentResult));

                    var resilientObservable = step.FallbackSelector != null
                        ? primaryObservable.Catch((Exception ex) => step.FallbackSelector(ex, currentResult))
                        : primaryObservable;

                    return resilientObservable
                        .PushStackFrame(builder.Context.AddToContext($"{builder.TransactionName} - Part {partNumber}",
                            builder.CallerMemberName, builder.CallerMemberPath, builder.CallerMemberLine))
                        .Materialize()
                        .BufferUntilCompleted()
                        .SelectMany(notifications => {
// MODIFICATION: Added extensive logging to trace the processing of buffered notifications.
                            Log(()
                                => $"[Tx.DEBUG][RunToCompletion] Part {partNumber} - Buffered notifications received. Total: {notifications.Length}.");
                            var errors = notifications.Where(n => n.Kind == NotificationKind.OnError).ToList();
                            var results = notifications.Where(n => n.Kind == NotificationKind.OnNext).ToList();
                            Log(()
                                => $"[Tx.DEBUG][RunToCompletion] Part {partNumber} - Parsed notifications. Results: {results.Count}, Errors: {errors.Count}.");

                            if (errors.Any()) {
                                Log(()
                                    => $"[Tx.DEBUG][RunToCompletion] Part {partNumber} - Processing {errors.Count} error(s).");
                                allFailures.AddRange(errors.Select(e
                                    => e.Exception.ExceptionToPublish(
                                        FaultHub.LogicalStackContext.Value.NewFaultContext([]))));
                            }

                            if (results.Any()) {
                                Log(()
                                    => $"[Tx.DEBUG][RunToCompletion] Part {partNumber} - Processing {results.Count} result(s). Adding to allResults. Propagating last value: '{results.Last().Value}'.");
                                allResults.AddRange(results.Select(r => r.Value));
                                return Observable.Return(results.Last().Value);
                            }

                            Log(()
                                => $"[Tx.DEBUG][RunToCompletion] Part {partNumber} - No results to propagate. Returning null to the chain.");
                            return Observable.Return((object)null);
                        });
                });
            }

            return stepChain
                .Where(result => result != null)
                .Do(
                    res => Log(() => $"[Tx.DEBUG][RunToCompletion] Item passed 'Where' and is entering ToList: {res}"),
                    ex => Log(() => $"[Tx.DEBUG][RunToCompletion] ToList source FAILED with: {ex.Message}"),
                    () => Log(() => "[Tx.DEBUG][RunToCompletion] ToList source COMPLETED."))
                .ToList()
                .SelectMany(results => {
                    Log(()
                        => $"[Tx.DEBUG][RunToCompletion] Final SelectMany. 'allResults' count: {allResults.Count}. 'results' from ToList count: {results.Count}. 'allFailures': {allFailures.Count}");
                    if (allFailures.Any()) {
                        var aggregateException = new AggregateException(allFailures);
                        if (isNested) {
                            var finalTypedResults = allResults.OfType<TFinal>().Cast<object>().ToList();
                            Log(()
                                => $"[Tx.DEBUG][RunToCompletion] Nested failure. RETURNING: Observable.Return(list with {finalTypedResults.Count} items) THEN Observable.Throw.");
                            return Observable.Return((object)finalTypedResults)
                                .Concat(Observable.Throw<object>(aggregateException));
                        }

                        Log(() => "[Tx.DEBUG][RunToCompletion] Top-level failure. RETURNING: Observable.Throw.");
                        return Observable.Throw<object>(aggregateException);
                    }

                    Log(()
                        => $"[Tx.DEBUG][RunToCompletion] Success. RETURNING: Observable.Return(results from ToList with {results.Count} items).");
                    return Observable.Return((object)results);
                });
        }
private static IObservable<object> FailFast<TFinal>(this TransactionBuilder<TFinal> builder, List<StepDefinition> allSteps) 
            => allSteps.Select((step, i) => new { Step = step, PartNumber = i + 1 })
                .Aggregate(Observable.Return((object)null), (currentObservable, stepInfo) => 
                    currentObservable.SelectMany(currentResult => 
                        Observable.Defer(() => stepInfo.Step.Selector(currentResult))
                            .PushStackFrame(builder.Context.AddToContext($"{builder.TransactionName} - Part {stepInfo.PartNumber}"))))
                .BufferUntilCompleted()
                .Select(list => (object)list);
    }
}