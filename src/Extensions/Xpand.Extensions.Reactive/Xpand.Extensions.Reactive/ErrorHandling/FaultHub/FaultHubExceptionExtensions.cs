using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xpand.Extensions.ExceptionExtensions;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub {
    public record FailureReport(string TopMessage, IReadOnlyList<FailurePath> FailurePaths);

    public record FailurePath(IReadOnlyList<OperationStep> Steps, Exception RootCause, IReadOnlyList<LogicalStackFrame> LogicalStack);

    public record OperationStep(string Name, IReadOnlyList<object> ContextData) {
        public virtual bool Equals(OperationStep other) 
            => !ReferenceEquals(null, other) && (ReferenceEquals(this, other) || Name == other.Name &&
                (ContextData ?? Enumerable.Empty<object>()).SequenceEqual(other.ContextData ?? Enumerable.Empty<object>()));
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
                var distinctSteps = steps.GroupBy(step => step.Name).Select(group => group.Last()).ToList();

                Console.WriteLine($"[PARSER-DIAGNOSTIC]    Deduplicated to {distinctSteps.Count} unique steps.");
                return new FailurePath(distinctSteps, root, logicalStack);
            }).ToList();

            Console.WriteLine($"[PARSER-DIAGNOSTIC] --- End Parse --- (Found {paths.Count} paths)");
            return new FailureReport(topException.Message, paths);
        }

        public static string Render(this FailureReport model) {
            Console.WriteLine("\n[RENDER-DIAGNOSTIC] --- Begin Render ---");
            Console.WriteLine($"[RENDER-DIAGNOSTIC] Rendering report for '{model.TopMessage}' with {model.FailurePaths.Count} failure path(s).");
            var sb = new StringBuilder();
            sb.AppendLine($"{model.TopMessage} ({model.FailurePaths.Count} times)");

            if (!model.FailurePaths.Any()) {
                Console.WriteLine("[RENDER-DIAGNOSTIC] No failure paths to render. Exiting.");
                return sb.ToString().Trim();
            }

            var firstPathSteps = model.FailurePaths[0].Steps;
            var commonPrefixLength = 0;
            for (var i = 0; i < firstPathSteps.Count; i++) {
                var currentStep = firstPathSteps[i];
                if (model.FailurePaths.Skip(1).All(p => p.Steps.Count > i && p.Steps[i].Equals(currentStep)))
                    commonPrefixLength++;
                else
                    break;
            }

            Console.WriteLine($"[RENDER-DIAGNOSTIC] Found common prefix of length: {commonPrefixLength}");

            var commonPath = firstPathSteps.Take(commonPrefixLength).ToList();
            for (var i = 0; i < commonPath.Count; i++) {
                var step = commonPath[i];
                var indent = new string(' ', (i + 1) * 2);
                Console.WriteLine(
                    $"[RENDER-DIAGNOSTIC]   - Rendering common step {i}: '{step.Name}' at indent level {i + 1}");
                sb.Append(indent).Append(step.Name);
                if (step.ContextData.Any()) sb.Append(" (").Append(string.Join(", ", step.ContextData)).Append(")");
                sb.AppendLine();
            }

            for (var i = 0; i < model.FailurePaths.Count; i++) {
                var path = model.FailurePaths[i];
                Console.WriteLine($"[RENDER-DIAGNOSTIC] >> Rendering Divergent Path #{i + 1}");
                if (i > 0 && model.FailurePaths.Count > 1) sb.AppendLine();

                var divergentSteps = path.Steps.Skip(commonPrefixLength).ToList();

                for (var j = 0; j < divergentSteps.Count; j++) {
                    var step = divergentSteps[j];
                    var currentLevel = commonPrefixLength + j + 1;
                    var indent = new string(' ', currentLevel * 2);
                    Console.WriteLine(
                        $"[RENDER-DIAGNOSTIC]    - Rendering divergent step {j}: '{step.Name}' at indent level {currentLevel}");

                    if (j == 0)
                        sb.Append(indent).Append($"{i + 1}. ");
                    else
                        sb.Append(indent).Append("   ");

                    sb.Append(step.Name);
                    if (step.ContextData.Any()) sb.Append(" (").Append(string.Join(", ", step.ContextData)).Append(")");
                    sb.AppendLine();
                }

                var detailsBaseLevel = commonPrefixLength + divergentSteps.Count();
                var detailsIndent = new string(' ', detailsBaseLevel * 2 + 4);

                Console.WriteLine($"[RENDER-DIAGNOSTIC]    Root Cause: {path.RootCause.Message} at base indent level {detailsBaseLevel}");
                sb.Append(detailsIndent).Append("• Root Cause: ").Append(path.RootCause.GetType().FullName).Append(": ")
                    .AppendLine(path.RootCause.Message);

                if (path.LogicalStack.Any()) {
                    Console.WriteLine($"[RENDER-DIAGNOSTIC]    Invocation Stack ({path.LogicalStack.Count} frames)");
                    var sectionIndent = detailsIndent + "  ";
                    sb.Append(sectionIndent).AppendLine("--- Invocation Stack ---");
                    foreach (var frame in path.LogicalStack) {
                        var frameContext = frame.Context.Any() ? $"({string.Join(", ", frame.Context)}) " : "";
                        sb.Append(sectionIndent).Append("  ").Append(frameContext).Append("at ")
                            .Append(frame.MemberName)
                            .Append(" in ").Append(Path.GetFileName(frame.FilePath)).Append(":line ")
                            .Append(frame.LineNumber).AppendLine();
                    }
                }

                if (path.RootCause != null) {
                    var sectionIndent = detailsIndent + "  ";
                    sb.Append(sectionIndent).AppendLine("--- Original Exception Details ---");
                    sb.AppendLine(path.RootCause.ToString().Indent(sectionIndent.Length + 2));
                }
            }

            Console.WriteLine($"[RENDER-DIAGNOSTIC] --- End Render (Final String Length: {sb.Length}) ---\n");
            return sb.ToString().Trim();
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
            Console.WriteLine($"[PARSER-DIAGNOSTIC]   - FindRootCauses searching in: {ex?.GetType().Name ?? "null"} ('{ex?.Message}')");
            if (ex is AggregateException aggEx) {
                Console.WriteLine("[PARSER-DIAGNOSTIC]     Type is AggregateException. Recursing into InnerExceptions.");
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