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
    Console.WriteLine(
        $"[RENDER-DIAGNOSTIC] Rendering report for '{model.TopMessage}' with {model.FailurePaths.Count} failure path(s).");
    var sb = new StringBuilder();
    
    sb.AppendLine($"{model.TopMessage} ({model.FailurePaths.Count} times)");

    for (var i = 0; i < model.FailurePaths.Count; i++) {
        var path = model.FailurePaths[i];
        if (i > 0) sb.AppendLine();
        
        Console.WriteLine($"[RENDER-DIAGNOSTIC] >> Rendering Path #{i + 1}");

        for (var j = 0; j < path.Steps.Count; j++) {
            var step = path.Steps[j];
            
            if (j == 0) {
                sb.Append($"{i + 1}. ");
            } else {
                var indent = new string(' ', 3 + (j - 1) * 2);
                sb.Append(indent);
            }

            sb.Append(step.Name);

            if (step.ContextData.Any()) sb.Append(" (").Append(string.Join(", ", step.ContextData)).Append(")");
            sb.AppendLine();
        }
        
        var finalIndent = new string(' ', 4);
        Console.WriteLine($"[RENDER-DIAGNOSTIC]    Root Cause: {path.RootCause.Message}");
        
        sb.Append(finalIndent).Append("• Root Cause: ").Append(path.RootCause.GetType().FullName).Append(": ")
            .AppendLine(path.RootCause.Message);

        if (path.LogicalStack.Any()) {
            Console.WriteLine($"[RENDER-DIAGNOSTIC]    Invocation Stack ({path.LogicalStack.Count} frames)");
            var sectionIndent = finalIndent + "  ";
            sb.Append(sectionIndent).AppendLine("--- Invocation Stack ---");
            foreach (var frame in path.LogicalStack) {
                var frameContext = frame.Context.Any() ? $"({string.Join(", ", frame.Context)}) " : "";
                sb.Append(sectionIndent).Append("  ").Append(frameContext).Append("at ").Append(frame.MemberName)
                    .Append(" in ").Append(Path.GetFileName(frame.FilePath)).Append(":line ")
                    .Append(frame.LineNumber).AppendLine();
            }
        }

        if (path.RootCause != null) {
            var sectionIndent = finalIndent + "  ";
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
    
    // Extract parameter names from the BoundaryName, e.g., "driver", "padProject" from "MyMethod(driver, padProject)"
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
    if (openParen > -1) {
        dirtyName = dirtyName.Substring(0, openParen);
    }
    var cleanName = dirtyName.Trim();
    var cleanNameNoSpace = cleanName.Replace(" ", "");
    
    // Filter out both the step's own name and any extracted parameter names from the context
    var contextData = userContext.Where(o => {
        if (o is not string s) return true; // Keep non-string context
        if (s.Replace(" ", "") == cleanNameNoSpace) return false; // Remove self-reference
        if (paramNames.Contains(s)) return false; // Remove parameters
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