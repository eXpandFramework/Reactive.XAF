using System;
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
using Xpand.Extensions.Reactive.Utility;
using static Xpand.Extensions.Reactive.ErrorHandling.FaultHub.FaultHubLogger;

namespace Xpand.Extensions.Reactive.Combine {
    public static partial class Combine {
        private static readonly AsyncLocal<int> TransactionNestingLevel = new();
        public interface IBatchTransactionBuilder {
            internal List<INamedStream> Operations { get; }
        }

        internal class BatchTransactionBuilder : IBatchTransactionBuilder {
            public BatchTransactionBuilder(params INamedStream[] namedStreams) 
                => Operations.AddRange(namedStreams);
            
            public List<INamedStream> Operations { get; } = new();
        }
        public static IBatchTransactionBuilder BeginBatchTransaction<T>(this IObservable<T> source, [CallerArgumentExpression("source")] string sourceName = null)
            => new BatchTransactionBuilder(new NamedStream<object> { Name = GetStepName(sourceName), Source = source.Select(o => (object)o) });

        public static IBatchTransactionBuilder Add<T>(this IBatchTransactionBuilder builder, IObservable<T> step,
            [CallerArgumentExpression("step")] string stepName = null) {
            builder.Operations.Add(new NamedStream<object> { Name = GetStepName(stepName), Source = step.Select(o => (object)o) });
            return builder;
        }
        
        public static IObservable<Unit> SequentialTransaction(this IBatchTransactionBuilder builder, bool failFast = false,
            Func<IObservable<object>, IObservable<object>> resiliencePolicy = null, object[] context = null,
            [CallerMemberName] string transactionName = null, IScheduler scheduler = null)
            => builder.Operations.SequentialTransaction(failFast, resiliencePolicy, context, transactionName, scheduler);
        public static IObservable<Unit> SequentialRunToEndTransaction(this IBatchTransactionBuilder builder, 
            Func<IObservable<object>, IObservable<object>> resiliencePolicy = null, object[] context = null,
            [CallerMemberName] string transactionName = null, IScheduler scheduler = null)
            => builder.SequentialTransaction(false,resiliencePolicy,context,transactionName,scheduler);
        public static IObservable<Unit> SequentialFailFastTransaction(this IBatchTransactionBuilder builder, 
            Func<IObservable<object>, IObservable<object>> resiliencePolicy = null, object[] context = null,
            [CallerMemberName] string transactionName = null, IScheduler scheduler = null)
            => builder.SequentialTransaction(true,resiliencePolicy,context,transactionName,scheduler);

        private class StepAction<TIn, TOut> {
            public Func<TIn[], IObservable<TOut>> Selector { get; init; }
            public Func<Exception, TIn[], IObservable<TOut>> FallbackSelector { get; init; }
            public string SelectorExpression { get; init; }
            public string FallbackSelectorExpression { get; init; }
            public string StepName { get; init; }
        }
        
