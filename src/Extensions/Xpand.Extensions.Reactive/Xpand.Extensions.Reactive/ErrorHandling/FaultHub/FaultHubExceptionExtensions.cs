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

        public static FailureReport Parse(this FaultHubException topException) {
            Console.WriteLine("\n[PARSER-DIAGNOSTIC] --- Begin Parse ---");
            Console.WriteLine($"[PARSER-DIAGNOSTIC] Parsing top-level exception: '{topException.Message}'");
            var rootCauses = FindRootCauses(topException).ToList();

            var paths = rootCauses.Select((root, i) => {
                Console.WriteLine($"[PARSER-DIAGNOSTIC] >> Building Path #{i + 1} for Root Cause: {root.Message}");
                var exceptionPath = topException.FailurePath(root).Reverse().ToList();
                Console.WriteLine(
                    $"[PARSER-DIAGNOSTIC]    FailurePath contains {exceptionPath.Count} total exceptions.");

                var steps = exceptionPath.OfType<FaultHubException>()
                    .SelectMany(UnpackContexts)
                    .Select(ParseStep).ToList();
                Console.WriteLine($"[PARSER-DIAGNOSTIC]    Extracted {steps.Count} raw operation steps.");

                var logicalStack =
                    exceptionPath.OfType<FaultHubException>().LastOrDefault()?.LogicalStackTrace.ToList() ?? [];
                var distinctSteps = steps.Distinct().ToList();

                Console.WriteLine($"[PARSER-DIAGNOSTIC]    Deduplicated to {distinctSteps.Count} unique steps.");
                return new FailurePath(distinctSteps, root, logicalStack);
            }).ToList();

            Console.WriteLine($"[PARSER-DIAGNOSTIC] --- End Parse --- (Found {paths.Count} paths)");
            return new FailureReport(topException.Message, paths);
        }

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

            RenderPathsRecursively(model.FailurePaths.ToList(), 0, "", true, sb);
            Console.WriteLine($"[RENDER-DIAGNOSTIC] --- End Render (Final String Length: {sb.Length}) ---\n");
            return sb.ToString().Trim();
        }
        private static void RenderPathsRecursively(List<FailurePath> paths, int level, string prefix, bool isRootLevel,
            StringBuilder sb) {
            var groups = paths.Where(p => p.Steps.Count > level)
                .GroupBy(p => p.Steps[level])
                .ToList();

            for (var i = 0; i < groups.Count; i++) {
                var group = groups[i];
                var isLastGroup = i == groups.Count - 1;
                var step = group.Key;

                var connector = isLastGroup ? "└ " : "├ ";
                sb.Append(prefix).Append(connector);

                var stepText = new StringBuilder(step.Name);
                if (!(isRootLevel && i == 0) && step.ContextData.Any())
                    stepText.Append(" (").Append(string.Join(", ", step.ContextData)).Append(")");
                sb.AppendLine(stepText.ToString());

                var subPaths = group.ToList();
                var childPrefix = prefix + (isLastGroup ? "  " : "│ ");

                var finishedPaths = subPaths.Where(p => p.Steps.Count == level + 1).ToList();
                var hasContinuingPaths = subPaths.Any(p => p.Steps.Count > level + 1);

                if (finishedPaths.Any()) RenderFailureDetails(finishedPaths.First(), sb, childPrefix);

                if (hasContinuingPaths) RenderPathsRecursively(subPaths, level + 1, childPrefix, false, sb);
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
                    var frameContext = frame.Context.Any() ? $"({string.Join(", ", frame.Context)}) " : "";
                    sb.Append(stackPrefix).Append(frameContext).Append("at ").Append(frame.MemberName)
                        .Append(" in ").Append(Path.GetFileName(frame.FilePath)).Append(":line ")
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

        private static OperationStep ParseStep(FaultHubException fhEx) {
            var ctx = fhEx.Context;
            var boundaryName = ctx.BoundaryName ?? "Unnamed Operation";
            var userContext = ctx.UserContext ?? [];

            string[] paramNames = [];
            var openParen = boundaryName.IndexOf('(');
            if (openParen > -1) {
                var closeParen = boundaryName.LastIndexOf(')');
                if (closeParen > openParen) {
                    var paramString = boundaryName.Substring(openParen + 1, closeParen - openParen - 1);
                    paramNames = paramString.Split(',').Select(p => p.Trim()).ToArray();
                }
            }

            var dirtyName = boundaryName;
            if (openParen > -1) dirtyName = dirtyName.Substring(0, openParen);
            var cleanName = dirtyName.Trim();
            var cleanNameNoSpace = cleanName.Replace(" ", "");

            var contextData = userContext.Where(o => {
                if (o is not string s) return true;
                if (s.Replace(" ", "") == cleanNameNoSpace) return false;
                if (paramNames.Contains(s)) return false;
                return true;
            }).ToList();

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