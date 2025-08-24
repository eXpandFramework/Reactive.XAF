using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xpand.Extensions.ExceptionExtensions;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub {
    public record FailureReport(string TopMessage, IReadOnlyList<FailurePath> FailurePaths);
    public record FailurePath(IReadOnlyList<OperationStep> Steps, Exception RootCause, IReadOnlyList<LogicalStackFrame> LogicalStack);
    public record OperationStep(string Name, IReadOnlyList<object> ContextData);

    public static class FaultHubExceptionExtensions {
        #region Public Extensions
        public static FailureReport Parse(this FaultHubException topException) {
            Console.WriteLine("\n[PARSER-DIAGNOSTIC] --- Begin Parse ---");
            Console.WriteLine($"[PARSER-DIAGNOSTIC] Parsing top-level exception: '{topException.Message}'");
            var rootCauses = FindRootCauses(topException).ToList();
            
            var paths = rootCauses.Select((root, i) => {
                Console.WriteLine($"[PARSER-DIAGNOSTIC] >> Building Path #{i + 1} for Root Cause: {root.Message}");
                var exceptionPath = topException.FailurePath(root).Reverse().ToList();
                Console.WriteLine($"[PARSER-DIAGNOSTIC]    FailurePath contains {exceptionPath.Count} total exceptions.");
                // This now correctly unpacks the nested contexts from each exception in the path.
                var steps = exceptionPath.OfType<FaultHubException>()
                    .SelectMany(UnpackContexts) 
                    .Select(ParseStep).ToList();
                Console.WriteLine($"[PARSER-DIAGNOSTIC]    Extracted {steps.Count} raw operation steps.");
                
                var logicalStack = exceptionPath.OfType<FaultHubException>().LastOrDefault()?.LogicalStackTrace.ToList() ?? [];
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
    sb.AppendLine(model.TopMessage);

    if (model.FailurePaths.Any()) {
        sb.AppendLine();
        sb.AppendLine($"--- Failures ({model.FailurePaths.Count}) ---");
    }

    for (var i = 0; i < model.FailurePaths.Count; i++) {
        var path = model.FailurePaths[i];
        Console.WriteLine($"[RENDER-DIAGNOSTIC] >> Rendering Path #{i + 1}");
        sb.AppendLine();
        sb.AppendLine($"{i + 1}. Failure Path:");
        var indent = "  ";
        foreach (var step in path.Steps) {
            Console.WriteLine($"[RENDER-DIAGNOSTIC]    - Step: {step.Name} ({string.Join(", ", step.ContextData)})");
            sb.Append(indent).Append("- Operation: ").Append(step.Name);
            if (step.ContextData.Any()) {
                sb.Append(" (").Append(string.Join(", ", step.ContextData)).Append(")");
            }
            sb.AppendLine();
            indent += "  ";
        }

        Console.WriteLine($"[RENDER-DIAGNOSTIC]    Root Cause: {path.RootCause.Message}");
        sb.Append(indent).Append("Root Cause: ").Append(path.RootCause.GetType().FullName).Append(": ").AppendLine(path.RootCause.Message);

        if (path.LogicalStack.Any()) {
            Console.WriteLine($"[RENDER-DIAGNOSTIC]    Invocation Stack ({path.LogicalStack.Count} frames)");
            sb.Append(indent).AppendLine("--- Invocation Stack ---");
            foreach (var frame in path.LogicalStack) {
                var frameContext = frame.Context.Any() ?
                    $"({string.Join(", ", frame.Context)})" : "()";
                sb.Append(indent).Append("  ").Append(frameContext).Append(" at ").Append(frame.MemberName)
                  .Append(" in ").Append(frame.FilePath).Append(":line ").Append(frame.LineNumber).AppendLine();
            }
        }

        if (path.RootCause != null) {
            sb.Append(indent).AppendLine("--- Original Exception Details ---");
            sb.AppendLine(path.RootCause.ToString().Indent(indent.Length + 2));
        }
    }

    Console.WriteLine($"[RENDER-DIAGNOSTIC] --- End Render (Final String Length: {sb.Length}) ---\n");
    return sb.ToString().Trim();
}
        #endregion

        #region Private Helpers
        private static OperationStep ParseStep(FaultHubException fhEx) {
            var ctx = fhEx.Context;
            var boundaryName = ctx.BoundaryName;
            var userContext = ctx.UserContext ?? [];
    
            Console.WriteLine($"[PARSER-DIAGNOSTIC]      - Parsing Step from FH: '{fhEx.Message}'. BoundaryName: '{boundaryName ?? "null"}', UserContext Items: {userContext.Length}");

            var dirtyName = boundaryName ?? "Unnamed Operation";
            var parenthesisIndex = dirtyName.IndexOf('(');
            if (parenthesisIndex > -1) {
                dirtyName = dirtyName.Substring(0, parenthesisIndex);
            }
            var cleanName = dirtyName.Trim();
            var cleanNameNoSpace = cleanName.Replace(" ", "");

            var contextData = userContext.Where(o => {
                if (o is not string s) return true;
                // Filter out any string from the context that is the same as the clean name.
                return s.Replace(" ", "") != cleanNameNoSpace;
            }).ToList();


            var finalName = cleanName.CompoundName();
            Console.WriteLine($"[PARSER-DIAGNOSTIC]        ==> Extracted Name: '{finalName}', Context Items: {contextData.Count}");
            return new OperationStep(finalName, contextData);            
        }
        
        private static IEnumerable<FaultHubException> UnpackContexts(FaultHubException fhEx) {
            var context = fhEx.Context;
            while (context != null) {
                // We create a temporary FHE for each level of the context chain to pass to ParseStep
                yield return new FaultHubException(fhEx.Message, fhEx.InnerException, context);
                context = context.InnerContext;
            }
        }
        private static IEnumerable<Exception> FindRootCauses(Exception ex) {
            Console.WriteLine($"[PARSER-DIAGNOSTIC]   - FindRootCauses searching in: {ex?.GetType().Name ?? "null"} ('{ex?.Message}')");

            if (ex is AggregateException aggEx) {
                Console.WriteLine("[PARSER-DIAGNOSTIC]     Type is AggregateException. Recursing into InnerExceptions.");
                foreach (var inner in aggEx.InnerExceptions) {
                    foreach (var root in FindRootCauses(inner)) {
                        yield return root;
                    }
                }
            } else if (ex is FaultHubException { InnerException: not null } fhEx) {
                Console.WriteLine("[PARSER-DIAGNOSTIC]     Type is FaultHubException. Recursing into InnerException.");
                foreach (var root in FindRootCauses(fhEx.InnerException)) {
                    yield return root;
                }
            } else if (ex != null) {
                Console.WriteLine($"[PARSER-DIAGNOSTIC]     ==> Found a Root Cause: {ex.Message}");
                yield return ex;
            }
        }

        private static string CompoundName(this string s) 
            => s == null ? null : Regex.Replace(s, @"(\B[A-Z])", " $1");
        #endregion
    }
}