        public static IBatchTransactionBuilder BeginBatchTransaction<T>(this IEnumerable<IObservable<T>> source, [CallerArgumentExpression("source")] string sourceExpression = null) {
            var builder = new BatchTransactionBuilder();
            var sourceList = source.ToList();
            for (int i = 0; i < sourceList.Count; i++) {
                var stepName = $"{sourceExpression}[{i}]";
                builder.Add(sourceList[i], stepName);
            }
            return builder;
        }
        public static IObservable<Unit> SequentialTransaction<TSource>(this IEnumerable<NamedStream<TSource>> source, bool failFast = false,
            Func<IObservable<object>, IObservable<object>> resiliencePolicy = null, object[] context = null,
            [CallerMemberName] string transactionName = null, IScheduler scheduler = null) {

            
            var namedStreams = source.Select(s =>
                new NamedStream<object> { Name = s.Name, Source = s.Source.Select(o => (object)o) });
    
            var operations = namedStreams.Operations(resiliencePolicy, context, failFast, transactionName, scheduler: scheduler);
            var transaction = operations.SequentialTransaction(context.AddToContext(transactionName.PrefixCallerWhenDefault()));
    
            return failFast
                ? transaction.Catch((Exception ex)
                    => {
                    var faultException = ex as FaultHubException ?? ex.ExceptionToPublish(
                        FaultHub.LogicalStackContext.Value.NewFaultContext(context, transactionName));
                    return Observable.Throw<Unit>(new TransactionAbortedException($"{transactionName} failed", faultException));
                })
                : transaction;
        }

        
        
        
        [Obsolete("This overload produces generic 'Op:X' diagnostics. Please use the .WithName() extension method on each observable and call the SequentialTransaction overload that accepts an IEnumerable<NamedStream> for improved diagnostics.", false)]
        public static IObservable<T> ConcurrentTransaction<T>(this IEnumerable<IObservable<T>> source,
            string transactionName, bool failFast = false, int maxConcurrency = 0, object[] context = null, IScheduler scheduler = null)
            => Observable.Defer(() => {
                    var scheduledSource = source.ToObservable(scheduler ?? Scheduler.Default);
                    return failFast ? scheduledSource.ConcurrentFailFast(transactionName, maxConcurrency, context)
                        : scheduledSource.ConcurrentRunToEnd(transactionName, maxConcurrency, context);
                })
                .ChainFaultContext(context, null, transactionName);

        public static IObservable<TSource> ConcurrentTransaction<TSource>(this IEnumerable<NamedStream<TSource>> source,
            string transactionName, bool failFast = false, int maxConcurrency = 0, object[] context = null, IScheduler scheduler = null) {
            var objectStreams = source.Select(s =>
                new NamedStream<object> { Name = s.Name, Source = s.Source.Select(o => (object)o) });

            return objectStreams.ConcurrentTransaction(transactionName, failFast, maxConcurrency, context, scheduler)
                .Cast<TSource>();
        }
        public static IObservable<object> ConcurrentTransaction(
            this IBatchTransactionBuilder builder,
            string transactionName,
            bool failFast = false,
            int maxConcurrency = 0,
            object[] context = null,
            IScheduler scheduler = null)
        {
            Log(() => $"[Tx.DEBUG][Concurrent.Builder] ENTERING for transaction '{transactionName}'. failFast={failFast}");
            var batchBuilder = (BatchTransactionBuilder)builder;
            var operations = batchBuilder.Operations.Select(op =>
                new NamedStream<object> { Name = op.Name, Source = op.Source });

            var transaction = operations.ConcurrentTransaction(transactionName, failFast, maxConcurrency, context, scheduler);

            return failFast
                ? transaction.Catch((Exception ex) => {
                    Log(() => $"[Tx.DEBUG][Concurrent.Builder] FAILFAST CATCH. Caught: '{ex.GetType().Name}'. Wrapping in InvalidOperationException.");
                    var faultException = ex as FaultHubException ?? ex.ExceptionToPublish(
                        FaultHub.LogicalStackContext.Value.NewFaultContext(context, transactionName));
                    return Observable.Throw<object>(new TransactionAbortedException($"{transactionName} failed", faultException));
                })
                : transaction;
        }
        public static IObservable<object> ConcurrentTransaction(this IEnumerable<NamedStream<object>> source,
            string transactionName, bool failFast = false, int maxConcurrency = 0, object[] context = null, IScheduler scheduler = null)
            => Observable.Defer(() => {
                    var scheduledSource = source.ToObservable(scheduler ?? Scheduler.Default);
                    return failFast ? scheduledSource.ConcurrentFailFast(transactionName, maxConcurrency, context)
                        : scheduledSource.ConcurrentRunToEnd(transactionName, maxConcurrency, context);
                })
                .ChainFaultContext(context, null, transactionName);
        private static IObservable<T> ConcurrentFailFast<T>(this IObservable<NamedStream<T>> source, string transactionName, int maxConcurrency, object[] context) 
            => source.Select(op => op.Source.ChainFaultContext(context.AddToContext($"{transactionName} - {op.Name}")))
                .Merge(maxConcurrency > 0 ? maxConcurrency : int.MaxValue);

