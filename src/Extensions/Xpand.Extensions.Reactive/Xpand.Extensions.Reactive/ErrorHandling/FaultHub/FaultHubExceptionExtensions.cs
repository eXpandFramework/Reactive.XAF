using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xpand.Extensions.ExceptionExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub {
    public static class FaultHubExceptionExtensions {
        public record OperationStep(string Name, IReadOnlyList<object> ContextData);
        public record FailurePath(IReadOnlyList<OperationStep> Steps, Exception RootCause, IReadOnlyList<LogicalStackFrame> InvocationStack);

        public record FailureReport(string TopLevelMessage, IReadOnlyList<FailurePath> Paths);

        public static FailureReport Parse(this Exception topLevelException) {
            var failurePaths = new List<FailurePath>();

            if (topLevelException.InnerException is AggregateException aggregateException)
                foreach (var innerEx in aggregateException.InnerExceptions)
                    failurePaths.Add(ParseSinglePath(innerEx));
            else
                failurePaths.Add(ParseSinglePath(topLevelException));

            return new FailureReport(topLevelException.Message, failurePaths);
        }
        static string CompoundName(this string s) 
            => s == null ? null : Regex.Replace(s, @"(\B[A-Z])", " $1");
        public static string Render(this FailureReport report) {
            var builder = new StringBuilder();
            builder.AppendLine(report.TopLevelMessage);

            if (report.Paths.Count > 0) {
                builder.AppendLine();
                builder.AppendLine($"--- Failures ({report.Paths.Count}) ---");
            }

            for (var i = 0; i < report.Paths.Count; i++) {
                var path = report.Paths[i];
                builder.AppendLine();
                builder.AppendLine($"{i + 1}. Failure Path:");

                var indent = "  ";
                foreach (var step in path.Steps) {
                    builder.Append(indent);
                    builder.Append($"- Operation: {step.Name}");
                    if (step.ContextData.Any()) builder.Append($" ({string.Join(", ", step.ContextData)})");
                    builder.AppendLine();
                    indent += "  ";
                }

                builder.AppendLine(
                    $"{indent}Root Cause: {path.RootCause.GetType().FullName}: {path.RootCause.Message}");

                if (path.InvocationStack.Any()) {
                    builder.AppendLine($"{indent}--- Invocation Stack ---");
                    foreach (var frame in path.InvocationStack) {
                        var frameContext = frame.Context.Any()
                            ? $"({string.Join(", ", frame.Context)})"
                            : "()";
                        builder.AppendLine(
                            $"{indent}  {frameContext} at {frame.MemberName} in {frame.FilePath}:line {frame.LineNumber}");
                    }
                }

                if (path.RootCause != null) {
                    builder.AppendLine($"{indent}--- Original Exception Details ---");
                    builder.AppendLine(path.RootCause.ToString().Indent(indent.Length + 2));
                }
            }

            return builder.ToString();
        }

        private static FailurePath ParseSinglePath(Exception ex) {
            var fault = ex as FaultHubException ?? new FaultHubException("Wrapper", ex, new AmbientFaultContext());

            var operationSteps = fault.Context.FromHierarchy(c => c.InnerContext)
                .Select(ctx => {
                    Console.WriteLine("\n--- PARSING STEP ---");
                    var boundaryName = ctx.BoundaryName;
                    var userContext = ctx.UserContext ?? [];
                    Console.WriteLine($"BoundaryName: '{boundaryName ?? "null"}'");
                    Console.WriteLine(
                        $"UserContext Items ({userContext.Length}): [{string.Join(", ", userContext)}]");


                    string name;
                    IReadOnlyList<object> contextData;

                    var userContextStrings = userContext.OfType<string>().ToList();
                    var boundaryNameNoSpace = boundaryName?.Replace(" ", "");

                    var isMatch = userContext.Any() &&
                                  userContextStrings.Any(s => s.Replace(" ", "") == boundaryNameNoSpace);
                    Console.WriteLine($"Checking if UserContext contains BoundaryName... IS MATCH? ==> {isMatch}");


                    if (isMatch) {
                        Console.WriteLine("--> Logic Path Taken: IF branch (UserContext is source of truth)");
                        name = userContext.OfType<string>().FirstOrDefault() ?? boundaryName;
                        contextData = userContext.Skip(1).ToList();
                    }
                    else {
                        Console.WriteLine("--> Logic Path Taken: ELSE branch (BoundaryName is source of truth)");
                        name = boundaryName;
                        contextData = userContext.ToList();
                    }

                    var finalName = name ?? "Unnamed Operation";
                    Console.WriteLine($"==> Result: Name='{finalName}', ContextData Items='{contextData.Count}'");

                    return new OperationStep(finalName.CompoundName(), contextData);
                })
                .ToList();

            var rootCause = ex.SelectMany()
                .FirstOrDefault(e
                    => e.InnerException == null && !(e is AggregateException ae && ae.InnerExceptions.Any())) ?? ex;

            var invocationStack = fault.LogicalStackTrace.ToList();

            return new FailurePath(operationSteps, rootCause, invocationStack);
        }
    }
}