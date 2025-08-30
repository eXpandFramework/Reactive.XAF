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
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.TypeExtensions;
using static Xpand.Extensions.Reactive.ErrorHandling.FaultHub.FaultHubLogger;

namespace Xpand.Extensions.Reactive.Combine {
    public enum TransactionMode {
        Sequential,
        Concurrent
    }
    public static partial class Combine {
        private static readonly AsyncLocal<int> TransactionNestingLevel = new();
        public const string TransactionNodeTag = "Transaction";
        public const string StepNodeTag = "Step";
        public static ITransactionBuilder<TSource> BeginWorkflow<TSource>(this IObservable<TSource> source,
            string transactionName, object[] context = null, IScheduler scheduler = null, [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerArgumentExpression(nameof(source))] string sourceExpression = null)
            => new TransactionBuilder<TSource>(source.BufferUntilCompleted()
                .Select(list => (object)list), transactionName ?? memberName, context, scheduler, memberName, filePath, lineNumber,[TransactionNodeTag,nameof(TransactionMode.Sequential)]) {
                InitialStepName = GetStepName(sourceExpression)
            };

        public static ITransactionBuilder<object> BeginWorkflow<TSource>(this IEnumerable<IObservable<TSource>> sources, string transactionName, TransactionMode mode = TransactionMode.Sequential,
            object[] context = null, IScheduler scheduler = null, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0,
            [CallerArgumentExpression(nameof(sources))] string sourceExpression = null)
            => new TransactionBuilder<object>(sources.ToArray().Select((obs, i) => (Name: GetStepName($"{sourceExpression}[{i}]"), Source: obs.Select(o => (object) o))).ToList(),
                    mode, transactionName, context, scheduler, memberName, filePath, lineNumber,[TransactionNodeTag,mode.ToString()]) { InitialStepName = sourceExpression };
        