        private static IObservable<T> ConcurrentRunToEnd<T>(this IObservable<NamedStream<T>> source, string transactionName, int maxConcurrency, object[] context) 
            => source.Select(op => op.Source
                    .ChainFaultContext(context.AddToContext($"{transactionName} - {op.Name}"))
                    .Materialize())
                .Merge(maxConcurrency > 0 ? maxConcurrency : int.MaxValue)
                .BufferUntilCompleted()
                .SelectMany(notifications => {
                    var exceptions = notifications.Where(n => n.Kind == NotificationKind.OnError).Select(n => n.Exception).ToList();
                    var results = notifications.Where(n => n.Kind == NotificationKind.OnNext).Select(n => n.Value).ToList();
                    return exceptions.Any() ? results.ToObservable().Concat(Observable.Throw<T>(new AggregateException(exceptions))) : results.ToObservable();
                });
        
        [Obsolete("This overload produces generic 'Op:X' diagnostics. Please use the .WithName() extension method on each observable and call the SequentialTransaction overload that accepts an IEnumerable<NamedStream> for improved diagnostics.", false)]
        private static IObservable<T> ConcurrentRunToEnd<T>(this IObservable<IObservable<T>> source,string transactionName, int maxConcurrency, object[] context) 
            => source.Select((obs, i) => obs.ChainFaultContext(context.AddToContext($"{transactionName} - Op:{i + 1}")).Materialize())
                .Merge(maxConcurrency > 0 ? maxConcurrency : int.MaxValue)
                .BufferUntilCompleted()
                .SelectMany(notifications => {
                    var exceptions = notifications.Where(n => n.Kind == NotificationKind.OnError).Select(n => n.Exception).ToList();
                    var results = notifications.Where(n => n.Kind == NotificationKind.OnNext).Select(n => n.Value).ToList();
                    return exceptions.Any() ? results.ToObservable().Concat(Observable.Throw<T>(new AggregateException(exceptions))) : results.ToObservable();
                });


        private static IObservable<T> ConcurrentFailFast<T>(this IObservable<IObservable<T>> source,string transactionName, int maxConcurrency, object[] context) 
            => source.Select((obs, i) => obs.ChainFaultContext(context.AddToContext($"{transactionName} - Op:{i + 1}")))
                .Merge(maxConcurrency > 0 ? maxConcurrency : int.MaxValue);

        static IObservable<Unit> SequentialTransaction(this IEnumerable<INamedStream> source, bool failFast = false,
            Func<IObservable<object>, IObservable<object>> resiliencePolicy = null, object[] context = null,
            [CallerMemberName] string transactionName = null, IScheduler scheduler = null) {
            var namedStreams = source.Select(s => new NamedStream<object> { Name = s.Name, Source = s.Source }).ToList();
    
            return namedStreams.SequentialTransaction(failFast, resiliencePolicy, context, transactionName, scheduler);
        }
        public static IObservable<Unit> SequentialTransaction(params INamedStream[] operations) 
            => operations.SequentialTransaction();

        public static IObservable<Unit> SequentialTransaction(this IEnumerable<NamedStream<object>> source, bool failFast = false,
            Func<IObservable<object>, IObservable<object>> resiliencePolicy = null, object[] context = null,
            [CallerMemberName] string transactionName = null, IScheduler scheduler = null) {
            var transaction = source.Operations(resiliencePolicy, context, failFast, transactionName, scheduler: scheduler)
                .SequentialTransaction(context.AddToContext(transactionName.PrefixCallerWhenDefault()));
            return failFast ? transaction.Catch((Exception ex)
                    => {
                var faultException = ex as FaultHubException ?? ex.ExceptionToPublish(
                    FaultHub.LogicalStackContext.Value.NewFaultContext(context, transactionName));
                return Observable.Throw<Unit>(new TransactionAbortedException($"{transactionName} failed", faultException));
            }) : transaction;
        }


