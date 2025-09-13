using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub{
    partial class Transaction {
        private static readonly AsyncLocal<TransactionContext> CurrentTransactionContext = new();
        public static TransactionContext Current => CurrentTransactionContext.Value;

        public static IObservable<FaultHubException> TransactionFault(this IObservable<FaultHubException> source, Guid transactionId)
            => source.Where(e => e.TransactionId() == transactionId);
        
        public static Guid? TransactionId(this FaultHubException exception) 
            => exception.GetMetadata<Guid>(nameof(TransactionFault));

        public static ITransactionBuilder<TSource> BeginWorkflow<TSource>(this IObservable<TSource> source,
            string transactionName, object[] context = null,Guid? correlationId=null, IScheduler scheduler = null, [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerArgumentExpression(nameof(source))] string sourceExpression = null)
            => new TransactionBuilder<TSource>(source.ContextualSource( transactionName).BufferUntilCompleted()
                    .Select(list => (object)list), transactionName ?? memberName, context.AddToContext(correlationId.ToMetadataToken(nameof(TransactionFault))), scheduler, memberName, filePath, lineNumber,
                new List<string> { TransactionNodeTag, nameof(TransactionMode.Sequential) }.AddNestedTag()) {
                InitialStepName = GetStepName(sourceExpression)
            };

        private static IObservable<TSource> ContextualSource<TSource>(this IObservable<TSource> source, string transactionName){
            var transactionContext = new TransactionContext(transactionName);
            return Observable.Create<TSource>(obs => {
                var parentContext = CurrentTransactionContext.Value;
                CurrentTransactionContext.Value = transactionContext;
                return source.Finally(() => CurrentTransactionContext.Value = parentContext).Subscribe(obs);
            });
        }

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
                    return errors.Any() ? new AggregateException(errors).ExceptionToPublish(new AmbientFaultContext { BoundaryName = boundaryName }).Throw<T>() : notifications.ToObservable().Dematerialize();
                });

        public static IObservable<T> AsStep<T>(this IObservable<T> source,Func<Exception,bool> isNonCritical=null, [CallerArgumentExpression("source")] string stepExpression = null) {
            var parsedStepName = GetStepName(stepExpression);
            LogFast($"[CORRELATION_TRACE] Operator applied. Expression: '{stepExpression}'. Parsed Name: '{parsedStepName}'.");
            var context = Current;
            return context == null ? source
                : source.Catch((Exception ex) => {
                    var tags = new List<string> { StepNodeTag, AsStepOriginTag };
                    if (isNonCritical?.Invoke(ex) ?? false) {
                        tags.Add(NonCriticalStepTag);
                    }
                    var stepFault = ex.ExceptionToPublish(new AmbientFaultContext {
                        BoundaryName = parsedStepName,
                        Tags = tags
                    });
                    context.Failures.Add(stepFault);
                    return Observable.Throw<T>(stepFault);
                });
        }
    }
    
    public class TransactionContext(string name) {
        public string Name { get; } = name;
        internal ConcurrentBag<FaultHubException> Failures { get; } = new();
    }

}