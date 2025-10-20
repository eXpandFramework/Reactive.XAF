using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xpand.Extensions.ExceptionExtensions;
using Xpand.Extensions.LinqExtensions;
using static Xpand.Extensions.Reactive.Relay.FaultHubException;

namespace Xpand.Extensions.Reactive.Relay {
    public record OperationNode(string Name, IReadOnlyList<object> ContextData, IReadOnlyList<OperationNode> Children,
        Exception RootCause = null, IReadOnlyList<LogicalStackFrame> LogicalStack = null,IReadOnlyList<string> Tags = null
    );
    public static class FaultHubExceptionExtensions {
        public static IEnumerable<OperationNode> Descendants(this OperationNode root) 
            => root.SelectManyRecursive(node => node.Children);
        public static string Render(this FaultHubException exception) {
            LogFast($"Starting exception rendering.");
            if (exception == null) {
                LogFast($"Exception is null, returning empty string.");
                return string.Empty;
            }

            var mergedTree = exception.OperationTree();
            if (mergedTree == null) {
                var message = exception.InnerException?.ToString() ?? exception.Message;
                LogFast($"Merged tree is null. Returning base exception message: {message}");
                return message;
            }

            var sb = new StringBuilder();
            var rootContextItems = exception.BuildReportHeader(mergedTree, sb);
            mergedTree.BuildReportBody(rootContextItems, sb);
            var finalReport = sb.ToString().Trim();
            LogFast($"Rendering finished.");
            return finalReport;
        }
        
        private static void BuildReportBody(this OperationNode mergedTree, object[] rootContextItems, StringBuilder sb) {
            if (mergedTree.Children.Any()) {
                mergedTree.RenderChildNodes(rootContextItems, sb);
            }
            else if (mergedTree.GetRootCause() != null || (mergedTree.GetLogicalStack() != null && mergedTree.GetLogicalStack().Any())) {
                LogFast($"No children, appending root cause details directly.");
                AppendRootCauseDetails(mergedTree, sb, "  ");
            }
        }
        private static object[] BuildReportHeader(this FaultHubException exception, OperationNode mergedTree, StringBuilder sb) {
            var allRootCauses = exception.FindRootCauses().ToList();
            LogFast($"Found {allRootCauses.Count} root causes.");
            var summary = string.Join(" • ", allRootCauses.Select(rc => rc.Message).Distinct());
            var title = mergedTree.Name.CompoundName();
            var rootContextItems = mergedTree.ExtractRootContext(title);
            var rootContextString = string.Join(", ", rootContextItems.Select(o => o.ToString()).Where(s => !string.IsNullOrWhiteSpace(s) && s != "()"));
            var rootContext = !string.IsNullOrEmpty(rootContextString) ? $" ({rootContextString})" : "";
            var errorMessage = exception.GenerateErrorMessage(mergedTree, title, allRootCauses, rootContext, summary);
            LogFast($"Generated error message: '{errorMessage}'");
            sb.AppendLine(errorMessage);
            return rootContextItems;
        }
        private static string GenerateErrorMessage(this FaultHubException exception, OperationNode node, string title, List<Exception> allRootCauses, string rootContext, string summary) {
            var tags = node?.Tags;
            if (tags == null || !tags.Any()) {
                tags = exception.SelectMany().OfType<FaultHubException>()
                    .FirstOrDefault(fh => fh.Context.Tags is { Count: > 0 })?.Context.Tags;
            }
            var displayableTags = tags?.Where(t => !t.StartsWith("_")).ToList();
            var tagsPart = (displayableTags is { Count: > 0 }) ? $" [{string.Join(", ", displayableTags)}]" : "";
            var summaryPart = allRootCauses.Count switch {
                0 => "",
                1 => $" <{summary}>",
                _ => $" <{allRootCauses.Count} times: {summary}>"
            };
            return $"{title} {exception.ErrorStatus}{rootContext}{tagsPart}{summaryPart}";
            
        }
        private static void RenderChildNodes(this OperationNode mergedTree,object[] rootContextItems,  StringBuilder sb){
            var titleContextItems = new HashSet<object>(rootContextItems);
            for (var i = 0; i < mergedTree.Children.Count; i++) {
                var child = mergedTree.Children[i];
                var isLastChild = i == mergedTree.Children.Count - 1;
                child.RenderNode(mergedTree, new List<OperationNode>(), titleContextItems, sb, "", isLastChild, true);
            }
        }