        public static ITransactionBuilder<TSource> BeginWorkflow<TSource>(this IObservable<TSource> source, object[] context = null,
            string transactionName = null, IScheduler scheduler = null, [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0,[CallerArgumentExpression(nameof(source))] string sourceExpression = null)
            => new TransactionBuilder<TSource>(source.BufferUntilCompleted()
                .Select(list => (object)list), transactionName??memberName, context, scheduler, memberName, filePath, lineNumber) {
                InitialStepName = GetStepName(sourceExpression)
            };
        
        public static ITransactionBuilder<TSource> BeginWorkflow<TSource>(this IEnumerable<IObservable<TSource>> sources,
            string transactionName, object[] context = null, IScheduler scheduler = null,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0,
            [CallerArgumentExpression(nameof(sources))] string sourceExpression = null) {
            var sourcesList = sources.ToList();
            if (!sourcesList.Any())
                return Observable.Empty<TSource>().BeginWorkflow(context, transactionName, scheduler, memberName, filePath, lineNumber);
            var firstStepExpression = $"{sourceExpression}[0]";
            var builder = sourcesList.First().BeginWorkflow(transactionName, context: context, scheduler: scheduler,
                memberName: memberName, filePath: filePath, lineNumber: lineNumber, sourceExpression: firstStepExpression);
            for (int i = 1; i < sourcesList.Count; i++) {
                var currentStepName = $"{sourceExpression}[{i}]";
                builder = builder.Then(sourcesList[i], stepName: currentStepName);
            }
            return builder;
        }

        private static ITransactionBuilder<TNext> Then<TCurrent, TNext>(this ITransactionBuilder<TCurrent> builder, StepAction<TCurrent, TNext> stepAction) {
            var ib = (TransactionBuilder<TCurrent>)builder;
            return new TransactionBuilder<TNext>(ib.InitialStep, ib.TransactionName, ib.Context, ib.Scheduler, [..ib.SubsequentSteps, stepAction.StepDefinition()], ib.Tags) {
                CallerMemberName = ib.CallerMemberName, CallerMemberPath = ib.CallerMemberPath, CallerMemberLine = ib.CallerMemberLine, InitialStepName = ib.InitialStepName,
                Mode = ib.Mode, BatchedSources = ib.BatchedSources
            };
        }
        public static ITransactionBuilder<TNext> Then<TCurrent, TNext>(this ITransactionBuilder<TCurrent> builder, IObservable<TNext> step,
            string stepName = null, Func<Exception, TCurrent[], IObservable<TNext>> fallbackSelector = null, [CallerArgumentExpression(nameof(step))] string selectorExpression = null,
            [CallerArgumentExpression(nameof(fallbackSelector))] string fallbackSelectorExpression = null,[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0) 
            => builder.Then(_ => step, stepName, fallbackSelector, selectorExpression, fallbackSelectorExpression, filePath, lineNumber);
        
        public static ITransactionBuilder<TNext> Then<TCurrent, TNext>(this ITransactionBuilder<TCurrent> builder, Func<TCurrent[], IObservable<TNext>> selector,
            string stepName = null, Func<Exception, TCurrent[], IObservable<TNext>> fallbackSelector = null, [CallerArgumentExpression(nameof(selector))] string selectorExpression = null,
            [CallerArgumentExpression(nameof(fallbackSelector))] string fallbackSelectorExpression = null,[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0) 
            => builder.Then(new StepAction<TCurrent, TNext> { Selector = selector, FallbackSelector = fallbackSelector, SelectorExpression = selectorExpression, 
                FallbackSelectorExpression = fallbackSelectorExpression, StepName = stepName, FilePath=filePath,LineNumber=lineNumber
            });

        public static ITransactionBuilder<TNext[]> Then<TCurrent, TNext>(this ITransactionBuilder<TCurrent> builder,
            Func<TCurrent[], ITransactionBuilder<TNext>> transactionSelector, bool failFast = true, [CallerArgumentExpression(nameof(transactionSelector))] string selectorExpression = null)
            => builder.Then(previousResults => transactionSelector(previousResults).Run(failFast), stepName: GetStepName(selectorExpression));

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
                        BoundaryName = ib.TransactionName, UserContext = ib.Context, InnerContext = ex.Context, Tags = ib.Tags.Concat([TransactionNodeTag,ib.Mode.ToString(),nameof(RunFailFast)]).ToList()
                    }));
                }):scheduledLogic.ChainFaultContext(ib.Context, null, ib.CallerMemberName, ib.CallerMemberPath, ib.CallerMemberLine,
                    ib.UpdateTags(collectAllResults));
            });

        private static List<string> UpdateTags<TFinal>(this TransactionBuilder<TFinal> ib,bool collectAllResults) {
            if (ib.Tags.Contains(StepNodeTag)) {
                return ib.Tags.ToList();
            }
            return ib.Tags.Concat([collectAllResults ? nameof(RunAndCollect) : nameof(RunToEnd)]).ToList();
        }

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

        public static ITransactionBuilder<object[]> ThenConcurrent<TCurrent>(this ITransactionBuilder<TCurrent> builder,
            Func<TCurrent[], IEnumerable<(string Name, IObservable<object> Source)>> concurrentSelector, 
            bool failFast = false, int maxConcurrency = 0, [CallerArgumentExpression(nameof(concurrentSelector))] string selectorExpression = null) {
            var tb = (TransactionBuilder<TCurrent>)builder;
            var stepName = GetStepName(selectorExpression);
            return builder.Then(previousResults => {
                Log(() => $"[Tx:{tb.TransactionName}][Step:{stepName}] Executing concurrent batch. FailFast={failFast}, MaxConcurrency={maxConcurrency}");
                var operations = concurrentSelector(previousResults).ToObservable();
                return failFast ? operations.ConcurrentFailFast(tb.TransactionName, maxConcurrency, tb.Context).ToList()
                        .Select(list => list.ToArray()).PushStackFrame(stepName)
                    : operations.ConcurrentRunToEnd(tb.TransactionName, maxConcurrency, tb.Context)
                        .SelectMany(resultTuple => {
                            var resultStream = Observable.Return(resultTuple.Results.ToArray());
                            return resultTuple.Fault == null ? resultStream : resultStream.Concat(Observable.Throw<object[]>(resultTuple.Fault));
                        }).PushStackFrame(stepName);
            }, stepName: stepName);
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
        
        

        private static StepDefinition StepDefinition<TCurrent, TNext>(this StepAction<TCurrent, TNext> stepAction){
            var step = new StepDefinition {
                Name = GetStepName(stepAction.SelectorExpression, stepAction.StepName, stepAction.Selector),
                Selector = currentResult => stepAction.Selector(CreateInputArray<TCurrent>(currentResult)).Select(res => (object)res),
                FilePath = stepAction.FilePath,LineNumber = stepAction.LineNumber
            };
            step.FallbackSelector = stepAction.FallbackSelector == null ? null : (ex, currentResult) => {
                var fallbackName = GetStepName(stepAction.FallbackSelectorExpression, null, stepAction.FallbackSelector);
                step.Name = $"{fallbackName} (Fallback)";
                return stepAction.FallbackSelector(ex, CreateInputArray<TCurrent>(currentResult)).Select(res => (object)res);
            };
            return step;
        }

        static TCurrent[] CreateInputArray<TCurrent>(object currentResult) => currentResult switch {
            null => [], TCurrent[] typedArray => typedArray,
            IEnumerable enumerable when typeof(TCurrent) == typeof(object) => enumerable.Cast<TCurrent>().ToArray(),
            TCurrent collection when collection is IEnumerable && !typeof(TCurrent).IsArray => [collection],
            IEnumerable<TCurrent> collection => collection.ToArray(),
            IEnumerable<object> objectCollection => objectCollection
                .SelectMany(o => {
                    if (o is TCurrent tCurrent) return tCurrent.YieldItem();
                    if (o is IEnumerable<TCurrent> cast) return cast;
                    if (!typeof(TCurrent).IsList()) return o.YieldItem().Cast<TCurrent>();
                    var instance = ((IList)Activator.CreateInstance(typeof(TCurrent)));
                    instance!.Add(o);
                    return ((TCurrent)instance).YieldItem();
                }).ToArray(),
            _ => [(TCurrent)currentResult]
        };
        private static IObservable<object> ConcurrentFailFast(this IObservable<(string Name, IObservable<object> Source)> source, string transactionName, int maxConcurrency, object[] context) 
            => source.Select(op => op.Source.PushStackFrame(op.Name)
                    .ChainFaultContext(context:context.AddToContext($"{transactionName} - {op.Name}"),null, op.Name))
                .Merge(maxConcurrency > 0 ? maxConcurrency : int.MaxValue);

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
        private static IObservable<T> PushFrameConditionally<T>(this IObservable<T> stepStream, string stepName, string filePath, int lineNumber) {
            var existingFrame = FaultHub.LogicalStackContext.Value?.FirstOrDefault();
            return existingFrame == null || existingFrame.Value.MemberName != stepName ? stepStream.PushStackFrame(stepName,filePath,lineNumber) : stepStream;
        }

        private static IObservable<(object finalStepResult, List<Exception> allFailures, List<object> allResults)> StepChain<TFinal>(this List<StepDefinition> allSteps, TransactionBuilder<TFinal> builder) 
            => allSteps.Aggregate(Observable.Return((results: (object)new List<object>(), failures: new List<Exception>(), allResults: new List<object>())), (accObservable, step) =>
                    accObservable.SelectMany(acc => step.ResilientBus(acc)
                        .PushFrameConditionally(!string.IsNullOrEmpty(step.Name) ? step.Name : $"Part {allSteps.IndexOf(step) + 1}",step.FilePath,step.LineNumber)
                        .Materialize().BufferUntilCompleted()
                        .Select(notifications => {
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
                return e.Exception.ExceptionToPublish(capturedStack.NewFaultContext(
                    builder.Context.AddToContext(builder.TransactionName,
                        $"{builder.TransactionName} - {stepNameForContext}"), tags: [StepNodeTag], memberName: stepNameForContext));
            });
        }
        
        private static IObservable<object> ResilientBus(this StepDefinition step,
            (object results, List<Exception> failures, List<object> allResults) acc){
            var primaryBus = Observable.Defer(() => step.Selector(acc.results));
            return step.FallbackSelector == null ? primaryBus
                : primaryBus.Catch((Exception ex) => step.FallbackSelector(ex, acc.results));
        }

        internal static string GetStepName(string expression, string explicitName = null, Delegate selector = null) {
            if (!string.IsNullOrEmpty(explicitName)) return explicitName;
            if (string.IsNullOrEmpty(expression)) return selector?.Method.Name;
            var codePart = expression.Split("=>").Last().Trim();
            var arrayMatch = System.Text.RegularExpressions.Regex.Match(codePart, @"^new\s*\[\s*\]\s*{\s*(?<content>.*?)\s*}(?<accessor>\[\d+\])?\s*$");
            if (arrayMatch.Success) {
                codePart = arrayMatch.Groups["content"].Value.Trim();
            }

            if (!codePart.Contains('.')) {
                var parenthesisIndex = codePart.IndexOf('(');
                if (parenthesisIndex > 0) {
                    codePart = codePart.Substring(0, parenthesisIndex);
                }
            }
            return string.IsNullOrEmpty(codePart) ? expression : codePart;
        }
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

        internal class StepAction<TIn, TOut> {
            public Func<TIn[], IObservable<TOut>> Selector { get; init; }
            public Func<Exception, TIn[], IObservable<TOut>> FallbackSelector { get; init; }
            public string SelectorExpression { get; init; }
            public string FallbackSelectorExpression { get; init; }
            public string StepName { get; init; }
            public string FilePath { get; set; }
            public int LineNumber { get; set; }
        }
        
        internal class StepDefinition {
            public Func<object, IObservable<object>> Selector { get; set; }
            public Func<Exception, object, IObservable<object>> FallbackSelector { get; set; }
            public string Name { get; set; }
            public string FilePath { get; set; }
            public int LineNumber { get; set; }
        }

        internal class TransactionBuilder<TCurrentResult> : ITransactionBuilder<TCurrentResult> {
            internal readonly IReadOnlyList<string> Tags;
            internal readonly IObservable<object> InitialStep;
            internal readonly List<StepDefinition> SubsequentSteps;
            internal readonly string TransactionName;
            internal readonly object[] Context;
            internal readonly IScheduler Scheduler;
            internal string CallerMemberName;
            internal string CallerMemberPath;
            internal int CallerMemberLine;
            internal string InitialStepName;

            internal TransactionMode Mode { get; init; } = TransactionMode.Sequential;
            internal List<(string Name, IObservable<object> Source)> BatchedSources { get; init; }

            internal TransactionBuilder(IObservable<object> initialStep, string transactionName, object[] context, IScheduler scheduler, List<StepDefinition> subsequentSteps,IReadOnlyList<string> tags = null) {
                InitialStep = initialStep;
                TransactionName = transactionName;
                Context = context;
                Scheduler = scheduler;
                SubsequentSteps = subsequentSteps;
                Tags=tags??[];
            }

            public TransactionBuilder(IObservable<object> initialStep, string transactionName, object[] context,
                IScheduler scheduler, string callerMemberName, string callerMemberPath, int callerMemberLine,IReadOnlyList<string> tags = null) 
                : this(initialStep, transactionName, context, scheduler, new List<StepDefinition>(),tags) {
                Log(() => $"[Tx.DEBUG][BUILDER][SEQ] Constructor called for transaction: '{transactionName}'.");
                CallerMemberName = callerMemberName;
                CallerMemberPath = callerMemberPath;
                CallerMemberLine = callerMemberLine;
            }

            public TransactionBuilder(List<(string Name, IObservable<object> Source)> batchedSources, TransactionMode mode,
                string transactionName, object[] context, IScheduler scheduler, string callerMemberName, string callerMemberPath, int callerMemberLine,IReadOnlyList<string> tags = null)
                : this(null, transactionName, context, scheduler, new List<StepDefinition>(),tags) {
                Log(() => $"[Tx.DEBUG][BUILDER][BATCH] Constructor called for transaction: '{transactionName}'. Mode: {mode}.");
                BatchedSources = batchedSources;
                Mode = mode;
                CallerMemberName = callerMemberName;
                CallerMemberPath = callerMemberPath;
                CallerMemberLine = callerMemberLine;
            }
        }
        
        [SuppressMessage("ReSharper", "UnusedTypeParameter")]
        public interface ITransactionBuilder<out TCurrentResult> { }
        public sealed class TransactionAbortedException(string message, Exception innerException, AmbientFaultContext context) : FaultHubException(message, innerException, context) {
            public override string ErrorStatus=>"failed" ;
        }
}