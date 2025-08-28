using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xpand.Extensions.Reactive.Combine;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub {
    public record OperationNode(
        string Name,
        IReadOnlyList<object> ContextData,
        IReadOnlyList<OperationNode> Children,
        Exception RootCause = null,
        IReadOnlyList<LogicalStackFrame> LogicalStack = null
    );
    public static class FaultHubExceptionExtensions {
        public static string Render(this FaultHubException exception) {
            if (exception == null) return string.Empty;
            IEnumerable<OperationNode> trees;
            if (exception.InnerException is AggregateException aggEx)
                trees = aggEx.InnerExceptions.OfType<FaultHubException>()
                    .Select(ex => ex.NewOperationTree()).Where(t => t != null);
            else
                trees = new[] { exception.NewOperationTree() }.Where(t => t != null);
            var mergedTree = trees.Union();
            if (mergedTree == null) return exception.InnerException?.ToString() ?? exception.Message;

            var sb = new StringBuilder();
            var allRootCauses = exception.FindRootCauses().ToList();
            var summary = string.Join(" • ", allRootCauses.Select(rc => rc.Message).Distinct());
            
            var title = mergedTree.Name.CompoundName();
            var rootContextItems = mergedTree.ContextData.Where(o => o is not OperationNode).ToArray();
            var rootContext = rootContextItems.Any() ? $" [{string.Join(", ", rootContextItems)}]" : "";
            
            var message = exception is TransactionAbortedException ? $"{title} failed" : $"{title} completed with errors";
            if (allRootCauses.Any()){
                message = $"{title} failed{rootContext} ({allRootCauses.Count} times: {summary})";
            }
            sb.AppendLine(message);
            
            for (var i = 0; i < mergedTree.Children.Count; i++) {
                var child = mergedTree.Children[i];
                var isLastChild = i == mergedTree.Children.Count - 1;
                RenderNode(child, sb, "", isLastChild, true);
            }

            return sb.ToString().Trim();
        }

        public static string RenderStack(this OperationNode node) {
            var logicalStack = node?.GetLogicalStack();
            if (logicalStack == null || !logicalStack.Any()) return string.Empty;
            var sb = new StringBuilder();
            sb.AppendLine("--- Invocation Stack ---");
            foreach (var frame in logicalStack)
                sb.Append("  at ").Append(frame.MemberName)
                    .Append(" in ").Append(frame.FilePath)
                    .Append(":line ").Append(frame.LineNumber)
                    .AppendLine();
            return sb.ToString().TrimEnd();
        }

        public static IReadOnlyList<LogicalStackFrame> GetLogicalStack(this OperationNode node)
            => node.LogicalStack ??
               node.Children.Select(child => child.GetLogicalStack()).FirstOrDefault(s => s != null);

        public static Exception GetRootCause(this OperationNode node)
            => node.RootCause ?? node.Children.Select(child => child.GetRootCause()).FirstOrDefault(ex => ex != null);


        public static string Render(this OperationNode rootNode,bool includeDetails = false) {
            if (rootNode == null) return string.Empty;
            var sb = new StringBuilder();
            RenderNode(rootNode, sb, "", true, includeDetails);
            return sb.ToString().TrimEnd();
        }

        public static OperationNode Union(this IEnumerable<OperationNode> source) {
            var nodes = source?.ToList();
            if (nodes == null || !nodes.Any()) return null;
            if (nodes.Count == 1) return nodes.Single();
            var rootGroups = nodes.GroupBy(n => n.Name).ToList();
            if (rootGroups.Count > 1) return new OperationNode("Multiple Operations", [], nodes);
            var groupToMerge = rootGroups.Single();
            var representative = groupToMerge.First();
            var allChildren = groupToMerge.SelectMany(n => n.Children).ToArray();
            var logicalStack = groupToMerge.SelectMany(n => n.LogicalStack ?? Enumerable.Empty<LogicalStackFrame>())
                .Distinct().ToList();
            return new OperationNode(representative.Name, representative.ContextData, allChildren.Any()
                    ? allChildren.GroupBy(c => c.Name).Select(childGroup => Union(childGroup.ToList())).ToList()
                    : new List<OperationNode>(), groupToMerge.FirstOrDefault(n => n.RootCause != null)?.RootCause,
                logicalStack);
        }

        public static string Title(this OperationNode rootNode) {
            if (rootNode == null) return "Invalid Operation";
            var operationName = rootNode.Name.CompoundName();
            var rootCause = rootNode.GetRootCause();
            return rootCause != null
                ? $"{operationName} failed ({rootCause.Message})"
                : $"{operationName} completed successfully";
        }

        private static string CompoundName(this string s)
            => s == null ? null : Regex.Replace(s.Replace('_', ' '), @"(\B[A-Z])", " $1");

        private static void RenderNode(OperationNode node, StringBuilder sb, string prefix, bool isLast, bool includeDetails) {
            var contextDataString = "";
            if (includeDetails && node.ContextData.Any()) {
                var filteredContext = node.ContextData
                    .Where(c => c != null && !node.Name.Contains(c.ToString()))
                    .ToList();
                if (filteredContext.Any()) {
                    contextDataString = $" ({string.Join(", ", filteredContext)})";
                }
            }
            var contextData = contextDataString;
            
            var connector = isLast ? "└ " : "├ ";
            sb.Append(prefix).Append(connector).AppendLine(node.Name.CompoundName() + contextData);
            var childPrefix = prefix + (isLast ? "  " : "│ ");

            var children = node.Children;
            for (var i = 0; i < children.Count; i++) {
                var child = children[i];
                var isLastChildInVisualBlock = (i == children.Count - 1) && (!includeDetails || node.RootCause == null);
                RenderNode(child, sb, childPrefix, isLastChildInVisualBlock, includeDetails);
            }

            if (includeDetails && node.RootCause != null) {
                sb.Append(childPrefix).Append("• Root Cause: ").Append(node.RootCause.GetType().FullName).Append(": ").AppendLine(node.RootCause.Message);
                var stack = node.RenderStack();
                if (!string.IsNullOrEmpty(stack)) {
                    var indentedStack = string.Join(Environment.NewLine, stack.Split(["\r\n", "\n"], StringSplitOptions.None).Select(l => childPrefix + "  " + l));
                    sb.AppendLine(indentedStack);
                }

                sb.Append(childPrefix).AppendLine("  --- Original Exception Details ---");
                using var reader = new System.IO.StringReader(node.RootCause.ToString());
                while (reader.ReadLine() is{ } line) {
                    sb.Append(childPrefix).Append("    ").AppendLine(line);
                }
            }
            
        }

        public static OperationNode GetChild(this OperationNode node, string name) 
            => node.Children.Single(c => c.Name == name);

        public static OperationNode NewOperationTree(this FaultHubException topException) {
OperationNode UnionTree(IEnumerable<OperationNode> source) {
                var nodes = source?.Where(n => n != null).ToList();
                if (nodes == null || !nodes.Any()) return null;
                if (nodes.Count == 1) return nodes.Single();
                return new OperationNode("Multiple Operations", [], nodes);
            }

            OperationNode BuildNode(Exception ex, IReadOnlyList<LogicalStackFrame> parentStack) {
                if (ex is AggregateException aggEx) {
                    return UnionTree(aggEx.InnerExceptions.Select(inner => BuildNode(inner, parentStack)));
                }

                if (ex is not FaultHubException fhEx) {
                    return null;
                }

                if (fhEx.Context?.BoundaryName == null) {
                    return BuildNode(fhEx.InnerException, parentStack);
                }
                
                var localStack = fhEx.Context.LogicalStackTrace ?? Enumerable.Empty<LogicalStackFrame>();
                var fullStackForThisNode = localStack.Concat(parentStack).Distinct().ToList();

                var name = fhEx.Context.BoundaryName;
                var contextData = fhEx.Context.UserContext ?? [];
                var children = new List<OperationNode>();

                var childNode = BuildNode(fhEx.InnerException, fullStackForThisNode);
                if (childNode != null) {
                    if (childNode.Name == "Multiple Operations") {
                        children.AddRange(childNode.Children);
                    } else {
                        children.Add(childNode);
                    }
                }
                
                var rootCause = (fhEx.InnerException is null or FaultHubException or AggregateException) ? null : fhEx.InnerException;
                
                var stackToStore = (rootCause != null) ? fullStackForThisNode : null;

                return new OperationNode(name, contextData, children, rootCause, stackToStore);
            }

            return BuildNode(topException, new List<LogicalStackFrame>());            
        }
        private static IEnumerable<Exception> FindRootCauses(this Exception ex) {
            if (ex is AggregateException aggEx)
                foreach (var inner in aggEx.InnerExceptions)
                foreach (var root in inner.FindRootCauses())
                    yield return root;
            else if (ex is FaultHubException { InnerException: not null } fhEx)
                foreach (var root in fhEx.InnerException.FindRootCauses())
                    yield return root;
            else if (ex != null) yield return ex;
        }

        
    }
}