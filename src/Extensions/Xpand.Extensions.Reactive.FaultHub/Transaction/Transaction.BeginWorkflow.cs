using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.Reactive.ErrorHandling;

using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Reactive.FaultHub.Transaction{
    partial class Transaction {
        

        public static IObservable<FaultHubException> TransactionFault(this IObservable<FaultHubException> source, Guid transactionId)
            => source.Where(e => e.TransactionId() == transactionId);
        
        public static Guid? TransactionId(this FaultHubException exception) 
            => exception.GetMetadata<Guid>(nameof(TransactionFault));

        public static ITransactionBuilder<TSource> BeginWorkflow<TSource>(this IObservable<TSource> source,
            string transactionName, object[] context = null,Guid? correlationId=null, IScheduler scheduler = null, [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerArgumentExpression(nameof(source))] string sourceExpression = null)
            => new TransactionBuilder<TSource>(source.BufferUntilCompleted()
                    .Select(list => (object)list), transactionName ?? memberName, context.AddToContext(correlationId.ToMetadataToken(nameof(TransactionFault))), scheduler, memberName, filePath, lineNumber,
                new List<string> { TransactionNodeTag, nameof(TransactionMode.Sequential) }.AddNestedTag()) {
                InitialStepName = GetStepName(sourceExpression)
            };

        private static IObservable<TSource> ContextualSource<TSource>(this IObservable<TSource> source, string transactionName) 
            => source.UseContext(new TransactionContext(transactionName), CurrentTransactionContext.Wrap());


        public static ITransactionBuilder<TSource> BeginWorkflow<TSource>(this IObservable<TSource> source, object[] context = null,
            string transactionName = null,Guid? correlationId=null, IScheduler scheduler = null, [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0,[CallerArgumentExpression(nameof(source))] string sourceExpression = null)
            => source.BeginWorkflow(transactionName,context,correlationId,scheduler,memberName,filePath,lineNumber,sourceExpression);

        public static ITransactionBuilder<object> BeginWorkflow<TSource>(this IEnumerable<IObservable<TSource>> sources, string transactionName=null, TransactionMode mode = TransactionMode.Sequential,
            object[] context = null, IScheduler scheduler = null, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0,
            [CallerArgumentExpression(nameof(sources))] string sourceExpression = null)
            => new TransactionBuilder<object>(sources.ToArray().Select((obs, i) => (Name: $"{sourceExpression}[{i}]", Source: obs.Select(o => (object) o))).ToList(),
                mode, transactionName ?? memberName, context, scheduler, memberName, filePath, lineNumber,new[]{TransactionNodeTag,mode.ToString()}.AddNestedTag()) { InitialStepName = sourceExpression };

        public static ITransactionBuilder<TSource> BeginWorkflow<TSource>(this IEnumerable<IObservable<TSource>> sources,
            string transactionName, object[] context = null,Guid? correlationId=null, IScheduler scheduler = null,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0,
            [CallerArgumentExpression(nameof(sources))] string sourceExpression = null) {
            var sourcesList = sources.ToList();
            if (!sourcesList.Any()) return Observable.Empty<TSource>().BeginWorkflow(context, transactionName,correlationId, scheduler, memberName, filePath, lineNumber);
            var firstStepExpression = $"{sourceExpression}[0]";
            var builder = sourcesList.First().BeginWorkflow(transactionName, context: context, scheduler: scheduler,
                memberName: memberName, filePath: filePath, lineNumber: lineNumber, sourceExpression: firstStepExpression);
            for (int i = 1; i < sourcesList.Count; i++) {
                var currentStepName = $"{sourceExpression}[{i}]";
                builder = builder.Then(sourcesList[i], stepName: currentStepName);
            }
            return builder;
        }
        public static IObservable<T> AsStep<T>(this IEnumerable<IObservable<T>> sources, [CallerMemberName] string boundaryName = "") 
            => sources.Select(s => s.Materialize()).Concat().ToList()
                .SelectMany(notifications => {
                    var errors = notifications.Where(n => n.Kind == NotificationKind.OnError).Select(n => n.Exception).ToList();
                    LogFast($"[DIAGNOSTIC][IEnumerable.AsStep] Collected {errors.Count} error(s). Types: [{string.Join(", ", errors.Select(e => e.GetType().Name))}]");
                    return errors.Any()
                        ? new AggregateException(errors).ExceptionToPublish(new AmbientFaultContext
                            { BoundaryName = boundaryName, Tags = [StepNodeTag, AsStepOriginTag] }).Throw<T>()
                        : notifications.ToObservable().Dematerialize();
                });
        public static IObservable<T> TraceTransactionContext<T>(this IObservable<T> source, string stepName) 
            => Observable.Defer(() => {
                var txName = Current?.Name ?? "NULL";
                LogFast($"[CONTEXT_TRACE] Entering '{stepName}'. Transaction Context: '{txName}'");
                return source.Do(_ => LogFast($"[CONTEXT_TRACE] Emit '{stepName}'. Transaction Context: '{txName}'"),() => LogFast($"[CONTEXT_TRACE] Completed '{stepName}'. Transaction Context: '{txName}'"));
            });

        public static IObservable<T> TransactionFlowContext<T>(this IObservable<T> source,
            Func<IObservable<T>, IObservable<T>> retrySelector = null)
            => source.FlowContext(retrySelector, CurrentTransactionContext.Wrap());

        public static IObservable<T> AsStep<T>(this IObservable<T> source, ResilienceAction resilienceAction, [CallerArgumentExpression("source")] string stepExpression = null)
            => source.AsStep(_ => resilienceAction, stepExpression);

        public static IObservable<T> AsStep<T>(this IObservable<T> source, Func<Exception, ResilienceAction> onFault = null, [CallerArgumentExpression("source")] string stepExpression = null) {
            var parsedStepName = GetStepName(stepExpression);
            LogFast($"[CORRELATION_TRACE][AsStep] Operator applied. Expression: '{stepExpression}'. Parsed Name: '{parsedStepName}'.");
            
            return source.Catch((Exception ex) => {
                var transactionContext = Current;
                LogFast($"[DIAGNOSTIC][AsStep] ==> CATCH BLOCK ENTERED for step '{parsedStepName}'. Exception: '{ex.GetType().Name}'.");
                
                if (transactionContext == null) {
                    LogFast($"[DIAGNOSTIC][AsStep] No active transaction. Re-throwing original exception.");
                    return Observable.Throw<T>(ex);
                }

                LogFast($"[DIAGNOSTIC][AsStep] Active transaction: '{transactionContext.Name}'. Invoking onFault selector.");
                var resilienceAction = onFault?.Invoke(ex) ?? ResilienceAction.Critical;
                LogFast($"[DIAGNOSTIC][AsStep] onFault selector returned: '{resilienceAction}'.");

                var tags = new List<string> { StepNodeTag, AsStepOriginTag };
                if (resilienceAction == ResilienceAction.Tolerate || resilienceAction == ResilienceAction.Suppress) {
                    tags.Add(NonCriticalStepTag);
                }

                LogFast($"[DIAGNOSTIC][AsStep] Creating fault. Tags: [{string.Join(", ", tags)}]");
                var stepFault = ex.ExceptionToPublish(new AmbientFaultContext {
                    BoundaryName = parsedStepName,
                    Tags = tags
                });

                if (resilienceAction == ResilienceAction.Suppress) {
                    LogFast($"[DIAGNOSTIC][AsStep] Reporting suppressed fault to transaction and completing stream.");
                    transactionContext.Failures.Add(stepFault);
                    return Observable.Empty<T>();
                }
                
                LogFast($"[DIAGNOSTIC][AsStep] <== CATCH BLOCK EXITING. Propagating fault.");
                return Observable.Throw<T>(stepFault);
            });
        }    }

    public enum ResilienceAction {
        Critical,
        Tolerate,
        Suppress
    }

    public class TransactionContext(string name) {
        public string Name { get; } = name;
        internal ConcurrentBag<FaultHubException> Failures { get; } = new();
    }

}