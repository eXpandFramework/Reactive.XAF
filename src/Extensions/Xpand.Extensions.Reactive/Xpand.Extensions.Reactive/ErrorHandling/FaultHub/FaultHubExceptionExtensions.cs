using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xpand.Extensions.ExceptionExtensions;
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
                    .Where(c => c != null && !node.Name.Contains($"{c}"))
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

//MODIFICATION:
//MODIFICATION:
        public static OperationNode NewOperationTree(this FaultHubException topException) {
            FaultHubLogger.Log(() => "[Parser] Starting NewOperationTree parser...");
            var contextLookup = topException.SelectMany().OfType<FaultHubException>()
                .Where(ex => ex.Context != null).GroupBy(ex => ex.Context).ToDictionary(g => g.Key, g => g.First());
            FaultHubLogger.Log(() => $"[Parser] Created context lookup dictionary with {contextLookup.Count} entries.");


            
            OperationNode Collapse(OperationNode node) {
                if (node == null) return null;
                FaultHubLogger.Log(() => $"[Collapse] ==> Processing node '{node.Name}'");
                var collapsedChildren = node.Children.Select(Collapse).Where(c => c != null).ToList();

                if (collapsedChildren.Count == 1 && collapsedChildren[0].Name == node.Name) {
                    FaultHubLogger.Log(() => $"[Collapse] Redundancy found! Collapsing child '{collapsedChildren[0].Name}' into parent '{node.Name}'. Returning child instead.");
                    return collapsedChildren[0];
                }
                
                var newChildren = new List<OperationNode>();
                foreach (var child in collapsedChildren) {
                    if (child.Name == node.Name) {
                        FaultHubLogger.Log(() => $"[Collapse] Redundancy found! Absorbing children of '{child.Name}' into '{node.Name}'.");
                        newChildren.AddRange(child.Children);
                    } else {
                        newChildren.Add(child);
                    }
                }

                var finalNode = node with { Children = newChildren };
                FaultHubLogger.Log(() => $"[Collapse] <== Returning node '{finalNode.Name}' with {finalNode.Children.Count} children.");
                return finalNode;
            }

            var rawTree = topException.Context.BuildFromContext([],contextLookup);
            FaultHubLogger.Log(() => "[Parser] Raw tree built. Starting collapse phase...");
            var collapsedTree = Collapse(rawTree);
            FaultHubLogger.Log(() => "[Parser] Collapse phase finished.");
            return collapsedTree;
        }

        static OperationNode BuildFromGenericException(this Exception ex, IReadOnlyList<LogicalStackFrame> parentStack,
            Dictionary<AmbientFaultContext, FaultHubException> contextLookup) {
            FaultHubLogger.Log(() => $"[Parser.Generic] Processing generic exception: {ex?.GetType().Name}");
            if (ex is not AggregateException aggEx) return ex is FaultHubException fhEx ? fhEx.Context.BuildFromContext(parentStack, contextLookup) : null;
            var children = aggEx.InnerExceptions.Select(e => e.BuildFromGenericException( parentStack,contextLookup)).Where(n => n != null).ToList();
            FaultHubLogger.Log(() => $"[Parser.Generic] Parsed AggregateException into {children.Count} children.");
            return children.Count == 1 ? children.Single() : new OperationNode("Multiple Operations", [], children);

        }

        static OperationNode BuildFromContext(this AmbientFaultContext context,
            IReadOnlyList<LogicalStackFrame> parentStack,
            Dictionary<AmbientFaultContext, FaultHubException> contextLookup) {
                if (context == null) return null;
                FaultHubLogger.Log(() => $"[Parser.Context] ==> Processing context for: '{context.BoundaryName ?? "NULL"}'");

                if (context.BoundaryName == null) {
                    FaultHubLogger.Log(() => $"[Parser.Context] BoundaryName is null. Trying to build from generic inner exception.");
                    if (contextLookup.TryGetValue(context, out var owner)) {
                        return owner.InnerException.BuildFromGenericException(parentStack,contextLookup);
                    }
                    FaultHubLogger.Log(() => $"[Parser.Context] Could not find owner for nameless context. Returning null.");
                    return null;
                }
                
                var localStack = context.LogicalStackTrace ?? Enumerable.Empty<LogicalStackFrame>();
                var fullStack = localStack.Concat(parentStack).Distinct().ToList();
                var children = new List<OperationNode>();
                
                FaultHubLogger.Log(() => $"[Parser.Context] Recursing on InnerContext: '{context.InnerContext?.BoundaryName ?? "NULL"}'");
                var hierarchicalChild = context.InnerContext.BuildFromContext(fullStack,contextLookup);
                if (hierarchicalChild != null) {
                    children.Add(hierarchicalChild);
                }

                Exception rootCause = null;
                if (contextLookup.TryGetValue(context, out var contextOwner)) {
                    if (context.InnerContext == null) {
                        var errorNode = contextOwner.InnerException.BuildFromGenericException(fullStack,contextLookup);
                        if (errorNode != null) {
                            if (errorNode.Name == "Multiple Operations") children.AddRange(errorNode.Children);
                            else children.Add(errorNode);
                        }
                        if (contextOwner.InnerException is not (null or FaultHubException or AggregateException)) {
                            rootCause = contextOwner.InnerException;
                        }
                    } 
                    else if (contextOwner.InnerException is AggregateException aggEx) {
                        foreach (var innerEx in aggEx.InnerExceptions.OfType<FaultHubException>().Where(ex => ex.Context != context.InnerContext)) {
                            var branchNode = innerEx.Context.BuildFromContext(fullStack,contextLookup);
                            if (branchNode != null) children.Add(branchNode);
                        }
                    }
                }
                
                var resultNode = new OperationNode(context.BoundaryName, context.UserContext ?? [], children, rootCause, fullStack);
                FaultHubLogger.Log(() => $"[Parser.Context] <== Created node for '{resultNode.Name}' with {resultNode.Children.Count} children.");
                return resultNode;
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