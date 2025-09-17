using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub{
    public static partial class Transaction {
        private static ITransactionBuilder<TNext> Then<TCurrent, TNext>(this ITransactionBuilder<TCurrent> builder, StepAction<TCurrent, TNext> stepAction) {
            var ib = (TransactionBuilder<TCurrent>)builder;
            return new TransactionBuilder<TNext>(ib.InitialStep, ib.TransactionName, ib.Context, ib.Scheduler, [..ib.SubsequentSteps, stepAction.StepDefinition()], ib.Tags) {
                CallerMemberName = ib.CallerMemberName, CallerMemberPath = ib.CallerMemberPath, CallerMemberLine = ib.CallerMemberLine, InitialStepName = ib.InitialStepName,
                Mode = ib.Mode, BatchedSources = ib.BatchedSources
            };
        }
        
        private static StepDefinition StepDefinition<TCurrent, TNext>(this StepAction<TCurrent, TNext> stepAction){
            var step = new StepDefinition {
                Name = GetStepName(stepAction.SelectorExpression, stepAction.StepName, stepAction.Selector),
                Selector = currentResult => stepAction.Selector(currentResult.AsArray<TCurrent>()).Select(res => (object)res),
                FilePath = stepAction.FilePath,LineNumber = stepAction.LineNumber,
                IsNonCritical = stepAction.IsNonCritical, DataSalvageStrategy = stepAction.DataSalvageStrategy
            };
            step.FallbackSelector = stepAction.FallbackSelector == null ? null : (ex, currentResult) => {
                var fallbackName = GetStepName(stepAction.FallbackSelectorExpression, null, stepAction.FallbackSelector);
                step.Name = $"{fallbackName} (Fallback)";
                return stepAction.FallbackSelector(ex, currentResult.AsArray<TCurrent>()).Select(res => (object)res);
            };
            return step;
        }
        public static ITransactionBuilder<TNext> Then<TCurrent, TNext>(this ITransactionBuilder<TCurrent> builder, IObservable<TNext> step,
            string stepName = null, Func<Exception, TCurrent[], IObservable<TNext>> fallbackSelector = null, Func<Exception, bool> isNonCritical = null,DataSalvageStrategy dataSalvageStrategy=DataSalvageStrategy.Inherit, 
            [CallerArgumentExpression(nameof(step))] string selectorExpression = null, [CallerArgumentExpression(nameof(fallbackSelector))] string fallbackSelectorExpression = null
            ,[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0) 
            => builder.Then(_ => step, stepName, fallbackSelector, isNonCritical,dataSalvageStrategy, selectorExpression, fallbackSelectorExpression, filePath, lineNumber);
        
        public static ITransactionBuilder<TNext> Then<TCurrent, TNext>(this ITransactionBuilder<TCurrent> builder, Func<TCurrent[], IObservable<TNext>> selector,
            string stepName = null, Func<Exception, TCurrent[], IObservable<TNext>> fallbackSelector = null,Func<Exception, bool> isNonCritical = null,DataSalvageStrategy dataSalvageStrategy=DataSalvageStrategy.Inherit
            , [CallerArgumentExpression(nameof(selector))] string selectorExpression = null, [CallerArgumentExpression(nameof(fallbackSelector))] string fallbackSelectorExpression = null
            ,[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0) 
            => builder.Then(new StepAction<TCurrent, TNext> { Selector = selector, FallbackSelector = fallbackSelector, IsNonCritical = isNonCritical, SelectorExpression = selectorExpression, 
                FallbackSelectorExpression = fallbackSelectorExpression, StepName = stepName, FilePath=filePath,LineNumber=lineNumber,DataSalvageStrategy = dataSalvageStrategy
            });

        [Obsolete]
        public static ITransactionBuilder<TNext[]> Then<TCurrent, TNext>(this ITransactionBuilder<TCurrent> builder,
            Func<TCurrent[], ITransactionBuilder<TNext>> transactionSelector, bool failFast = true, [CallerArgumentExpression(nameof(transactionSelector))] string selectorExpression = null)
            => builder.Then(previousResults => transactionSelector(previousResults).Run(failFast), stepName: GetStepName(selectorExpression));
        
        public static ITransactionBuilder<object[]> ThenConcurrent<TCurrent>(this ITransactionBuilder<TCurrent> builder,
            Func<TCurrent[], IEnumerable<(string Name, IObservable<object> Source)>> concurrentSelector, 
            bool failFast = false, int maxConcurrency = 0, [CallerArgumentExpression(nameof(concurrentSelector))] string selectorExpression = null,[CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) {
            var tb = (TransactionBuilder<TCurrent>)builder;
            var stepName = GetStepName(selectorExpression);
            return builder.Then(previousResults => {
                LogFast($"[Tx:{tb.TransactionName}][Step:{stepName}] Executing concurrent batch. FailFast={failFast}, MaxConcurrency={maxConcurrency}");
                var operations = concurrentSelector(previousResults).ToObservable();
                return failFast ? operations.ConcurrentFailFast(tb.TransactionName, maxConcurrency, tb.Context,filePath, lineNumber).ToList()
                        .Select(list => list.ToArray()).PushStackFrame(stepName)
                    : operations.ConcurrentRunToEnd(tb.TransactionName, maxConcurrency, tb.Context,filePath, lineNumber)
                        .SelectMany(resultTuple => {
                            var resultStream = Observable.Return(resultTuple.Results.ToArray());
                            return resultTuple.Fault == null ? resultStream : resultStream.Concat(Observable.Throw<object[]>(resultTuple.Fault));
                        }).PushStackFrame(stepName);
            }, stepName: stepName);
        }        

    }
}