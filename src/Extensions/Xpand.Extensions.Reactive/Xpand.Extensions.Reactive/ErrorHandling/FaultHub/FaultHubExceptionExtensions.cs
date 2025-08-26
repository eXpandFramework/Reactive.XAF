using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xpand.Extensions.ExceptionExtensions;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub {
    public record FailureReport(string TopMessage, IReadOnlyList<FailurePath> FailurePaths);

    public record FailurePath(
        IReadOnlyList<OperationStep> Steps,
        Exception RootCause,
        IReadOnlyList<LogicalStackFrame> LogicalStack);

    public record OperationStep(string Name, IReadOnlyList<object> ContextData) {
        public virtual bool Equals(OperationStep other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Name == other.Name &&
                   (ContextData ?? Enumerable.Empty<object>()).SequenceEqual(other.ContextData ??
                                                                             Enumerable.Empty<object>());
        }

        public override int GetHashCode() {
            var hashCode = new HashCode();
            hashCode.Add(Name);
            if (ContextData != null)
                foreach (var item in ContextData)
                    hashCode.Add(item);

            return hashCode.ToHashCode();
        }
    }

    public static class FaultHubExceptionExtensions {
        #region Public Extensions

//MODIFICATION: The logic to find the logical stack has been updated.
//Instead of only looking at the top-level exception, it now searches the
//exception path from the root cause upwards to find the most relevant stack trace.
        public static FailureReport Parse(this FaultHubException topException) {
            Console.WriteLine("\n[PARSER-DIAGNOSTIC] --- Begin Parse ---");
            Console.WriteLine($"[PARSER-DIAGNOSTIC] Parsing top-level exception: '{topException.Message}'");
            var rootCauses = FindRootCauses(topException).ToList();

            var paths = rootCauses.Select((root, i) => {
                Console.WriteLine($"[PARSER-DIAGNOSTIC] >> Building Path #{i + 1} for Root Cause: {root.Message}");
                var exceptionPath = topException.Yield<Exception>().Concat(topException.FailurePath(root)).Reverse().ToList();
                Console.WriteLine(
                    $"[PARSER-DIAGNOSTIC]    FailurePath contains {exceptionPath.Count} total exceptions.");

                var steps = exceptionPath.OfType<FaultHubException>()
                    .Select(ParseStep)
                    .ToList();

                Console.WriteLine($"[PARSER-DIAGNOSTIC]    Extracted {steps.Count} raw operation steps.");

                var logicalStack = exceptionPath.OfType<FaultHubException>()
                    .FirstOrDefault(fhEx => fhEx.LogicalStackTrace.Any())?.LogicalStackTrace.ToList() 
                                   ?? topException.LogicalStackTrace.ToList();
                var distinctSteps = steps.Distinct().ToList();

                Console.WriteLine($"[PARSER-DIAGNOSTIC]    Deduplicated to {distinctSteps.Count} unique steps.");
                return new FailurePath(distinctSteps, root, logicalStack);
            }).ToList();

            Console.WriteLine($"[PARSER-DIAGNOSTIC] --- End Parse --- (Found {paths.Count} paths)");
            return new FailureReport(topException.Message, paths);
        }
//MODIFICATION: End of changes.
        public static string Render(this FailureReport model) {
            Console.WriteLine("\n[RENDER-DIAGNOSTIC] --- Begin Render ---");
            Console.WriteLine(
                $"[RENDER-DIAGNOSTIC] Rendering report for '{model.TopMessage}' with {model.FailurePaths.Count} failure path(s).");
            var sb = new StringBuilder();

            var rootCauseSummary = string.Join(" • ", model.FailurePaths.Select(p => p.RootCause.Message).Distinct());
            sb.AppendLine(
                $"{model.TopMessage} ({model.FailurePaths.Count} times{(string.IsNullOrEmpty(rootCauseSummary) ? "" : $" • {rootCauseSummary}")})");
            
            if (!model.FailurePaths.Any()) {
                Console.WriteLine("[RENDER-DIAGNOSTIC] No failure paths to render. Exiting.");
                return sb.ToString().Trim();
            }

            var topOperationContext = model.FailurePaths.FirstOrDefault()?.Steps.FirstOrDefault()?.ContextData ?? Enumerable.Empty<object>();
            var parentContexts = new HashSet<object>(topOperationContext);
            var topOperationName = model.TopMessage.Split(' ')[0];
            RenderPathsRecursively(model.FailurePaths.ToList(), 0, "", true, sb, null, topOperationName, parentContexts);
            Console.WriteLine($"[RENDER-DIAGNOSTIC] --- End Render (Final String Length: {sb.Length}) ---\n");
            return sb.ToString().Trim();
        }
// File: Xpand.Extensions.Reactive\Xpand.Extensions.Reactive\ErrorHandling\FaultHub\FaultHubExceptionExtensions.cs

private static void RenderPathsRecursively(List<FailurePath> paths, int level, string prefix, bool isRootLevel,
    StringBuilder sb, OperationStep parentStep, string topOperationName, ISet<object> parentContexts) {
    var groups = paths.Where(p => p.Steps.Count > level)
        .GroupBy(p => p.Steps[level])
        .ToList();
    for (var i = 0; i < groups.Count; i++) {
        var group = groups[i];
        var isLastGroup = i == groups.Count - 1;
        var step = group.Key;
        var childPrefix = prefix + (isLastGroup ? "  " : "│ ");
        bool hasContinuingPaths;
        if (parentStep != null && step.Name == parentStep.Name) {
            var subPaths = group.ToList();
            
            hasContinuingPaths = subPaths.Any(p => p.Steps.Count > level + 1);
            if (hasContinuingPaths) RenderPathsRecursively(subPaths, level + 1, prefix, false, sb, step, topOperationName, parentContexts);
            continue;
        }

        if (isRootLevel && step.Name.Replace(" ", "") == topOperationName.Replace(" ", "")) {
            var subPaths = group.ToList();
            hasContinuingPaths = subPaths.Any(p => p.Steps.Count > level + 1);
            if (hasContinuingPaths) RenderPathsRecursively(subPaths, level + 1, prefix, false, sb, step, topOperationName, parentContexts);
            continue;
        }

        var connector = isLastGroup ? "└ " : "├ ";
        sb.Append(prefix).Append(connector);

        var stepText = new StringBuilder(step.Name);
        
        var newContextData = step.ContextData.Where(c => !parentContexts.Contains(c)).ToList();
        if (newContextData.Any()) {
            stepText.Append(" (").Append(string.Join(", ", newContextData)).Append(")");
        }
        
        sb.AppendLine(stepText.ToString());

        var childParentContexts = new HashSet<object>(parentContexts);
        foreach (var ctx in step.ContextData) {
            childParentContexts.Add(ctx);
        }
        
        var finishedPaths = group.Where(p => p.Steps.Count == level + 1).ToList();
        hasContinuingPaths = group.Any(p => p.Steps.Count > level + 1);
        if (finishedPaths.Any()) RenderFailureDetails(finishedPaths.First(), sb, childPrefix);
        if (hasContinuingPaths) RenderPathsRecursively(group.ToList(), level + 1, childPrefix, false, sb, step, topOperationName, childParentContexts);
    }
}
private static void RenderFailureDetails(FailurePath path, StringBuilder sb, string prefix) {
            sb.Append(prefix).Append("• Root Cause: ").Append(path.RootCause.GetType().FullName).Append(": ")
                .AppendLine(path.RootCause.Message);
            var hasStack = path.LogicalStack.Any();
            var hasOriginal = path.RootCause != null;

            var detailsPrefixBuilder = new StringBuilder();
            foreach (var character in prefix) {
                switch (character) {
                    case '├':
                    case '│':
                        detailsPrefixBuilder.Append('│');
                        break;
                    case '└':
                        detailsPrefixBuilder.Append(' ');
                        break;
                    default:
                        detailsPrefixBuilder.Append(character);
                        break;
                }
            }
            var detailsPrefix = detailsPrefixBuilder.ToString();

            if (hasStack) {
                sb.Append(detailsPrefix).Append("   --- Invocation Stack ---").AppendLine();
                var stackPrefix = detailsPrefix + "     ";
                foreach (var frame in path.LogicalStack) {
                    var frameContext = frame.Context?.Any() == true ? $"({string.Join(", ", frame.Context)}) " : "";
                    sb.Append(stackPrefix).Append(frameContext).Append("at ").Append(frame.MemberName)
                        .Append(" in ").Append(frame.FilePath).Append(":line ")
                        .Append(frame.LineNumber).AppendLine();
                }
            }

            if (hasOriginal) {
                sb.Append(detailsPrefix).Append("   --- Original Exception Details ---").AppendLine();
                var originalPrefix = detailsPrefix + "     ";
                var exceptionLines = path.RootCause.ToString().Split([Environment.NewLine], StringSplitOptions.None);
                foreach (var line in exceptionLines) {
                    sb.Append(originalPrefix).AppendLine(line);
                }
            }
        }

// File: Xpand.Extensions.Reactive\Xpand.Extensions.Reactive\ErrorHandling\FaultHub\FaultHubExceptionExtensions.cs

        private static OperationStep ParseStep(FaultHubException fhEx) {
            var ctx = fhEx.Context;
            var boundaryName = ctx.BoundaryName ?? "Unnamed Operation";
            var userContext = ctx.UserContext ?? [];
            var openParen = boundaryName.IndexOf('(');
            var dirtyName = boundaryName;
            if (openParen > -1) dirtyName = dirtyName.Substring(0, openParen);
            var cleanName = dirtyName.Trim();

            var contextData = userContext.ToList();

            var finalName = cleanName.CompoundName();
            return new OperationStep(finalName, contextData);
        }        
        #endregion

        #region Private Helpers

        private static IEnumerable<FaultHubException> UnpackContexts(FaultHubException fhEx) {
            var context = fhEx.Context;
            while (context != null) {
                yield return new FaultHubException(fhEx.Message, fhEx.InnerException, context);
                context = context.InnerContext;
            }
        }

        private static IEnumerable<Exception> FindRootCauses(Exception ex) {
            Console.WriteLine(
                $"[PARSER-DIAGNOSTIC]   - FindRootCauses searching in: {ex?.GetType().Name ?? "null"} ('{ex?.Message}')");

            if (ex is AggregateException aggEx) {
                Console.WriteLine(
                    "[PARSER-DIAGNOSTIC]     Type is AggregateException. Recursing into InnerExceptions.");
                foreach (var inner in aggEx.InnerExceptions)
                foreach (var root in FindRootCauses(inner))
                    yield return root;
            }
            else if (ex is FaultHubException { InnerException: not null } fhEx) {
                Console.WriteLine("[PARSER-DIAGNOSTIC]     Type is FaultHubException. Recursing into InnerException.");
                foreach (var root in FindRootCauses(fhEx.InnerException)) yield return root;
            }
            else if (ex != null) {
                Console.WriteLine($"[PARSER-DIAGNOSTIC]     ==> Found a Root Cause: {ex.Message}");
                yield return ex;
            }
        }

        private static string CompoundName(this string s)
            => s == null ? null : Regex.Replace(s, @"(\B[A-Z])", " $1");

        #endregion
    }
}