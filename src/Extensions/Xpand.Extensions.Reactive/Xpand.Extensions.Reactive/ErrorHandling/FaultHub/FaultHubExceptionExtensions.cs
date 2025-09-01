using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xpand.Extensions.ExceptionExtensions;
using Xpand.Extensions.StringExtensions;
using static Xpand.Extensions.Reactive.ErrorHandling.FaultHub.FaultHubLogger;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub {
    public record OperationNode(string Name, IReadOnlyList<object> ContextData, IReadOnlyList<OperationNode> Children,
        Exception RootCause = null, IReadOnlyList<LogicalStackFrame> LogicalStack = null,IReadOnlyList<string> Tags = null
    );
    public static class FaultHubExceptionExtensions {
        public static string Render(this FaultHubException exception) {
            Log(() => "[Render] Starting exception rendering.");
            if (exception == null) {
                Log(() => "[Render] Exception is null, returning empty string.");
                return string.Empty;
            }

            var mergedTree = exception.OperationTree();
            if (mergedTree == null) {
                var message = exception.InnerException?.ToString() ?? exception.Message;
                Log(() => $"[Render] Merged tree is null. Returning base exception message: {message}");
                return message;
            }

            var sb = new StringBuilder();
            var rootContextItems = exception.BuildReportHeader(mergedTree, sb);
            mergedTree.BuildReportBody(rootContextItems, sb);
            var finalReport = sb.ToString().Trim();
            Log(() => "[Render] Rendering finished.");
            return finalReport;
        }
        
        private static void BuildReportBody(this OperationNode mergedTree, object[] rootContextItems, StringBuilder sb) {
            if (mergedTree.Children.Any()) {
                Log(() => "[Render] Rendering child nodes.");
                mergedTree.RenderChildNodes(rootContextItems, sb);
            }
            else if (mergedTree.GetRootCause() != null || (mergedTree.GetLogicalStack() != null && mergedTree.GetLogicalStack().Any())) {
                Log(() => "[Render] No children, appending root cause details directly.");
                AppendRootCauseDetails(mergedTree, sb, "  ");
            }
        }
        private static object[] BuildReportHeader(this FaultHubException exception, OperationNode mergedTree, StringBuilder sb) {
            var allRootCauses = exception.FindRootCauses().ToList();
            Log(() => $"[Render] Found {allRootCauses.Count} root causes.");

            var summary = string.Join(" • ", allRootCauses.Select(rc => rc.Message).Distinct());
            var title = mergedTree.Name.CompoundName();

            var rootContextItems = mergedTree.ContextData
                .Where(o => o is not string s ? o is not OperationNode
                    : !(mergedTree.Tags?.Contains(s) ?? false) && s.CompoundName() != title && s != title)
                .ToArray();

            var rootContext = rootContextItems.Any() ? $" [{string.Join(", ", rootContextItems)}]" : "";
            var errorMessage = exception.GenerateErrorMessage(mergedTree, title, allRootCauses, rootContext, summary);
            Log(() => $"[Render] Generated error message: '{errorMessage}'");

            sb.AppendLine(errorMessage);
            return rootContextItems;
        }
        private static string GenerateErrorMessage(this FaultHubException exception, OperationNode node, string title, List<Exception> allRootCauses, string rootContext, string summary) {
            var prefix = "";
            if (node?.Tags != null && node.Tags.Any()) {
                prefix = string.Join(" ", node.Tags.Select(s => s.FirstCharacterToUpper())) + ": ";
            }

            var summaryPart = allRootCauses.Count switch {
                0 => "",
                1 => $" ({summary})",
                _ => $" ({allRootCauses.Count} times: {summary})"
            };
            return $"{prefix}{title} {exception.ErrorStatus}{rootContext}{summaryPart}";
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
            return rootGroups.Count > 1 ? nodes.CreateVirtualRootForMultipleGroups() : rootGroups.Single().MergeNodeGroup();
        }        
        private static OperationNode MergeNodeGroup(this IGrouping<string, OperationNode> groupToMerge) {
            var representative = groupToMerge.First();
            Log(() => $"[Union] Merging {groupToMerge.Count()} nodes under root '{representative.Name}'.");
            var allChildren = groupToMerge.SelectMany(n => n.Children).ToArray();
            var mergedChildren = !allChildren.Any() ? new List<OperationNode>()
                : allChildren.GroupBy(c => c.Name).Select(childGroup => Union(childGroup.ToList())).ToList();
            var logicalStack = groupToMerge.SelectMany(n => n.LogicalStack ?? Enumerable.Empty<LogicalStackFrame>()).Distinct().ToList();
            var rootCause = groupToMerge.FirstOrDefault(n => n.RootCause != null)?.RootCause;
            return new OperationNode(representative.Name, representative.ContextData, mergedChildren, rootCause, logicalStack);
        }
        private static OperationNode CreateVirtualRootForMultipleGroups(this List<OperationNode> nodes) {
            Log(() => "[Union] Multiple root names found. Creating virtual 'Multiple Operations' root.");
            return new OperationNode("Multiple Operations", [], nodes);
        }
        private static string CompoundName(this string s) 
            => s == null ? null : Regex.Replace(s.ParseMemberName().Replace('_', ' '), @"(\B[A-Z])", " $1");

        internal static string ParseMemberName(this string s) {
            if (s == null) return null;

            var codePart = s.Split("=>").Last().Trim();
            
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
            var baseName = string.IsNullOrEmpty(codePart) ? s : codePart;

            return baseName.Split('(').First().Replace("()", "");
        }
        private static void RenderNode(this OperationNode node, OperationNode parent, List<OperationNode> ancestors, HashSet<object> titleContextItems, StringBuilder sb, string prefix, bool isLast, bool includeDetails) {
            var contextData = ancestors.ContextData(node, parent,  titleContextItems, includeDetails);
            var connector = isLast ? "└ " : "├ ";
            var nodePrefix =node.Tags != null && node.Tags.Any()?node.Tags.Contains(Transaction.StepNodeTag)? "Step: ":string.Join(" ", node.Tags.Select(s => s.FirstCharacterToUpper())) + ": ": "";
            Log(() => $"[RenderNode] Rendering '{node.Name}' with prefix '{prefix}', context '{contextData}'.");
            sb.Append(prefix).Append(connector).AppendLine(nodePrefix + node.Name.CompoundName() + contextData);
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
            if (!string.IsNullOrEmpty(stack)) sb.AppendLine(string.Join(Environment.NewLine, stack.Split(["\r\n", "\n"], StringSplitOptions.None).Select(l => childPrefix + "  " + l)));
            sb.Append(childPrefix).AppendLine("  --- Original Exception Details ---");
            using var reader = new System.IO.StringReader(node.RootCause.ToString());
            while (reader.ReadLine() is{ } line) {
                sb.Append(childPrefix).Append("    ").AppendLine(line);
            }
        }

        private static string ContextData(this List<OperationNode> ancestors, OperationNode node, OperationNode parent, HashSet<object> titleContextItems, bool includeDetails) {
            if (!includeDetails || !node.ContextData.Any()) return "";
            var filteredContext = ancestors.FilterContextItems( node, parent, titleContextItems);
            return filteredContext.Any() ? $" ({string.Join(", ", filteredContext)})" : "";
        }
        
        private static List<object> FilterContextItems(this List<OperationNode> ancestors, OperationNode node, OperationNode parent, HashSet<object> titleContextItems) {
            var knownTags = new HashSet<string>(node.Tags ?? Enumerable.Empty<string>());
            return node.ContextData.Where(c => node.ShouldKeepContextItem(c,  parent, ancestors, titleContextItems, knownTags)).ToList();
        }
        
        private static bool ShouldKeepContextItem(this OperationNode node,object c,  OperationNode parent, List<OperationNode> ancestors, HashSet<object> titleContextItems, HashSet<string> knownTags) {
            if (c == null || titleContextItems.Contains(c)) return false;
            if (ancestors.Any(a => a.ContextData.Contains(c))) {
                Log(() => $"[ContextData.Filter] Suppressing inherited context '{c}' from node '{node.Name}'.");
                return false;
            }
            if (c is not string contextString) {
                Log(() => $"[ContextData.Filter] Keeping non-string context '{c}'.");
                return true;
            }
            if (knownTags.Contains(contextString) || contextString.Contains(" - ") || node.IsRedundantWithNameInHierarchy(contextString,  parent)) {
                if (contextString.Contains(" - ")) Log(() => $"[ContextData.Filter] Removing '{contextString}' (matches structural noise pattern 'A - B').");
                return false;
            }
            Log(() => $"[ContextData.Filter] Keeping '{contextString}'.");
            return true;
        }
        private static bool IsRedundantWithNameInHierarchy(this OperationNode node,string contextString,  OperationNode parent) {
            var contextCompoundName = contextString.CompoundName();
            if (contextCompoundName == node.Name.CompoundName()) {
                Log(() => $"[ContextData.Filter] Removing '{contextString}' (matches node name '{node.Name}').");
                return true;
            }

            if (parent == null) return false;
            if (contextCompoundName == parent.Name.CompoundName()) {
                Log(() => $"[ContextData.Filter] Removing '{contextString}' (matches parent name '{parent.Name}').");
                return true;
            }
            foreach (var sibling in parent.Children) {
                if (sibling != node && contextCompoundName == sibling.Name.CompoundName()) {
                    Log(() => $"[ContextData.Filter] Removing '{contextString}' (matches sibling name '{sibling.Name}').");
                    return true;
                }
            }
            return false;
        }
        public static OperationNode GetChild(this OperationNode node, string name) 
            => node.Children.Single(c => c.Name == name);
        public static OperationNode OperationTree(this FaultHubException topException) {
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
            Log(() => $"[Collapse] ==> Processing node '{node.Name}' with {node.Children.Count} children.");
            return node.ProcessCollapsedChildren( node.Children.Select(c => c.Collapse()).Where(c => c != null));
        }
        
        private static OperationNode ProcessCollapsedChildren(this OperationNode originalNode, IEnumerable<OperationNode> collapsedChildren) {
            var currentNode = originalNode;
            var finalChildren = new List<OperationNode>();
            foreach (var child in collapsedChildren) {
                if (currentNode.IsChildRedundant( child)) {
                    currentNode = currentNode.MergeWithRedundantChild(child);
                    finalChildren.AddRange(child.Children);
                } else {
                    finalChildren.Add(child);
                }
            }
            var finalNode = currentNode with { Children = finalChildren.Distinct().ToList() };
            Log(() => $"[Collapse] <== Returning node '{finalNode.Name}' with {finalNode.Children.Count} children. Tags: [{(finalNode.Tags != null ? string.Join(", ", finalNode.Tags) : "null")}]");
            return finalNode;
        }
        
        private static OperationNode MergeWithRedundantChild(this OperationNode parent, OperationNode redundantChild) {
            Log(() => $"[Collapse] Redundancy found! Merging child '{redundantChild.Name}' into parent '{parent.Name}'.");
            var mergedTags = (parent.Tags ?? []).Concat(redundantChild.Tags ?? []).Distinct().ToList();
            var mergedContext = parent.ContextData.Concat(redundantChild.ContextData).Distinct().ToArray();
            var mergedStack = (parent.LogicalStack ?? Enumerable.Empty<LogicalStackFrame>())
                .Concat(redundantChild.LogicalStack ?? Enumerable.Empty<LogicalStackFrame>())
                .Distinct().ToList();
            var mergedRootCause = parent.RootCause ?? redundantChild.RootCause;
            
            return parent with { 
                Tags = mergedTags, 
                ContextData = mergedContext, 
                LogicalStack = mergedStack, 
                RootCause = mergedRootCause 
            };
        }
        private static bool IsChildRedundant(this OperationNode parent, OperationNode child) 
            => parent.Name.Split('.').Last() == child.Name.Split('.').Last();
        
        static OperationNode BuildFromGenericException(this Exception ex, IReadOnlyList<LogicalStackFrame> parentStack,
            Dictionary<AmbientFaultContext, FaultHubException> contextLookup) {
            Log(() => $"[Parser.Generic] Processing generic exception: {ex?.GetType().Name}");
            if (ex is not AggregateException aggEx) return ex is FaultHubException fhEx ? fhEx.Context.BuildFromContext(parentStack, contextLookup) : null;
            
            var children = aggEx.InnerExceptions.Select(e => e.BuildFromGenericException( [],contextLookup)).Where(n => n != null).ToList();
            Log(() => $"[Parser.Generic] Parsed AggregateException into {children.Count} children.");
            return children.Count == 1 ? children.Single() : new OperationNode("Multiple Operations", [], children);
        }

        static OperationNode BuildFromContext(this AmbientFaultContext context, IReadOnlyList<LogicalStackFrame> parentStack, Dictionary<AmbientFaultContext, FaultHubException> contextLookup) {
            if (context == null) return null;
            Log(() => $"[Parser.Context] ==> Processing context for: '{context.BoundaryName ?? "NULL"}'");
            return context.BoundaryName == null ? context.HandleNamelessContext( parentStack, contextLookup) : context.BuildNamedNode( parentStack, contextLookup);
        }
        
        private static OperationNode BuildNamedNode(this AmbientFaultContext context, IReadOnlyList<LogicalStackFrame> parentStack, Dictionary<AmbientFaultContext, FaultHubException> contextLookup) {
            var localStack = context.LogicalStackTrace ?? Enumerable.Empty<LogicalStackFrame>();
            var fullStack = localStack.Concat(parentStack).Distinct().ToList();
            var children = new List<OperationNode>();
            Log(() => $"[Parser.Context] Recursing on InnerContext: '{context.InnerContext?.BoundaryName ?? "NULL"}'");
            var hierarchicalChild = context.InnerContext.BuildFromContext(fullStack, contextLookup);
            if (hierarchicalChild != null) children.Add(hierarchicalChild);
            var rootCause = context.RootCause(contextLookup, fullStack, children);
            var resultNode = new OperationNode(context.BoundaryName, context.UserContext ?? [], children, rootCause, fullStack, context.Tags);
            Log(() => $"[INSTRUMENTATION][BuildFromContext] Created node '{resultNode.Name}' with Tags: [{(resultNode.Tags != null ? string.Join(", ", resultNode.Tags) : "null")}]");
            Log(() => $"[Parser.Context] <== Created node for '{resultNode.Name}' with {resultNode.Children.Count} children.");
            return resultNode;
        }
        private static OperationNode HandleNamelessContext(this AmbientFaultContext context, IReadOnlyList<LogicalStackFrame> parentStack, Dictionary<AmbientFaultContext, FaultHubException> contextLookup) {
            Log(() => "[Parser.Context] BoundaryName is null. Trying to build from generic inner exception.");
            if (contextLookup.TryGetValue(context, out var owner)) return owner.InnerException.BuildFromGenericException(parentStack, contextLookup);
            Log(() => "[Parser.Context] Could not find owner for nameless context. Returning null.");
            return null;
        }
        private static Exception RootCause(this AmbientFaultContext context, Dictionary<AmbientFaultContext, FaultHubException> contextLookup, List<LogicalStackFrame> fullStack, List<OperationNode> children) {
            if (!contextLookup.TryGetValue(context, out var contextOwner)) return null;
            Log(() => $"[RootCause] Analyzing context '{context.BoundaryName}' owned by exception '{contextOwner.Message}'.");
            if (context.InnerContext == null) return contextOwner.ProcessLeafContext( fullStack, contextLookup, children);
            if (contextOwner.InnerException is AggregateException aggEx) aggEx.ProcessAggregateBranches( fullStack, contextLookup, children);
            return null;
        }
        
        private static Exception ProcessLeafContext(this FaultHubException contextOwner, List<LogicalStackFrame> fullStack, Dictionary<AmbientFaultContext, FaultHubException> contextLookup, List<OperationNode> children) {
            Log(() => "[RootCause] No inner context. Building from owner's inner exception.");
            var errorNode = contextOwner.InnerException.BuildFromGenericException(fullStack, contextLookup);
            if (errorNode != null) {
                if (errorNode.Name == "Multiple Operations") {
                    Log(() => "[RootCause] Error node is 'Multiple Operations'. Adding its children directly.");
                    children.AddRange(errorNode.Children);
                } else {
                    Log(() => $"[RootCause] Adding single error node '{errorNode.Name}'.");
                    children.Add(errorNode);
                }
            }
            if (contextOwner.InnerException is not (null or FaultHubException or AggregateException)) {
                Log(() => $"[RootCause] Found primitive root cause: {contextOwner.InnerException.GetType().Name}");
                return contextOwner.InnerException;
            }
            return null;
        }
        
        private static void ProcessAggregateBranches(this AggregateException aggEx, List<LogicalStackFrame> fullStack, Dictionary<AmbientFaultContext, FaultHubException> contextLookup, List<OperationNode> children) {
            Log(() => "[RootCause] Owner's inner exception is AggregateException. Processing branches.");
            var processedBoundaries = new HashSet<string>(children.Select(c => c.Name));
            foreach (var innerEx in aggEx.InnerExceptions.OfType<FaultHubException>()) {
                var childContext = innerEx.Context;
                bool isAlreadyProcessed = false;
                while (childContext != null) {
                    if (processedBoundaries.Contains(childContext.BoundaryName)) {
                        isAlreadyProcessed = true;
                        break;
                    }
                    childContext = childContext.InnerContext;
                }
                if (isAlreadyProcessed) continue;
                Log(() => $"[RootCause] Unprocessed branch found: '{innerEx.Context.BoundaryName}'. Building its node.");
                var branchNode = innerEx.Context.BuildFromContext(fullStack, contextLookup);
                if (branchNode == null) continue;
                children.Add(branchNode);
                processedBoundaries.Add(branchNode.Name);
            }
        }

        public static IEnumerable<Exception> FindRootCauses(this FaultHubException e) {
            Log(() => $"[FindRootCauses] Traversing exception: {e?.GetType().Name}");
            return e.SelectMany()
                .Where(ex => ex is not AggregateException && ex is not FaultHubException { InnerException: not null });
        }

        
    }
}