        public static IObservable<TResult> SequentialTransaction<TSource, TResult>(
            this IEnumerable<NamedStream<TSource>> source, Func<TSource[], IObservable<TResult>> resultSelector,
            Func<IObservable<TSource>, IObservable<TSource>> resiliencePolicy = null, object[] context = null,
            [CallerMemberName] string transactionName = null, IScheduler scheduler = null)
            => source.Select(op => {
                    var operation = resiliencePolicy != null ? resiliencePolicy(op.Source) : op.Source;
                    return operation
                        .SubscribeOn(scheduler ?? Scheduler.Default)
                        .ChainFaultContext((context ?? []).AddToArray($"{transactionName} - {op.Name}"))
                        .Select(item => (object)item)
                        .Catch((FaultHubException ex) => Observable.Return((object)ex));
                })
                .ToNowObservable().Concat().BufferUntilCompleted()
                .SelectMany(results => {
                    var exceptions = results.OfType<Exception>().ToList();
                    var successfulResults = results.OfType<TSource>().ToArray();
                    return !exceptions.Any()
                        ? Observable.Return(successfulResults)
                        : Observable.Throw<TSource[]>(new FaultHubException($"{transactionName} failed",
                            new AggregateException(exceptions),
                            FaultHub.LogicalStackContext.Value.NewFaultContext(context, transactionName)));
                })
                .SelectMany(resultSelector)
                .PushStackFrame(context.AddToContext(transactionName));
        
        static IObservable<Unit> SequentialTransaction(this IEnumerable<IObservable<object>> source, object[] context)
            => source.ToNowObservable().SelectManySequential(obs => obs.DefaultIfEmpty(new object()))
                .BufferUntilCompleted()
                .Select(results => results.OfType<Exception>().ToList())
                .SelectMany(allFailures => {
                    if (!allFailures.Any()) {
                        return Unit.Default.Observe();
                    }

                    var txName = context?.OfType<string>().FirstOrDefault() ?? "Transaction";
                    return Observable.Throw<Unit>(new FaultHubException($"{txName} completed with errors",
                        new AggregateException(allFailures),
                        FaultHub.LogicalStackContext.Value.NewFaultContext(context)));
                })
                .PushStackFrame(context);

