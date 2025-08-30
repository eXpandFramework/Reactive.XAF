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
            Log(() => "[Render] Starting exception rendering.");
            if (exception == null) {
                Log(() => "[Render] Exception is null, returning empty string.");
                return string.Empty;
            }
            var mergedTree = exception.NewOperationTree();
            if (mergedTree == null) {
                var message = exception.InnerException?.ToString() ?? exception.Message;
                Log(() => $"[Render] Merged tree is null. Returning base exception message: {message}");
                return message;
            }
            var sb = new StringBuilder();
            var allRootCauses = exception.FindRootCauses().ToList();
            Log(() => $"[Render] Found {allRootCauses.Count} root causes.");
            var summary = string.Join(" • ", allRootCauses.Select(rc => rc.Message).Distinct());
            var title = mergedTree.Name.CompoundName();
            var rootContextItems = mergedTree.ContextData
                .Where(o => o is not OperationNode && o?.ToString().CompoundName() != title&&o?.ToString() != title)
                .ToArray();
            var rootContext = rootContextItems.Any() ? $" [{string.Join(", ", rootContextItems)}]" : "";
            var errorMessage = exception.GenerateErrorMessage( title, allRootCauses, rootContext, summary);
            Log(() => $"[Render] Generated error message: '{errorMessage}'");
            sb.AppendLine(errorMessage);
            if (mergedTree.Children.Any()) {
                Log(() => "[Render] Rendering child nodes.");
                mergedTree.RenderChildNodes(rootContextItems, sb);
            }
            else if (mergedTree.GetRootCause() != null || (mergedTree.GetLogicalStack() != null && mergedTree.GetLogicalStack().Any())) {
                Log(() => "[Render] No children, appending root cause details directly.");
                AppendRootCauseDetails(mergedTree, sb, "  ");
            }
            var finalReport = sb.ToString().Trim();
            Log(() => "[Render] Rendering finished.");
            return finalReport;
        }

        private static string GenerateErrorMessage(this FaultHubException exception, string title, List<Exception> allRootCauses, string rootContext, string summary) {
            
            var summaryPart = allRootCauses.Any() ? $" ({allRootCauses.Count} times: {summary})" : "";
            return $"{title} {exception.ErrorStatus}{rootContext}{summaryPart}";
            
        }   

        private static void RenderChildNodes(this OperationNode mergedTree,object[] rootContextItems,  StringBuilder sb){
            Log(()=>$"[RenderChildNodes] Rendering {mergedTree.Children.Count} children for node '{mergedTree.Name}'.");
            var titleContextItems = new HashSet<object>(rootContextItems);
            for (var i = 0; i < mergedTree.Children.Count; i++) {
                var child = mergedTree.Children[i];
                var isLastChild = i == mergedTree.Children.Count - 1;
                child.RenderNode(mergedTree, new List<OperationNode>(), titleContextItems, sb, "", isLastChild, true);
            }
        }

        public static string RenderStack(this OperationNode node) {
            Log(() => $"[RenderStack] Called for node '{node?.Name ?? "null"}'.");
            var logicalStack = node?.GetLogicalStack();
            if (logicalStack == null || !logicalStack.Any()) {
                Log(() => "[RenderStack] No logical stack found, returning empty string.");
                return string.Empty;
            }
            var blacklistedPatterns = FaultHub.BlacklistedFilePathRegexes;
            Log(() => blacklistedPatterns.Any()
                ? $"[RenderStack] Found {blacklistedPatterns.Count} blacklist patterns. Using filtering."
                : "[RenderStack] No blacklist patterns. Using simple render.");
            if (!blacklistedPatterns.Any()) return logicalStack.RenderSimpleStack();
            var sb = new StringBuilder();
            sb.AppendLine("--- Invocation Stack ---");
            sb.BuildRenderedStackString( logicalStack,blacklistedPatterns);
            return sb.ToString().TrimEnd();
        }

        private static void BuildRenderedStackString(this StringBuilder sb, IReadOnlyList<LogicalStackFrame> originalStack, Dictionary<string, string> blacklistedPatterns) {
            Log(()=>$"[BuildRenderedStackString] Filtering stack with {originalStack.Count} frames.");
            var filteredItems = originalStack.FilterAndGroupBlacklistedFrames( blacklistedPatterns);
            if (!filteredItems.OfType<LogicalStackFrame>().Any() && originalStack.Any()) {
                Log(()=>"[BuildRenderedStackString] Blacklist would hide all frames. Falling back to full stack.");
                sb.RenderFallbackStack( originalStack);
            }
            else {
                Log(()=>"[BuildRenderedStackString] Rendering filtered stack.");
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
            => node.RootCause ??
            node.Children.Select(child => child.GetRootCause()).FirstOrDefault(ex => ex != null);


        public static string Render(this OperationNode rootNode,bool includeDetails = false) {
            Log(() => $"[Render.OperationNode] Rendering node '{rootNode?.Name ?? "null"}' with includeDetails={includeDetails}.");
            if (rootNode == null) return string.Empty;
            var sb = new StringBuilder();
            rootNode.RenderNode(null, new List<OperationNode>(), new HashSet<object>(), sb, "", true, includeDetails);
            return sb.ToString().TrimEnd();
        }

        public static OperationNode Union(this IEnumerable<OperationNode> source) {
            var nodes = source?.ToList();
            Log(() => $"[Union] Starting union of {nodes?.Count ?? 0} nodes.");
            if (nodes == null || !nodes.Any()) return null;
            if (nodes.Count == 1) return nodes.Single();
            var rootGroups = nodes.GroupBy(n => n.Name).ToList();
            if (rootGroups.Count > 1) {
                Log(() => "[Union] Multiple root names found. Creating virtual 'Multiple Operations' root.");
                return new OperationNode("Multiple Operations", [], nodes);
            }
            var groupToMerge = rootGroups.Single();
            var representative = groupToMerge.First();
            Log(() => $"[Union] Merging {groupToMerge.Count()} nodes under root '{representative.Name}'.");
            var allChildren = groupToMerge.SelectMany(n => n.Children).ToArray();
            var logicalStack = groupToMerge.SelectMany(n => n.LogicalStack ?? Enumerable.Empty<LogicalStackFrame>()).Distinct().ToList();
            return new OperationNode(representative.Name, representative.ContextData, allChildren.Any()
                    ? allChildren.GroupBy(c => c.Name).Select(childGroup => Union(childGroup.ToList())).ToList()
                    : new List<OperationNode>(), groupToMerge.FirstOrDefault(n => n.RootCause != null)?.RootCause,
                logicalStack);
        }
        
        private static string CompoundName(this string s) 
            => s == null ? null : Regex.Replace(s.Split('(').First().Replace("()", "").Replace('_', ' '), @"(\B[A-Z])", " $1");

        private static void RenderNode(this OperationNode node, OperationNode parent, List<OperationNode> ancestors, HashSet<object> titleContextItems, StringBuilder sb, string prefix, bool isLast, bool includeDetails) {
            var contextData = ancestors.ContextData(node, parent,  titleContextItems, includeDetails);
            var connector = isLast ? "└ " : "├ ";
            Log(() => $"[RenderNode] Rendering '{node.Name}' with prefix '{prefix}', context '{contextData}'.");
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
            Log(()=>$"[AppendRootCauseDetails] Appending details for '{node.Name}'. Root cause: {node.RootCause?.GetType().Name}");
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
                var filteredContext = node.ContextData
                    .Where(c => {
                        if (c == null) return false;
                        if (titleContextItems.Contains(c)) return false;
                        if (c is not string contextString) {
                            Log(() => $"[ContextData.Filter] Keeping non-string context '{c}'.");
                            return true;
                        }

                        var nodeCompoundName = node.Name.CompoundName();
                        var parentCompoundName = parent?.Name.CompoundName();
                        var contextCompoundName = contextString.CompoundName();

                        if (contextCompoundName == nodeCompoundName) {
                            Log(() => $"[ContextData.Filter] Removing '{contextString}' (matches node name '{node.Name}').");
                            return false;
                        }

                        if (parentCompoundName != null) {
                            if (contextCompoundName == parentCompoundName) {
                                Log(() => $"[ContextData.Filter] Removing '{contextString}' (matches parent name '{parent.Name}').");
                                return false;
                            }
                            foreach (var sibling in parent.Children) {
                                if (sibling != node && contextCompoundName == sibling.Name.CompoundName()) {
                                    Log(() => $"[ContextData.Filter] Removing '{contextString}' (matches sibling name '{sibling.Name}').");
                                    return false;
                                }
                            }
                        }

                        if (contextString.Contains(" - ")) {
                            Log(() => $"[ContextData.Filter] Removing '{contextString}' (matches structural noise pattern 'A - B').");
                            return false;
                        }

                        Log(() => $"[ContextData.Filter] Keeping '{contextString}'.");
                        return true;
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
            return collapsedTree;
        }

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
                var child = collapsedChildren[0];
                Log(() => $"[Collapse] Redundancy found! Merging child '{child.Name}' into parent '{node.Name}'.");
                var mergedContext = node.ContextData.Concat(child.ContextData).Distinct().ToArray();
                var mergedStack = (node.LogicalStack ?? Enumerable.Empty<LogicalStackFrame>()).Concat(child.LogicalStack ?? Enumerable.Empty<LogicalStackFrame>()).Distinct().ToList();
                return node with { Children = child.Children, ContextData = mergedContext, LogicalStack = mergedStack };
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
            
            var children = aggEx.InnerExceptions.Select(e => e.BuildFromGenericException( [],contextLookup)).Where(n => n != null).ToList();
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
                
                var localStack = context.LogicalStackTrace ??
                Enumerable.Empty<LogicalStackFrame>();
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
            Log(()=>$"[RootCause] Analyzing context '{context.BoundaryName}' owned by exception '{contextOwner.Message}'.");
            if (context.InnerContext == null) {
                Log(()=>"[RootCause] No inner context. Building from owner's inner exception.");
                var errorNode = contextOwner.InnerException.BuildFromGenericException(fullStack,contextLookup);
                if (errorNode != null) {
                    if (errorNode.Name == "Multiple Operations") {
                        Log(()=>"[RootCause] Error node is 'Multiple Operations'. Adding its children directly.");
                        children.AddRange(errorNode.Children);
                    }
                    else {
                        Log(()=>$"[RootCause] Adding single error node '{errorNode.Name}'.");
                        children.Add(errorNode);
                    }
                }
                if (contextOwner.InnerException is not (null or FaultHubException or AggregateException)) {
                    Log(()=>$"[RootCause] Found primitive root cause: {contextOwner.InnerException.GetType().Name}");
                    rootCause = contextOwner.InnerException;
                }
            } 
            else if (contextOwner.InnerException is AggregateException aggEx) {
                Log(()=>"[RootCause] Owner's inner exception is AggregateException. Processing branches.");
                var processedBoundaries = new HashSet<string>(children.Select(c => c.Name));
                foreach (var innerEx in aggEx.InnerExceptions.OfType<FaultHubException>()) {
                    var childContext = innerEx.Context;
                    while (childContext != null) {
                        if (processedBoundaries.Contains(childContext.BoundaryName)) break;
                        childContext = childContext.InnerContext;
                    }

                    if (childContext == null) {
                        Log(()=>$"[RootCause] Unprocessed branch found: '{innerEx.Context.BoundaryName}'. Building its node.");
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
            Log(() => $"[FindRootCauses] Traversing exception: {ex?.GetType().Name}");
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