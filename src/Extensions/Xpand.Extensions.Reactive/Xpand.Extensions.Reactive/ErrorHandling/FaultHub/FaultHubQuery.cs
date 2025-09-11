using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub{
    public static class FaultHubQuery {
        public static IObservable<Alert> ToAlert(this IObservable<FaultHubException> source, params AlertRule[] rules)  
            => source.Select(ex => new { Exception = ex, Tree = ex.OperationTree() }).WhenNotDefault(x => x.Tree)
                .SelectMany(x => rules.Select(rule => new { Rule = rule, MatchedNode = x.Tree.Descendants().FirstOrDefault(rule.Predicate) }).WhereNotDefault(match =>match.MatchedNode )
                    .Select(match => new Alert(match.Rule.Name, match.Rule.Severity, match.Rule.MessageSelector?.Invoke(match.MatchedNode) ??
                        $"Alert triggered by rule '{match.Rule.Name}' for operation '{match.MatchedNode.Name}'.",
                        x.Exception
                    ))
                    .Take(1)
                );
        
        public static IObservable<string> TriggerRecovery(this IObservable<FaultHubException> source, IEnumerable<RecoveryRule> rules)
            => source.Select(ex => new { Exception = ex, Tree = ex.OperationTree() }).WhenNotDefault(x => x.Tree)
                .Select(x => rules.Select(rule => new { Rule = rule, MatchedNode = x.Tree.Descendants().FirstOrDefault(rule.Predicate) }).WhereNotDefault(match => match.MatchedNode)
                        .Select(match => match.Rule.RecoveryAction(x.Exception, match.MatchedNode).Select(_ => match.Rule.Name)).FirstOrDefault()).WhenNotDefault()
                .SelectMany();
        
        public static IObservable<FailureMetric> ToFailureMetrics(this IObservable<FaultHubException> source)
            => source.SelectMany(ex => {
                var tree = ex.OperationTree();
                if (tree == null) return [];
                var leafNodes = tree.Descendants().Where(n => n.RootCause != null).ToList();
                return leafNodes.Select(leaf => {
                    var path = tree.FindPathToNode( leaf);
                    var stepNode = path.LastOrDefault(n => n.Tags.Contains(Transaction.StepNodeTag));
                    var transactionNode = path.FirstOrDefault(n => n.Tags.Contains(Transaction.TransactionNodeTag));
                    return new FailureMetric(
                        TransactionName: transactionNode?.Name ?? tree.Name,
                        StepName: stepNode?.Name ?? leaf.Name,
                        RootCauseType: leaf.RootCause.GetType().FullName,
                        Tags: leaf.Tags ?? [],
                        Timestamp: DateTime.UtcNow
                    );
                });
            });

        private static IReadOnlyList<OperationNode> FindPathToNode(this OperationNode root, OperationNode target) {
            var pathStack = new Stack<OperationNode>();
            root.FindPathRecursive( target, pathStack);
            return pathStack.Reverse().ToList();
        }

        private static bool FindPathRecursive(this OperationNode current, OperationNode target, Stack<OperationNode> path) {
            path.Push(current);
            if (current == target) return true;

            foreach (var child in current.Children) {
                if (FindPathRecursive(child, target, path)) return true;
            }

            path.Pop();
            return false;
        }

        public static IMetadataToken ToMetadataToken(this object value,string key=null,[CallerArgumentExpression(nameof(value))] string valueExpression = null) 
            => new MetadataToken(key??valueExpression, value);

        public static T? GetMetadata<T>(this FaultHubException exception, string key) where T : struct {
            var token = exception.AllContexts.OfType<MetadataToken>().FirstOrDefault(t => t.Key == key);
            return token?.Value is T value ? value : null;
        }
    }
    
    public record FailureMetric(string TransactionName, string StepName, string RootCauseType, IReadOnlyList<string> Tags, DateTime Timestamp);
    public record RecoveryRule(string Name, Func<OperationNode, bool> Predicate, Func<FaultHubException, OperationNode, IObservable<Unit>> RecoveryAction);
    public record AlertRule(string Name, AlertSeverity Severity, Func<OperationNode, bool> Predicate, Func<OperationNode, string> MessageSelector = null);
    public record Alert(string RuleName, AlertSeverity Severity, string Message, FaultHubException Exception);

    public enum AlertSeverity {
        Warning,
        Error,
        Critical
    }
    
    public interface IMetadataToken { }

    public class MetadataToken(string key, object value) : IMetadataToken {
        public string Key { get; } = key;
        public object Value { get; } = value;
    }
}