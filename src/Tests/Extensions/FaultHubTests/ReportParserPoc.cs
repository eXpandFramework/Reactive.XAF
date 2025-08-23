// File: C:\Work\Reactive.XAF\src\Tests\Extensions\FaultHubTests\POC\ReportParserPoc.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.ExceptionExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.Tests.FaultHubTests {
    [TestFixture]
    public class ReportParserPoc {

        #region Pure Report Data Model
        private record FailureReport(string TopLevelMessage, IReadOnlyList<FailurePath> Paths);
        private record FailurePath(IReadOnlyList<OperationStep> Steps, Exception RootCause, IReadOnlyList<LogicalStackFrame> InvocationStack);
        private record OperationStep(string Name, IReadOnlyList<object> ContextData);
        #endregion

        #region Parser & Renderer Implementation
        private static class ReportParser {
            public static FailureReport Parse(Exception topLevelException) {
                var failurePaths = new List<FailurePath>();

                if (topLevelException.InnerException is AggregateException aggregateException) {
                    foreach (var innerEx in aggregateException.InnerExceptions) {
                        failurePaths.Add(ParseSinglePath(innerEx));
                    }
                } else {
                    failurePaths.Add(ParseSinglePath(topLevelException));
                }
                
                return new FailureReport(topLevelException.Message, failurePaths);
            }

            private static FailurePath ParseSinglePath(Exception ex) {
                var fault = ex as FaultHubException ?? new FaultHubException("Wrapper", ex, new AmbientFaultContext());
                
                var operationSteps = fault.Context.FromHierarchy(c => c.InnerContext)
                    .Select(ctx => new OperationStep(
                        (string)ctx.CustomContext.FirstOrDefault() ?? "Unnamed Operation",
                        ctx.CustomContext.Skip(1).ToList()))
                    .ToList();

                var rootCause = ex.SelectMany()
                    .FirstOrDefault(e => e.InnerException == null && !(e is AggregateException ae && ae.InnerExceptions.Any())) ?? ex;
                
                var invocationStack = fault.LogicalStackTrace.ToList();
                
                return new FailurePath(operationSteps, rootCause, invocationStack);
            }

            // MODIFICATION: The new Render method to format the report model into a string.
//MODIFICATION: The incorrect .Reverse() call has been removed from the foreach loop.
            //MODIFICATION: The logic for formatting the stack frame context has been corrected to always include parentheses.
            public static string Render(FailureReport report) {
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
                        if (step.ContextData.Any()) {
                            builder.Append($" ({string.Join(", ", step.ContextData)})");
                        }
                        builder.AppendLine();
                        indent += "  ";
                    }

                    builder.AppendLine($"{indent}Root Cause: {path.RootCause.GetType().FullName}: {path.RootCause.Message}");

                    if (path.InvocationStack.Any()) {
                        builder.AppendLine($"{indent}--- Invocation Stack ---");
                        foreach (var frame in path.InvocationStack) {
                            var frameContext = frame.Context.Any()
                                ? $"({string.Join(", ", frame.Context)})"
                                : "()"; // Ensure () is always present.
                            builder.AppendLine($"{indent}  {frameContext} at {frame.MemberName} in {frame.FilePath}:line {frame.LineNumber}");
                        }
                    }

                    if (path.RootCause != null) {
                        builder.AppendLine($"{indent}--- Original Exception Details ---");
                        builder.AppendLine(path.RootCause.ToString().Indent(indent.Length + 2));
                    }
                }
                return builder.ToString();
            }        }
        #endregion

        [Test]
        public void Traverses_Parses_And_Renders_Complex_Nested_Exception_Correctly() {
            // ARRANGE
            var rootCause1 = new Exception("Upcoming");
            var logicalStack1 = new List<LogicalStackFrame> { new("WhenUpcomingUrls", @"C:\...\Services\LaunchPadProjectPageParseService.cs", 582) };
            var context1C = new AmbientFaultContext { CustomContext = ["When Upcoming Urls"], LogicalStackTrace = logicalStack1 };
            var context1B = new AmbientFaultContext { CustomContext = ["Parse Up Coming", "LaunchPad kommunitas"], InnerContext = context1C };
            var context1A = new AmbientFaultContext { CustomContext = ["Schedule Launch Pad Parse", "Kommunitas Kommunitas"], InnerContext = context1B };
            var faultPath1 = new FaultHubException("Wrapper1", rootCause1, context1A);

            var rootCause2 = new Exception("StartParsing");
            var logicalStack2 = new List<LogicalStackFrame> { new("StartParsing", @"C:\...\Services\LaunchPadProjectPageParseService.cs", 1228) };
            var context2F = new AmbientFaultContext { CustomContext = ["Start Parsing", "driver", "padProject"], LogicalStackTrace = logicalStack2 };
            var context2E = new AmbientFaultContext { CustomContext = ["Project Parse Transaction"], InnerContext = context2F };
            var context2D = new AmbientFaultContext { CustomContext = ["When Existing Project Page Parsed"], InnerContext = context2E };
            var context2C = new AmbientFaultContext { CustomContext = ["Parse Upcoming Projects", "LaunchPad kommunitas"], InnerContext = context2D };
            var context2B = new AmbientFaultContext { CustomContext = ["Parse Up Coming", "LaunchPad kommunitas"], InnerContext = context2C };
            var context2A = new AmbientFaultContext { CustomContext = ["Schedule Launch Pad Parse", "Kommunitas Kommunitas"], InnerContext = context2B };
            var faultPath2 = new FaultHubException("Wrapper2", rootCause2, context2A);
            
            var aggregate = new AggregateException(faultPath1, faultPath2);
            var topLevelException = new FaultHubException("Parse Launch Pad failed", aggregate, new AmbientFaultContext());

            // ACT
            var reportModel = ReportParser.Parse(topLevelException);
            var reportString = ReportParser.Render(reportModel);
            
            // To aid debugging during a test run
            Console.WriteLine(reportString);

            // ASSERT: Verify the final rendered string matches the desired output structure.
            reportString.ShouldStartWith("Parse Launch Pad failed");
            reportString.ShouldContain("--- Failures (2) ---");
            
            // -- Verify Path 1 --
            reportString.ShouldContain("1. Failure Path:");
            reportString.ShouldContain("  - Operation: Schedule Launch Pad Parse (Kommunitas Kommunitas)");
            reportString.ShouldContain("    - Operation: Parse Up Coming (LaunchPad kommunitas)");
            reportString.ShouldContain("      - Operation: When Upcoming Urls");
            reportString.ShouldContain("      Root Cause: System.Exception: Upcoming");
            reportString.ShouldContain(@"() at WhenUpcomingUrls in C:\...\Services\LaunchPadProjectPageParseService.cs:line 582");
            reportString.ShouldContain("--- Original Exception Details ---");

            // -- Verify Path 2 --
            reportString.ShouldContain("2. Failure Path:");
            reportString.ShouldContain("        - Operation: Start Parsing (driver, padProject)");
            reportString.ShouldContain("        Root Cause: System.Exception: StartParsing");
        }
    }
}