        public static string RenderStack(this OperationNode node) {
            LogFast($"Called for node '{node?.Name ?? "null"}'.");
            var logicalStack = node?.GetLogicalStack();
            if (logicalStack == null || !logicalStack.Any()) {
                LogFast($"No logical stack found, returning empty string.");
                return string.Empty;
            }
            var blacklistedPatterns = FaultHub.BlacklistedFilePathRegexes;
            if (!blacklistedPatterns.Any()) return logicalStack.RenderSimpleStack();
            var sb = new StringBuilder();
            sb.AppendLine("--- Invocation Stack ---");
            sb.BuildRenderedStackString( logicalStack,blacklistedPatterns);
            return sb.ToString().TrimEnd();        }

        public record GroupedStackFrame(LogicalStackFrame Frame, int Count);

        private static List<object> GroupConsecutiveFrames(this IReadOnlyList<LogicalStackFrame> stack) {
            var grouped = new List<object>();
            if (stack == null || !stack.Any()) {
                return grouped;
            }

            using var enumerator = stack.GetEnumerator();
            if (!enumerator.MoveNext()) return grouped;
    
            var currentFrame = enumerator.Current;
            var consecutiveCount = 1;

            while (enumerator.MoveNext()) {
                if (enumerator.Current.Equals(currentFrame)) {
                    consecutiveCount++;
                } else {
                    grouped.Add(consecutiveCount > 1 ? new GroupedStackFrame(currentFrame, consecutiveCount) : currentFrame);
                    currentFrame = enumerator.Current;
                    consecutiveCount = 1;
                }
            }
            grouped.Add(consecutiveCount > 1 ? new GroupedStackFrame(currentFrame, consecutiveCount) : currentFrame);
            return grouped;
        }
        private static object[] ExtractRootContext(this OperationNode mergedTree, string title) 
            => mergedTree.ContextData.ExceptType(typeof(IMetadataToken))
                .Where(o => {
                    if (o is OperationNode) return false;
                    if (o is not string s) return true;
                    return !(mergedTree.Tags?.Contains(s) ?? false) && s.CompoundName() != title && s != title;
                })
                .ToArray();

        private static void RenderStackContent(this StringBuilder sb, IReadOnlyList<LogicalStackFrame> originalStack, List<object> filteredItems) {
            if (!filteredItems.Any(item => item is LogicalStackFrame || item is GroupedStackFrame) && originalStack.Any()) {
                LogFast($"Blacklist would hide all frames. Falling back to full stack.");
                sb.RenderFallbackStack(originalStack);
            }
            else {
                LogFast($"Rendering filtered stack.");
                sb.RenderFilteredStack(filteredItems);
            }
        }
        private static void BuildRenderedStackString(this StringBuilder sb, IReadOnlyList<LogicalStackFrame> originalStack, Dictionary<string, string> blacklistedPatterns) {
            var groupedStack = originalStack.GroupConsecutiveFrames();
            var filteredItems = groupedStack.FilterAndGroupBlacklistedFrames(blacklistedPatterns);
            sb.RenderStackContent(originalStack, filteredItems);
            sb.AppendBlacklistFooter(filteredItems, blacklistedPatterns);
        }

