using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub{
    partial class Transaction {
        public static ITransactionBuilder<TSource> BeginWorkflow<TSource>(this IObservable<TSource> source,
            string transactionName, object[] context = null, IScheduler scheduler = null, [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerArgumentExpression(nameof(source))] string sourceExpression = null)
            => new TransactionBuilder<TSource>(source.BufferUntilCompleted()
                    .Select(list => (object)list), transactionName ?? memberName, context, scheduler, memberName, filePath, lineNumber,
                new List<string> { TransactionNodeTag, nameof(TransactionMode.Sequential) }.AddNestedTag()) {
                InitialStepName = GetStepName(sourceExpression)
            };
        public static ITransactionBuilder<object> BeginWorkflow<TSource>(this IEnumerable<IObservable<TSource>> sources, string transactionName, TransactionMode mode = TransactionMode.Sequential,
            object[] context = null, IScheduler scheduler = null, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0,
            [CallerArgumentExpression(nameof(sources))] string sourceExpression = null)
            => new TransactionBuilder<object>(sources.ToArray().Select((obs, i) => (Name: GetStepName($"{sourceExpression}[{i}]"), Source: obs.Select(o => (object) o))).ToList(),
                mode, transactionName, context, scheduler, memberName, filePath, lineNumber,new[]{TransactionNodeTag,mode.ToString()}.AddNestedTag()) { InitialStepName = sourceExpression };
        
        public static ITransactionBuilder<TSource> BeginWorkflow<TSource>(this IObservable<TSource> source, object[] context = null,
            string transactionName = null, IScheduler scheduler = null, [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0,[CallerArgumentExpression(nameof(source))] string sourceExpression = null)
            => new TransactionBuilder<TSource>(source.BufferUntilCompleted()
                .Select(list => (object)list), transactionName??memberName, context, scheduler, memberName, filePath, lineNumber,new []{TransactionNodeTag,nameof(TransactionMode.Sequential)}.AddNestedTag()) {
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
    }
}