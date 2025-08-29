using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xpand.Extensions.ExceptionExtensions;
using Xpand.Extensions.Reactive.Combine;
using static Xpand.Extensions.Reactive.ErrorHandling.FaultHub.FaultHubLogger;

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
            var mergedTree = exception.NewOperationTree();
            if (mergedTree == null) return exception.InnerException?.ToString() ?? exception.Message;
            var sb = new StringBuilder();
            var allRootCauses = exception.FindRootCauses().ToList();
            var summary = string.Join(" • ", allRootCauses.Select(rc => rc.Message).Distinct());
            var rootContextItems = mergedTree.ContextData.Where(o => o is not OperationNode).ToArray();
            var rootContext = rootContextItems.Any() ? $" [{string.Join(", ", rootContextItems)}]" : "";
            sb.AppendLine(exception.GenerateErrorMessage( mergedTree.Name.CompoundName(), allRootCauses, rootContext, summary));
            if (mergedTree.Children.Any()) {
                mergedTree.RenderChildNodes(rootContextItems, sb);
            }
            else if (mergedTree.GetRootCause() != null || (mergedTree.GetLogicalStack() != null && mergedTree.GetLogicalStack().Any())) {
                AppendRootCauseDetails(mergedTree, sb, "  ");
            }
            return sb.ToString().Trim();
        }

        private static string GenerateErrorMessage(this FaultHubException exception, string title, List<Exception> allRootCauses,
            string rootContext, string summary)
            => allRootCauses.Any() ? $"{title} failed{rootContext} ({allRootCauses.Count} times: {summary})" :
                exception is TransactionAbortedException ? $"{title} failed" : $"{title} completed with errors";

        private static void RenderChildNodes(this OperationNode mergedTree,object[] rootContextItems,  StringBuilder sb){
            var titleContextItems = new HashSet<object>(rootContextItems);
            for (var i = 0; i < mergedTree.Children.Count; i++) {
                var child = mergedTree.Children[i];
                var isLastChild = i == mergedTree.Children.Count - 1;
                child.RenderNode(mergedTree, new List<OperationNode>(), titleContextItems, sb, "", isLastChild, true);
            }
        }

        public static string RenderStack(this OperationNode node) {
            var logicalStack = node?.GetLogicalStack();
            if (logicalStack == null || !logicalStack.Any()) return string.Empty;
            var blacklistedPatterns = FaultHub.BlacklistedFilePathRegexes;
            if (!blacklistedPatterns.Any()) return logicalStack.RenderSimpleStack();
            var sb = new StringBuilder();
            sb.AppendLine("--- Invocation Stack ---");
            sb.BuildRenderedStackString( logicalStack,blacklistedPatterns);
            return sb.ToString().TrimEnd();
        }

        private static void BuildRenderedStackString(this StringBuilder sb, IReadOnlyList<LogicalStackFrame> originalStack, Dictionary<string, string> blacklistedPatterns) {
            var filteredItems = originalStack.FilterAndGroupBlacklistedFrames( blacklistedPatterns);
            if (!filteredItems.OfType<LogicalStackFrame>().Any() && originalStack.Any()) {
                sb.RenderFallbackStack( originalStack);
            }
            else {
                sb.RenderFilteredStack( filteredItems);
            }
            sb.AppendBlacklistFooter( filteredItems, blacklistedPatterns);
        }

        private static void RenderFilteredStack(this StringBuilder sb, List<object> filteredItems) {
            foreach (var item in filteredItems) {
                if (item is LogicalStackFrame frame) {
                    sb.Append("  ").AppendLine(frame.ToString());
                }
                else if (item is int count) {
                    sb.Append("  ").AppendLine($"... {count} frame(s) hidden ...");
                }
            }
        }
        
        private static void RenderFallbackStack(this StringBuilder sb, IReadOnlyList<LogicalStackFrame> originalStack) {
            foreach (var frame in originalStack) {
                sb.Append("  ").AppendLine(frame.ToString());
            }
            sb.Append("  ").AppendLine("... (Fallback: All frames shown as the blacklist would hide the entire stack) ...");
        }

        private static List<object> FilterAndGroupBlacklistedFrames(this IReadOnlyList<LogicalStackFrame> logicalStack, Dictionary<string, string> blacklistedPatterns) {
            var filteredItems = new List<object>();
            int consecutiveHiddenCount = 0;

            foreach (var frame in logicalStack) {
                if (!string.IsNullOrEmpty(frame.FilePath) && blacklistedPatterns.Keys.Any(pattern => Regex.IsMatch(frame.FilePath, pattern, RegexOptions.IgnoreCase))) {
                    consecutiveHiddenCount++;
                }
                else {
                    if (consecutiveHiddenCount > 0) {
                        filteredItems.Add(consecutiveHiddenCount);
                        consecutiveHiddenCount = 0;
                    }
                    filteredItems.Add(frame);
                }
            }

            if (consecutiveHiddenCount > 0) {
                filteredItems.Add(consecutiveHiddenCount);
            }
            return filteredItems;
        }

        private static string RenderSimpleStack(this IReadOnlyList<LogicalStackFrame> logicalStack) {
            var sb = new StringBuilder();
            sb.AppendLine("--- Invocation Stack ---");
            foreach (var frame in logicalStack) {
                sb.Append("  ").AppendLine(frame.ToString());
            }
            return sb.ToString().TrimEnd();
        }
        
        private static void AppendBlacklistFooter(this StringBuilder sb, List<object> filteredItems, Dictionary<string, string> blacklistedPatterns) {
            var appliedPatterns = blacklistedPatterns.Values.Distinct().ToList();
            if (appliedPatterns.Any() && filteredItems.OfType<int>().Any()) {
                sb.AppendLine();
                sb.Append("Blacklisted patterns applied: ").Append(string.Join(", ", appliedPatterns));
            }
        }
        public static IReadOnlyList<LogicalStackFrame> GetLogicalStack(this OperationNode node)
            => node.LogicalStack ??
               node.Children.Select(child => child.GetLogicalStack()).FirstOrDefault(s => s != null);

        public static Exception GetRootCause(this OperationNode node)
            => node.RootCause ?? node.Children.Select(child => child.GetRootCause()).FirstOrDefault(ex => ex != null);


        public static string Render(this OperationNode rootNode,bool includeDetails = false) {
            if (rootNode == null) return string.Empty;
            var sb = new StringBuilder();
            rootNode.RenderNode(null, new List<OperationNode>(), new HashSet<object>(), sb, "", true, includeDetails);
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
            var logicalStack = groupToMerge.SelectMany(n => n.LogicalStack ?? Enumerable.Empty<LogicalStackFrame>()).Distinct().ToList();
            return new OperationNode(representative.Name, representative.ContextData, allChildren.Any()
                    ? allChildren.GroupBy(c => c.Name).Select(childGroup => Union(childGroup.ToList())).ToList()
                    : new List<OperationNode>(), groupToMerge.FirstOrDefault(n => n.RootCause != null)?.RootCause,
                logicalStack);
        }
        
        private static string CompoundName(this string s)
            => s == null ? null : Regex.Replace(s.Replace("()", "").Replace('_', ' '), @"(\B[A-Z])", " $1");

        private static void RenderNode(this OperationNode node, OperationNode parent, List<OperationNode> ancestors, HashSet<object> titleContextItems, StringBuilder sb, string prefix, bool isLast, bool includeDetails) {
            var contextData = ancestors.ContextData(node, parent,  titleContextItems, includeDetails);
            var connector = isLast ? "└ " : "├ ";
            sb.Append(prefix).Append(connector).AppendLine(node.Name.CompoundName() + contextData);
            var childPrefix = prefix + (isLast ? "  " : "│ ");
            node.RenderChildNodes( ancestors, titleContextItems, sb, includeDetails, childPrefix);
            if (includeDetails && node.RootCause != null) AppendRootCauseDetails(node, sb, childPrefix);
        }

        private static void RenderChildNodes(this OperationNode node, List<OperationNode> ancestors, HashSet<object> titleContextItems, StringBuilder sb, bool includeDetails, string childPrefix){
            var newAncestors = new List<OperationNode>(ancestors) { node };
            for (var i = 0; i < node.Children.Count; i++) {
                var child = node.Children[i];
                var isLastChildInVisualBlock = (i == node.Children.Count - 1) && (!includeDetails || node.RootCause == null);
                child.RenderNode(node, newAncestors, titleContextItems, sb, childPrefix, isLastChildInVisualBlock, includeDetails);
            }
        }

        private static void AppendRootCauseDetails(OperationNode node, StringBuilder sb, string childPrefix){
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

        private static string ContextData(this List<OperationNode> ancestors,OperationNode node, OperationNode parent, HashSet<object> titleContextItems, bool includeDetails){
            var contextDataString = "";
            if (includeDetails && node.ContextData.Any()) {
                var namesToFilter = new HashSet<string>(ancestors.Select(a => a.Name)) { node.Name };
                if (parent != null) {
                    namesToFilter.Add(parent.Name);
                    namesToFilter.Add($"{parent.Name} - {node.Name}");
                    foreach (var sibling in parent.Children.Where(c => c != node)) {
                        namesToFilter.Add(sibling.Name);
                        namesToFilter.Add($"{sibling.Name} - {node.Name}");
                    }
                }

                Log(()=>$"[Render.Filter] For Node '{node.Name}', FilterSet is: [{string.Join(", ", namesToFilter)}]");
                var filteredContext = node.ContextData
                    .Where(c => {
                        if (c == null) return false;
                        if (titleContextItems.Contains(c)) return false;
                        var contextString = c.ToString();
                        var shouldKeep = !namesToFilter.Contains(contextString);
                        Log(()=>$"[Render.Filter.Eval] Item: '{contextString}', Keep: {shouldKeep}");
                        return shouldKeep;
                    })
                    .ToList();
                if (filteredContext.Any()) {
                    contextDataString = $" ({string.Join(", ", filteredContext)})";
                }
            }
            var contextData = contextDataString;
            return contextData;
        }

        public static OperationNode GetChild(this OperationNode node, string name) 
            => node.Children.Single(c => c.Name == name);

        public static OperationNode NewOperationTree(this FaultHubException topException) {
            Log(() => "[Parser] Starting NewOperationTree parser...");
            var contextLookup = topException.FaultHubExceptions();
            var rawTree = topException.Context.BuildFromContext([],contextLookup);
            Log(() => "[Parser] Raw tree built. Starting collapse phase...");
            var collapsedTree = rawTree.Collapse();
            Log(() => "[Parser] Collapse phase finished.");
            return collapsedTree;        }

        private static Dictionary<AmbientFaultContext, FaultHubException> FaultHubExceptions(this FaultHubException topException){
            var contextLookup = topException.SelectMany().OfType<FaultHubException>()
                .Where(ex => ex.Context != null).GroupBy(ex => ex.Context)
                .ToDictionary(g => g.Key, g => g.First());
            if (contextLookup.Count == 1) {
                var singleFault = contextLookup.Values.First();
                var currentCtx = singleFault.Context?.InnerContext;
                while (currentCtx != null) {
                    contextLookup.TryAdd(currentCtx, singleFault);
                    currentCtx = currentCtx.InnerContext;
                }
            }
            Log(() => $"[Parser] Created context lookup dictionary with {contextLookup.Count} entries.");
            return contextLookup;
        }

        static OperationNode Collapse(this OperationNode node) {
            if (node == null) return null;
            Log(() => $"[Collapse] ==> Processing node '{node.Name}'");
            var collapsedChildren = node.Children.Select(Collapse).Where(c => c != null).ToList();
            if (collapsedChildren.Count == 1 && collapsedChildren[0].Name == node.Name) {
                Log(() => $"[Collapse] Redundancy found! Collapsing child '{collapsedChildren[0].Name}' into parent '{node.Name}'. Returning child instead.");
                return collapsedChildren[0];
            }
                
            var newChildren = new List<OperationNode>();
            foreach (var child in collapsedChildren) {
                if (child.Name == node.Name) {
                    Log(() => $"[Collapse] Redundancy found! Absorbing children of '{child.Name}' into '{node.Name}'.");
                    newChildren.AddRange(child.Children);
                } else {
                    newChildren.Add(child);
                }
            }

            var finalNode = node with { Children = newChildren };
            Log(() => $"[Collapse] <== Returning node '{finalNode.Name}' with {finalNode.Children.Count} children.");
            return finalNode;
        }

        static OperationNode BuildFromGenericException(this Exception ex, IReadOnlyList<LogicalStackFrame> parentStack,
            Dictionary<AmbientFaultContext, FaultHubException> contextLookup) {
            Log(() => $"[Parser.Generic] Processing generic exception: {ex?.GetType().Name}");
            if (ex is not AggregateException aggEx) return ex is FaultHubException fhEx ? fhEx.Context.BuildFromContext(parentStack, contextLookup) : null;
            var children = aggEx.InnerExceptions.Select(e => e.BuildFromGenericException( parentStack,contextLookup)).Where(n => n != null).ToList();
            Log(() => $"[Parser.Generic] Parsed AggregateException into {children.Count} children.");
            return children.Count == 1 ? children.Single() : new OperationNode("Multiple Operations", [], children);

        }

        static OperationNode BuildFromContext(this AmbientFaultContext context,
            IReadOnlyList<LogicalStackFrame> parentStack,
            Dictionary<AmbientFaultContext, FaultHubException> contextLookup) {
                if (context == null) return null;
                Log(() => $"[Parser.Context] ==> Processing context for: '{context.BoundaryName ?? "NULL"}'");

                if (context.BoundaryName == null) {
                    Log(() => $"[Parser.Context] BoundaryName is null. Trying to build from generic inner exception.");
                    if (contextLookup.TryGetValue(context, out var owner)) {
                        return owner.InnerException.BuildFromGenericException(parentStack,contextLookup);
                    }
                    Log(() => $"[Parser.Context] Could not find owner for nameless context. Returning null.");
                    return null;
                }
                
                var localStack = context.LogicalStackTrace ?? Enumerable.Empty<LogicalStackFrame>();
                var fullStack = localStack.Concat(parentStack).Distinct().ToList();
                var children = new List<OperationNode>();
                
                Log(() => $"[Parser.Context] Recursing on InnerContext: '{context.InnerContext?.BoundaryName ?? "NULL"}'");
                var hierarchicalChild = context.InnerContext.BuildFromContext(fullStack,contextLookup);
                if (hierarchicalChild != null) {
                    children.Add(hierarchicalChild);
                }
                
                var resultNode = new OperationNode(context.BoundaryName, context.UserContext ?? [], children, context.RootCause( contextLookup, fullStack, children), fullStack);
                Log(() => $"[Parser.Context] <== Created node for '{resultNode.Name}' with {resultNode.Children.Count} children.");
                return resultNode;
            }

        private static Exception RootCause(this AmbientFaultContext context, Dictionary<AmbientFaultContext, FaultHubException> contextLookup, List<LogicalStackFrame> fullStack, List<OperationNode> children){
            Exception rootCause = null;
            if (!contextLookup.TryGetValue(context, out var contextOwner)) return null;
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
                var processedBoundaries = new HashSet<string>(children.Select(c => c.Name));
                foreach (var innerEx in aggEx.InnerExceptions.OfType<FaultHubException>()) {
                    var childContext = innerEx.Context;
                    while (childContext != null) {
                        if (processedBoundaries.Contains(childContext.BoundaryName)) break;
                        childContext = childContext.InnerContext;
                    }

                    if (childContext == null) {
                        var branchNode = innerEx.Context.BuildFromContext(fullStack, contextLookup);
                        if (branchNode == null) continue;
                        children.Add(branchNode);
                        processedBoundaries.Add(branchNode.Name);
                    }
                }
            }

            return rootCause;
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