        private static IEnumerable<IObservable<object>> Operations<TSource>(this IEnumerable<NamedStream<TSource>> source,
            Func<IObservable<object>, IObservable<object>> resiliencePolicy, object[] context, bool failFast,
            string transactionName, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0, IScheduler scheduler = null)
            => source.Select((op, _) => {
                    var objectSource = op.Source.Select(o => (object)o);
                    var operation = resiliencePolicy?.Invoke(objectSource) ?? objectSource;
                    return operation
                        .SubscribeOn(scheduler ?? Scheduler.Default)
                        .ChainFaultContext(context.AddToContext($"{transactionName} - {op.Name}"), null, memberName, filePath, lineNumber);
                })
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
            internal string InitialStepName; 
            public TransactionBuilder(IObservable<object> initialStep, string transactionName, object[] context,
                IScheduler scheduler, string callerMemberName,string callerMemberPath,int callerMemberLine) : this(initialStep, transactionName, context, scheduler, new List<StepDefinition>()) {
                Log(() => $"[Tx.DEBUG][BUILDER] Constructor called for transaction: '{transactionName}'.");
                CallerMemberName = callerMemberName;
                CallerMemberPath=callerMemberPath;
                CallerMemberLine = callerMemberLine;
            }
        }
        public static ITransactionBuilder<TSource> BeginWorkflow<TSource>(this IObservable<TSource> source, object[] context = null,
                string transactionName = null, IScheduler scheduler = null, [CallerMemberName] string memberName = "",
                [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0,[CallerArgumentExpression(nameof(source))] string sourceExpression = null)
            => new TransactionBuilder<TSource>(source.BufferUntilCompleted().Select(list => (object)list), transactionName??memberName, context, scheduler, memberName, filePath, lineNumber) {
                InitialStepName = GetStepName(sourceExpression)
            };
        public static ITransactionBuilder<TSource> BeginWorkflow<TSource>(this IObservable<TSource> source, 
                string transactionName ,object[] context = null, IScheduler scheduler = null, [CallerMemberName] string memberName = "",
                [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0,[CallerArgumentExpression(nameof(source))] string sourceExpression = null) {
            var stepName = GetStepName(sourceExpression);
            Log(() => $"[Tx.DEBUG][BeginWorkflow] Captured sourceExpression: '{sourceExpression}'. Parsed InitialStepName: '{stepName}'.");
            return new TransactionBuilder<TSource>(source.BufferUntilCompleted().Select(list => (object)list), transactionName??memberName, context, scheduler, memberName, filePath, lineNumber) {
                InitialStepName = stepName
            };
        }
        
        public static ITransactionBuilder<TNext> Then<TCurrent, TNext>(this ITransactionBuilder<TCurrent> builder, Func<TCurrent[], IObservable<TNext>> selector,
            string stepName = null,
            Func<Exception, TCurrent[], IObservable<TNext>> fallbackSelector = null, [CallerArgumentExpression(nameof(selector))] string selectorExpression = null,
            [CallerArgumentExpression(nameof(fallbackSelector))] string fallbackSelectorExpression = null) 
            => builder.Then(new StepAction<TCurrent, TNext> {
                Selector = selector, FallbackSelector = fallbackSelector, SelectorExpression = selectorExpression, FallbackSelectorExpression = fallbackSelectorExpression, StepName = stepName
            });

        private static ITransactionBuilder<TNext> Then<TCurrent, TNext>(this ITransactionBuilder<TCurrent> builder, StepAction<TCurrent, TNext> stepAction) {
            var ib = (TransactionBuilder<TCurrent>)builder;
            return new TransactionBuilder<TNext>(ib.InitialStep, ib.TransactionName, ib.Context, ib.Scheduler, [..ib.SubsequentSteps, stepAction.StepDefinition()]) 
                { CallerMemberName = ib.CallerMemberName, CallerMemberPath = ib.CallerMemberPath, CallerMemberLine = ib.CallerMemberLine,InitialStepName = ib.InitialStepName};
        }

        private static StepDefinition StepDefinition<TCurrent, TNext>(this StepAction<TCurrent, TNext> stepAction){
            var step = new StepDefinition {
                Name = GetStepName(stepAction.SelectorExpression, stepAction.StepName, stepAction.Selector),
                Selector = currentResult => stepAction.Selector(CreateInputArray<TCurrent>(currentResult)).Select(res => (object)res)
            };
            step.FallbackSelector = stepAction.FallbackSelector == null ? null : (ex, currentResult) => {
                var fallbackName = GetStepName(stepAction.FallbackSelectorExpression, null, stepAction.FallbackSelector);
                step.Name = $"{fallbackName} (Fallback)";
                return stepAction.FallbackSelector(ex, CreateInputArray<TCurrent>(currentResult)).Select(res => (object)res);
            };
            return step;
        }
        
        static TCurrent[] CreateInputArray<TCurrent>(object currentResult) => currentResult switch {
            null => [], IEnumerable<TCurrent> collection => collection.ToArray(),
            _ => [(TCurrent)currentResult]
        };

        public static IObservable<TFinal[]> RunFailFast<TFinal>(this ITransactionBuilder<TFinal> builder)
            => builder.Run();

        public static IObservable<TFinal[]> RunToEnd<TFinal>(this ITransactionBuilder<TFinal> builder)
            => builder.Run(false);

        public static IObservable<TFinal[]> Run<TFinal>(this ITransactionBuilder<TFinal> builder, bool failFast = true) 
            => Observable.Defer(() => {
                var ib = (TransactionBuilder<TFinal>)builder;
                Log(() => $"[Tx.DEBUG][Run] Called for '{ib.TransactionName}'. Checking InitialStepName: '{ib.InitialStepName ?? "NULL"}'.");
                var isNested = TransactionNestingLevel.Value > 0;
                TransactionNestingLevel.Value++;
                Log(() => $"[Tx.DEBUG][Run] isNested: {isNested}. Nesting level incremented to: {TransactionNestingLevel.Value}");
                var allSteps = new List<StepDefinition> { new() { Selector = _ => ib.InitialStep, Name = ib.InitialStepName } };
                allSteps.AddRange(ib.SubsequentSteps);
                var transactionLogic = failFast ? ib.FailFast(allSteps) : ib.RunToEnd(allSteps, isNested);
                Log(() => $"[Tx.DEBUG][Run] Runner selected: {(failFast ? "FailFast" : "RunToCompletion")}");
                var finalLogic = transactionLogic.Select(result => {
                    Log(() => "[Tx.DEBUG][Run] Casting final result to array.");
                    return ((IEnumerable<object>)result).Cast<TFinal>().ToArray();
                });
                return (ib.Scheduler == null ? finalLogic : finalLogic.SubscribeOn(ib.Scheduler))
                    .Do(
                        val => Log(() => $"[Tx.DEBUG][Run] <<-- NOTIFICATION TO CHAINFAULTCONTEXT: OnNext. Item count: {val?.Length ?? 0}"),
                        err => Log(() => $"[Tx.DEBUG][Run] <<-- NOTIFICATION TO CHAINFAULTCONTEXT: OnError: {err.GetType().Name} - {err.Message}"),
                        () => Log(() => "[Tx.DEBUG][Run] <<-- NOTIFICATION TO CHAINFAULTCONTEXT: OnCompleted.")
                    )
                    .ChainFaultContext(ib.Context.AddToContext(ib.TransactionName), null, ib.CallerMemberName, ib.CallerMemberPath, ib.CallerMemberLine)
                    .Finally(() => {
                        TransactionNestingLevel.Value--;
                        Log(() => $"[Tx.DEBUG][Run] Finally block. Nesting level decremented to: {TransactionNestingLevel.Value}");
                    });
            });
        
        private static IObservable<object> RunToEnd<TFinal>(this TransactionBuilder<TFinal> builder,
            List<StepDefinition> allSteps, bool isNested) {
            Log(() => $"[Tx.DEBUG][RunToCompletion] Entered for '{builder.TransactionName}'. isNested: {isNested}");
            var allFailures = new List<Exception>();
            var allResults = new List<object>();
            var stepChain = Observable.Return((object)null);
            for (var i = 0; i < allSteps.Count; i++) {
                var step = allSteps[i];
                var partNumber = i + 1;
                var stepName = !string.IsNullOrEmpty(step.Name) ? step.Name : $"Part {partNumber}";
                stepChain = stepChain.SelectMany(currentResult => {
                    Log(() => $"[Tx.DEBUG][RunToCompletion] Executing Part {partNumber} of '{builder.TransactionName}'. Input is: {currentResult?.GetType().Name ?? "null"}");
                    var primaryObservable = Observable.Defer(() => step.Selector(currentResult));
                    var resilientObservable = step.FallbackSelector == null ? primaryObservable
                        : primaryObservable.Catch((Exception ex) => step.FallbackSelector(ex, currentResult));
                    return resilientObservable
                        .PushStackFrame()
                        .ChainFaultContext(builder.Context.AddToContext($"{builder.TransactionName} - {stepName}",builder.CallerMemberName, builder.CallerMemberPath, builder.CallerMemberLine))
                        .Materialize()
                        .BufferUntilCompleted()
                        .SelectMany(notifications => {
                            Log(() => $"[Tx.DEBUG][RunToCompletion] Part {partNumber} - Buffered notifications received. Total: {notifications.Length}.");
                            var errors = notifications.Where(n => n.Kind == NotificationKind.OnError).ToList();
                            var results = notifications.Where(n => n.Kind == NotificationKind.OnNext).ToList();
                            Log(() => $"[Tx.DEBUG][RunToCompletion] Part {partNumber} - Parsed notifications. Results: {results.Count}, Errors: {errors.Count}.");
                            if (errors.Any()) {
                                Log(() => $"[Tx.DEBUG][RunToCompletion] Part {partNumber} - Processing {errors.Count} error(s).");
                                allFailures.AddRange(errors.Select(e => e.Exception.ExceptionToPublish(FaultHub.LogicalStackContext.Value.NewFaultContext([]))));
                            }

                            if (results.Any()) {
                                Log(() => $"[Tx.DEBUG][RunToCompletion] Part {partNumber} - Processing {results.Count} result(s). Adding to allResults. Propagating last value: '{results.Last().Value}'.");
                                allResults.AddRange(results.Select(r => r.Value));
                                return Observable.Return(results.Last().Value);
                            }

                            Log(() => $"[Tx.DEBUG][RunToCompletion] Part {partNumber} - No results to propagate. Returning null to the chain.");
                            return Observable.Return((object)null);
                        });
                });
            }

            return stepChain.Where(result => result != null)
                .Do(
                    res => Log(() => $"[Tx.DEBUG][RunToCompletion] Item passed 'Where' and is entering ToList: {res}"),
                    ex => Log(() => $"[Tx.DEBUG][RunToCompletion] ToList source FAILED with: {ex.Message}"),
                    () => Log(() => "[Tx.DEBUG][RunToCompletion] ToList source COMPLETED."))
                .BufferUntilCompleted()
                .SelectMany(results => {
                    Log(() => $"[Tx.DEBUG][RunToCompletion] Final SelectMany. 'allResults' count: {allResults.Count}. 'results' from ToList count: {results.Length}. 'allFailures': {allFailures.Count}");
                    if (allFailures.Any()) {
                        var aggregateException = new AggregateException(allFailures);
                        if (isNested) {
                            var finalTypedResults = allResults.OfType<TFinal>().Cast<object>().ToList();
                            Log(() => $"[Tx.DEBUG][RunToCompletion] Nested failure. RETURNING: Observable.Return(list with {finalTypedResults.Count} items) THEN Observable.Throw.");
                            return Observable.Return((object)finalTypedResults).Concat(Observable.Throw<object>(aggregateException));
                        }

                        Log(() => "[Tx.DEBUG][RunToCompletion] Top-level failure. RETURNING: Observable.Throw.");
                        return Observable.Throw<object>(aggregateException);
                    }

                    Log(() => $"[Tx.DEBUG][RunToCompletion] Success. RETURNING: Observable.Return(results from ToList with {results.Length} items).");
                    return Observable.Return((object)results);
                });
        }
        private static string GetStepName(string expression, string explicitName = null, Delegate selector = null) 
            => !string.IsNullOrEmpty(explicitName) ? explicitName : string.IsNullOrEmpty(expression) ? selector?.Method.Name : expression;

        private static IObservable<object> FailFast<TFinal>(this TransactionBuilder<TFinal> builder, List<StepDefinition> allSteps) 
            => allSteps.Select((step, i) => new { Step = step, PartNumber = i + 1 })
                .Aggregate(Observable.Return((object)null), (currentObservable, stepInfo) => currentObservable
                    .SelectMany(currentResult => Observable.Defer(() => stepInfo.Step.Selector(currentResult))
                        .PushStackFrame()
                        .ChainFaultContext(builder.Context.AddToContext($"{builder.TransactionName} - {stepInfo.Step.Name ?? $"Part {stepInfo.PartNumber}"}"))))
                .BufferUntilCompleted()
                .Select(list => (object)list);
    }
    
    internal class StepDefinition {
        public Func<object, IObservable<object>> Selector { get; set; }
        public Func<Exception, object, IObservable<object>> FallbackSelector { get; set; }
        public string Name { get; set; }
    }
    
    public sealed class TransactionAbortedException(string message, Exception innerException, AmbientFaultContext context)
        : FaultHubException(message, innerException, context) {
        public TransactionAbortedException(string message, FaultHubException innerException)
            : this(message, innerException, innerException.Context) { }
    };}