        private static void RenderFilteredStack(this StringBuilder sb, List<object> filteredItems) {
            foreach (var item in filteredItems) {
                if (item is LogicalStackFrame frame) {
                    sb.Append("  ").AppendLine(frame.ToString());
                }
                else if (item is GroupedStackFrame groupedFrame) {
                    sb.Append("  ").AppendLine($"{groupedFrame.Frame} ({groupedFrame.Count} similar calls)");
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

        private static List<object> FilterAndGroupBlacklistedFrames(this List<object> groupedStack, Dictionary<string, string> blacklistedPatterns) {
            var filteredItems = new List<object>();
            int consecutiveHiddenCount = 0;

            foreach (var item in groupedStack) {
                LogicalStackFrame? frame = item switch {
                    LogicalStackFrame f => f,
                    GroupedStackFrame gf => gf.Frame,
                    _ => null
                };

                if (frame != null && !string.IsNullOrEmpty(frame.Value.FilePath) && blacklistedPatterns.Keys.Any(pattern => Regex.IsMatch(frame.Value.FilePath, pattern, RegexOptions.IgnoreCase))) {
                    consecutiveHiddenCount += item switch {
                        LogicalStackFrame => 1,
                        GroupedStackFrame gf => gf.Count,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                }
                else {
                    if (consecutiveHiddenCount > 0) {
                        filteredItems.Add(consecutiveHiddenCount);
                        consecutiveHiddenCount = 0;
                    }
                    filteredItems.Add(item);
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
            LogFast($"Rendering node '{rootNode?.Name ?? "null"}' with includeDetails={includeDetails}.");
            if (rootNode == null) return string.Empty;
            var sb = new StringBuilder();
            rootNode.RenderNode(null, new List<OperationNode>(), new HashSet<object>(), sb, "", true, includeDetails);
            return sb.ToString().TrimEnd();
        }

        public static OperationNode Union(this IEnumerable<OperationNode> source) {
            var nodes = source?.ToList();
            LogFast($"Starting union of {nodes?.Count ?? 0} nodes.");
            if (nodes == null || !nodes.Any()) return null;
            if (nodes.Count == 1) return nodes.Single();
            var rootGroups = nodes.GroupBy(n => n.Name).ToList();
            return rootGroups.Count > 1 ? nodes.CreateVirtualRootForMultipleGroups() : rootGroups.Single().MergeNodeGroup();
        }        
        private static OperationNode MergeNodeGroup(this IGrouping<string, OperationNode> groupToMerge) {
            var representative = groupToMerge.First();
            var allChildren = groupToMerge.SelectMany(n => n.Children).ToArray();
            var mergedChildren = !allChildren.Any() ? new List<OperationNode>()
                : allChildren.GroupBy(c => c.Name).Select(childGroup => Union(childGroup.ToList())).ToList();
            var logicalStack = groupToMerge.SelectMany(n => n.LogicalStack ?? Enumerable.Empty<LogicalStackFrame>()).Distinct().ToList();
            var rootCause = groupToMerge.FirstOrDefault(n => n.RootCause != null)?.RootCause;
            return new OperationNode(representative.Name, representative.ContextData, mergedChildren, rootCause, logicalStack);
            
        }
        private static OperationNode CreateVirtualRootForMultipleGroups(this List<OperationNode> nodes) {
            LogFast($"Multiple root names found. Creating virtual 'Multiple Operations' root.");
            return new OperationNode("Multiple Operations", [], nodes);
        }
        private static string CompoundName(this string s) 
            => s == null ? null : Regex.Replace(s.ParseMemberName().Replace('_', ' '), @"(\B[A-Z])", " $1");

        internal static string ParseMemberName(this string s) {
            if (s == null) return null;
            var codePart = s.Split("=>").Last().Trim();
            var arrayMatch = Regex.Match(codePart, @"^new\s*\[\s*\]\s*{\s*(?<content>.*?)\s*}(?<accessor>\[\d+\])?\s*$");
            if (arrayMatch.Success) {
                codePart = arrayMatch.Groups["content"].Value.Trim();
            }
            var indexerMatch = Regex.Match(codePart, @"^(.*?)\[.*\]\s*$");
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
            var displayableTags = node.Tags?.Where(t => !t.StartsWith(SystemTag)).ToList();
            var tagsSuffix = (displayableTags?.Any() ?? false) ? $" [{string.Join(", ", displayableTags)}]" : "";
            sb.Append(prefix).Append(connector).AppendLine(node.Name.CompoundName() + tagsSuffix + contextData);
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
            LogFast($"Appending details for '{node.Name}'. Root cause: {node.RootCause?.GetType().Name}");
            sb.Append(childPrefix).Append("• Root Cause: ").Append(node.RootCause?.GetType().FullName).Append(": ").AppendLine(node.RootCause?.Message);
            var stack = node.RenderStack();
            if (!string.IsNullOrEmpty(stack)) sb.AppendLine(string.Join(Environment.NewLine, stack.Split(["\r\n", "\n"], StringSplitOptions.None).Select(l => childPrefix + "  " + l)));
            sb.Append(childPrefix).AppendLine("  --- Original Exception Details ---");
            using var reader = new System.IO.StringReader(node.RootCause!.ToString());
            while (reader.ReadLine() is{ } line) {
                sb.Append(childPrefix).Append("    ").AppendLine(line);
            }
        }

        private static string ContextData(this List<OperationNode> ancestors, OperationNode node, OperationNode parent, HashSet<object> titleContextItems, bool includeDetails) {
            if (!includeDetails || !node.ContextData.ExceptType(typeof(IMetadataToken)).Any()) return "";
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
                return false;
            }

            if (c is IMetadataToken) {
                return false;
            }
            if (c is not string contextString) {
                return true;
            }
            if (knownTags.Contains(contextString) || contextString.Contains(" - ") || node.IsRedundantWithNameInHierarchy(contextString,  parent)) {
                return false;
            }
            return true;
            
        }
        private static bool IsRedundantWithNameInHierarchy(this OperationNode node,string contextString,  OperationNode parent) {
            var contextCompoundName = contextString.CompoundName();
            if (contextCompoundName == node.Name.CompoundName()) {
                return true;
            }

            if (parent == null) return false;
            if (contextCompoundName == parent.Name.CompoundName()) {
                return true;
            }
            foreach (var sibling in parent.Children) {
                if (sibling != node && contextCompoundName == sibling.Name.CompoundName()) {
                    return true;
                }
            }
            return false;
            
        }
        public static OperationNode GetChild(this OperationNode node, string name) 
            => node.Children.Single(c => c.Name == name);
        public static OperationNode OperationTree(this FaultHubException topException) {
            LogFast($"Starting NewOperationTree parser...");
            var contextLookup = topException.FaultHubExceptions();
            var rawTree = topException.Context.BuildFromContext([],contextLookup);
            LogFast($"Raw tree built. Starting collapse phase...");
            var collapsedTree = rawTree.Collapse();
            LogFast($"Collapse phase finished.");
            return collapsedTree;
        }

        private static Dictionary<AmbientFaultContext, FaultHubException> FaultHubExceptions(this FaultHubException topException){
            
            var lookup = new Dictionary<AmbientFaultContext, FaultHubException>();
            var queue = new Queue<Exception>();
            if (topException != null) queue.Enqueue(topException);
            var visited = new HashSet<Exception>();
            while (queue.Count > 0) {
                var ex = queue.Dequeue();
                if (!visited.Add(ex)) continue;
                if (ex is FaultHubException { Context: not null } fhEx) lookup.TryAdd(fhEx.Context, fhEx);
                if (ex.InnerException != null) queue.Enqueue(ex.InnerException);
                if (ex is not AggregateException aggEx) continue;
                foreach (var inner in aggEx.InnerExceptions) {
                    queue.Enqueue(inner);
                }
            }
            LogFast($"Created context lookup dictionary with {lookup.Count} entries.");
            return lookup;
            
        }

        static OperationNode Collapse(this OperationNode node) {
            if (node == null) return null;
            var processedChildren = node.ProcessChildren();
            var childToMerge = node.FindMergableChild(processedChildren);
            if (childToMerge != null) {
                return node.MergeWithChild(childToMerge, processedChildren).Collapse();
            }
            var finalNode = node with { Children = processedChildren };
            return finalNode;
        }

        private static OperationNode FindMergableChild(this OperationNode node, IReadOnlyList<OperationNode> processedChildren) 
            => processedChildren.FirstOrDefault(c =>
                c.Name.CompoundName() == node.Name.CompoundName() || node.Name.CompoundName().EndsWith($".{c.Name.CompoundName()}") || c.Name.CompoundName().EndsWith($".{node.Name.CompoundName()}"));

        private static OperationNode MergeWithChild(this OperationNode node, OperationNode childToMerge, IReadOnlyList<OperationNode> allProcessedChildren) {
            LogFast($"Merging child '{childToMerge.Name}' into parent '{node.Name}'.");
            var mergedTags = (node.Tags ?? []).Concat(childToMerge.Tags ?? []).Distinct().ToList();
            var mergedContext = node.ContextData.Concat(childToMerge.ContextData).Distinct().ToArray();
            var mergedStack = (node.LogicalStack ?? Enumerable.Empty<LogicalStackFrame>())
                .Concat(childToMerge.LogicalStack ?? Enumerable.Empty<LogicalStackFrame>())
                .Distinct().ToList();
            var mergedRootCause = node.RootCause ?? childToMerge.RootCause;
            var finalChildren = childToMerge.Children.Concat(allProcessedChildren.Where(c => c != childToMerge)).ToList();
            return new OperationNode(node.Name, mergedContext, finalChildren, mergedRootCause, mergedStack, mergedTags);
        }
        private static List<OperationNode> ProcessChildren(this OperationNode node) 
            => node.Children.Select(c => c.Collapse()).Where(c => c != null)
                .GroupBy(c => c.Name).Select(group => group.ToList().Union()).ToList();
        
        static OperationNode BuildFromGenericException(this Exception ex, IReadOnlyList<LogicalStackFrame> parentStack,
            Dictionary<AmbientFaultContext, FaultHubException> contextLookup) {
            LogFast($"ENTER. Exception Type: '{ex?.GetType().Name}', Message: '{ex?.Message}'");
            if (ex is not AggregateException aggEx) return ex is FaultHubException fhEx ? fhEx.Context.BuildFromContext(parentStack, contextLookup) : null;
    
            var children = aggEx.InnerExceptions.Select(e => e.BuildFromGenericException(parentStack, contextLookup)).Where(n => n != null).ToList();
            LogFast($"Parsed AggregateException into {children.Count} children.");
            return children.Count == 1 ? children.Single() : new OperationNode("Multiple Operations", [], children);
        }

        static OperationNode BuildFromContext(this AmbientFaultContext context, IReadOnlyList<LogicalStackFrame> parentStack, Dictionary<AmbientFaultContext, FaultHubException> contextLookup) {
            if (context == null) return null;
            return context.BoundaryName == null ? context.HandleNamelessContext( parentStack, contextLookup) : context.BuildNamedNode( parentStack, contextLookup);
        }
        
        private static OperationNode BuildNamedNode(this AmbientFaultContext context, IReadOnlyList<LogicalStackFrame> parentStack, Dictionary<AmbientFaultContext, FaultHubException> contextLookup) {
            var localStack = (context.LogicalStackTrace ?? Enumerable.Empty<LogicalStackFrame>()).ToArray();
            var fullStack = localStack.Concat(parentStack.Except(localStack)).ToList();
            var children = new List<OperationNode>();
            var hierarchicalChild = context.InnerContext.BuildFromContext(fullStack, contextLookup);
            if (hierarchicalChild != null) {
                if (hierarchicalChild.Name == "Multiple Operations") {
                    children.AddRange(hierarchicalChild.Children);
                } else {
                    children.Add(hierarchicalChild);
                }
            }
            var rootCause = context.RootCause(contextLookup, fullStack, children);
            var resultNode = new OperationNode(context.BoundaryName, context.UserContext ?? [], children, rootCause, fullStack, context.Tags);
            return resultNode;
        }
        private static OperationNode HandleNamelessContext(this AmbientFaultContext context, IReadOnlyList<LogicalStackFrame> parentStack, Dictionary<AmbientFaultContext, FaultHubException> contextLookup) {
            LogFast($"BoundaryName is null. Trying to build from generic inner exception.");
            if (!contextLookup.TryGetValue(context, out var owner) || owner.InnerException == null) {
                LogFast($"Could not find owner or owner has no inner exception. Returning null.");
                return null;
            }

            var nodeFromInner = owner.InnerException.BuildFromGenericException(parentStack, contextLookup);
            if (nodeFromInner != null) {
                return nodeFromInner;
            }
    
            LogFast($"Inner exception did not produce a node. Creating virtual node for primitive exception '{owner.InnerException.GetType().Name}'.");
            return new OperationNode(
                Name: owner.InnerException.GetType().Name,
                ContextData: [],
                Children: [],
                RootCause: owner.InnerException,
                LogicalStack: context.LogicalStackTrace
            );
        }
        private static Exception RootCause(this AmbientFaultContext context, Dictionary<AmbientFaultContext, FaultHubException> contextLookup, List<LogicalStackFrame> fullStack, List<OperationNode> children) {
            if (!contextLookup.TryGetValue(context, out var contextOwner)) return null;
            return context.InnerContext == null ? contextOwner.ProcessLeafContext( fullStack, contextLookup, children) : null;
        }
        
        private static Exception ProcessLeafContext(this FaultHubException contextOwner, List<LogicalStackFrame> fullStack, Dictionary<AmbientFaultContext, FaultHubException> contextLookup, List<OperationNode> children) {
            LogFast($"No inner context. Building from owner's inner exception.");
            if (contextOwner.InnerException is FaultHubException { Context: not null } innerFhEx && contextOwner.Context.InnerContext == innerFhEx.Context) {
                LogFast($"Child node for '{innerFhEx.Context.BoundaryName}' is already being created via InnerContext traversal. Skipping redundant InnerException parse.");
                return innerFhEx.FindRootCauses().FirstOrDefault();
            }
            var errorNode = contextOwner.InnerException.BuildFromGenericException(fullStack, contextLookup);
            if (errorNode != null) {
                if (errorNode.Name == "Multiple Operations") {
                    LogFast($"errorNode is 'Multiple Operations', adding {errorNode.Children.Count} children.");
                    children.AddRange(errorNode.Children);
                } else {
                    LogFast($"errorNode is a single node, adding '{errorNode.Name}'.");
                    children.Add(errorNode);
                }
            }
            if (contextOwner.InnerException is not (null or FaultHubException or AggregateException)) {
                LogFast($"Found primitive root cause: {contextOwner.InnerException.GetType().Name}");
                return contextOwner.InnerException;
            }
            return null;
            
        }
        

        public static IEnumerable<Exception> FindRootCauses(this FaultHubException e) {
            return e.SelectMany()
                .Where(ex => ex is not AggregateException && ex is not FaultHubException { InnerException: not null });
        }

        
    }
}