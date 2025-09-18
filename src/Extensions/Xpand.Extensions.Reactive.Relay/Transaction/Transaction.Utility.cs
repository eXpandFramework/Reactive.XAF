using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.Reactive.Relay.Transaction{
    public static partial class Transaction {
        private static readonly AsyncLocal<int> TransactionNestingLevel = new();
        public const string NonCriticalStepTag =FaultHubException.SystemTag+ "NonCriticalStep";
        public const string NonCriticalAggregateTag =FaultHubException.SystemTag+  "NonCriticalAggregate";
        public const string AsStepOriginTag = FaultHubException.SystemTag+"AsStepOrigin";
        internal static readonly AsyncLocal<TransactionContext> CurrentTransactionContext = new();
        internal const string SalvagedDataKey = "FaultHub.SalvagedData";
        public static TransactionContext Current => CurrentTransactionContext.Value;
        public const string TransactionNodeTag = "Transaction";
        public const string NestedTransactionNodeTag = "Nested";
        public const string StepNodeTag = "Step";
        
        
        private static List<string> AddNestedTag(this ICollection<string> tags) 
            => TransactionNestingLevel.Value <= 0 ? tags.ToList() : tags.AddToArray(NestedTransactionNodeTag).ToList();
        
        private static List<string> UpdateRunTags<TFinal>(this TransactionBuilder<TFinal> ib,bool collectAllResults) 
            => ib.Tags.Contains(StepNodeTag) ? ib.Tags.ToList() : ib.Tags.Concat([collectAllResults ? nameof(RunAndCollect) : nameof(RunToEnd)]).ToList();


        private static IObservable<T> PushFrameConditionally<T>(this IObservable<T> stepStream, string stepName, string filePath, int lineNumber) {
            var existingFrame = FaultHub.LogicalStackContext.Value?.FirstOrDefault();
            return existingFrame == null || existingFrame.Value.MemberName != stepName ? stepStream.PushStackFrame( stepName,filePath,lineNumber) : stepStream;
        }
        
        public static string GetStepName(string expression, string explicitName = null, Delegate selector = null) {
            if (!string.IsNullOrEmpty(explicitName)) return explicitName;
            if (string.IsNullOrEmpty(expression)) return selector?.Method.Name;

            var codePart = expression.Split("=>").Last().Trim();

            var arrayMatch = System.Text.RegularExpressions.Regex.Match(codePart, @"^new\s*\[\s*\]\s*{\s*(?<content>.*?)\s*}(?<accessor>\[\d+\])?\s*$");
            if (arrayMatch.Success) {
                codePart = arrayMatch.Groups["content"].Value.Trim();
            }

            var indexerMatch = System.Text.RegularExpressions.Regex.Match(codePart, @"^(.*?)\[.*\]\s*$");
            if (indexerMatch.Success) {
                codePart = indexerMatch.Groups[1].Value.Trim();
            }

            var parenthesisIndex = codePart.IndexOf('(');
            if (parenthesisIndex > 0) {
                codePart = codePart.Substring(0, parenthesisIndex);
            }

            codePart = codePart.TrimEnd(')');
            return string.IsNullOrEmpty(codePart) ? expression : codePart;
        }
    }
    
    internal class StepAction<TIn, TOut> {
        public DataSalvageStrategy DataSalvageStrategy { get; init; }
        public Func<Exception, bool> IsNonCritical { get; init; }
        public Func<TIn[], IObservable<TOut>> Selector { get; init; }
        public Func<Exception, TIn[], IObservable<TOut>> FallbackSelector { get; init; }
        public string SelectorExpression { get; init; }
        public string FallbackSelectorExpression { get; init; }
        public string StepName { get; init; }
        public string FilePath { get; set; }
        public int LineNumber { get; set; }
    }
        
    internal class StepDefinition {
        public DataSalvageStrategy DataSalvageStrategy { get; set; }
        public Func<Exception, bool> IsNonCritical { get; set; }
        public Func<object, IObservable<object>> Selector { get; set; }
        public Func<Exception, object, IObservable<object>> FallbackSelector { get; set; }
        public string Name { get; set; }
        public string FilePath { get; set; }
        public int LineNumber { get; set; }
    }

    internal class TransactionBuilder<TCurrentResult> : ITransactionBuilder<TCurrentResult> {
        internal DataSalvageStrategy DataSalvageStrategy { get; set; } = DataSalvageStrategy.EmitPartialResults;
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

        internal TransactionBuilder(IObservable<object> initialStep,string transactionName, object[] context, IScheduler scheduler, List<StepDefinition> subsequentSteps,IReadOnlyList<string> tags = null) {
            LogFast($"[Tx.DEBUG][BUILDER][SEQ] Constructor called for transaction: '{transactionName}'.");
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
            LogFast($"[Tx.DEBUG][BUILDER][SEQ] Constructor called for transaction: '{transactionName}'.");
            CallerMemberName = callerMemberName;
            CallerMemberPath = callerMemberPath;
            CallerMemberLine = callerMemberLine;
        }

        public TransactionBuilder(List<(string Name, IObservable<object> Source)> batchedSources, TransactionMode mode,
            string transactionName, object[] context, IScheduler scheduler, string callerMemberName, string callerMemberPath, int callerMemberLine,IReadOnlyList<string> tags = null)
            : this(null, transactionName, context, scheduler, new List<StepDefinition>(),tags) {
            LogFast($"[Tx.DEBUG][BUILDER][BATCH] Constructor called for transaction: '{transactionName}'. Mode: {mode}.");
            BatchedSources = batchedSources;
            Mode = mode;
            CallerMemberName = callerMemberName;
            CallerMemberPath = callerMemberPath;
            CallerMemberLine = callerMemberLine;
        }
    }

    [SuppressMessage("ReSharper", "UnusedTypeParameter")]
    public interface ITransactionBuilder<out TCurrentResult> {
    }
    public sealed class TransactionAbortedException(string message, Exception innerException, AmbientFaultContext context)
        : FaultHubException(message, innerException, context) {
        public TransactionAbortedException(string message, FaultHubException innerException, object[] context,
            IReadOnlyList<string> tags = null, string boundaryName = "") :this(message, innerException,new AmbientFaultContext {
            BoundaryName = boundaryName, UserContext = context, InnerContext = innerException.Context,
            Tags = tags }) {
        }

        public override string ErrorStatus=>"failed" ;
    }
    public enum TransactionMode {
        Sequential,
        Concurrent
    }
    
    public enum DataSalvageStrategy {
        Inherit,
        EmitPartialResults,
        EmitEmpty
    }
}