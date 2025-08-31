using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Tests.FaultHubTests.FaultHubExtensionTest;

namespace Xpand.Extensions.Tests.FaultHubTests.Diagnostics {
    public class DiagnosticsRenderStackTests : FaultHubExtensionTestBase {

        [Test]
        public void RenderStack_Correctly_Formats_Logical_Stack() {
            var exception = CreateFaultWithLogicalStack(
                new LogicalStackFrame("MethodA", "fileA.cs", 10),
                new LogicalStackFrame("MethodB", "fileB.cs", 25)
            );
            var tree = exception.NewOperationTree();
            var expected = string.Join(Environment.NewLine,
                "--- Invocation Stack ---",
                "  at MethodA in fileA.cs:line 10",
                "  at MethodB in fileB.cs:line 25"
            );

            var result = tree.RenderStack();

            result.ShouldBe(expected);
        }

        [Test]
        public void RenderStack_On_Unioned_Node_Correctly_Renders_Merged_Stack() {
            var frame1 = new LogicalStackFrame("CommonMethod", "common.cs", 5);
            var frame2 = new LogicalStackFrame("UniqueMethodA", "fileA.cs", 15);
            var frame3 = new LogicalStackFrame("UniqueMethodB", "fileB.cs", 25);

            var ex1 = CreateFaultWithLogicalStack(frame1, frame2);
            var ex2 = CreateFaultWithLogicalStack(frame1, frame3);

            var tree1 = ex1.NewOperationTree();
            var tree2 = ex2.NewOperationTree();

            var unionedTree = new[] { tree1, tree2 }.Union();

            var result = unionedTree.RenderStack();

            result.ShouldContain("--- Invocation Stack ---");
            result.ShouldContain("at CommonMethod in common.cs:line 5");
            result.ShouldContain("at UniqueMethodA in fileA.cs:line 15");
            result.ShouldContain("at UniqueMethodB in fileB.cs:line 25");
    
            result.Count(c => c == '\n').ShouldBe(3); 
        }
        [Test]
        public void RenderStack_Returns_Empty_String_When_No_Logical_Stack_Is_Present() {
            var exception = CreateFaultWithLogicalStack();
            var tree = exception.NewOperationTree();

            var result = tree.RenderStack();

            result.ShouldBeEmpty();
        }

        [Test]
        [SuppressMessage("ReSharper", "ExpressionIsAlwaysNull")]
        public void RenderStack_Handles_Null_Input() {
            OperationNode tree = null;

            var result = tree.RenderStack();

            result.ShouldBeEmpty();
        }
        
        [Test]
        public void RenderStack_Handles_Frames_With_And_Without_Context() {
            var exception = CreateFaultWithLogicalStack(
                new LogicalStackFrame("MethodA", "fileA.cs", 10, "My Context"),
                new LogicalStackFrame("MethodB", "fileB.cs", 25)
            );
            var tree = exception.NewOperationTree();
            var expected = string.Join(Environment.NewLine,
                "--- Invocation Stack ---",
                "  (My Context) at MethodA in fileA.cs:line 10",
                "  at MethodB in fileB.cs:line 25"
            );

            var result = tree.RenderStack();

            result.ShouldBe(expected);
        }
        
         private FaultHubException CreateFaultWithLogicalStack(params LogicalStackFrame[] frames) {
            var context = new AmbientFaultContext {
                BoundaryName = "TestOperation",
                LogicalStackTrace = frames
            };
            return new FaultHubException("Test Failure", new InvalidOperationException("Root Cause"), context);
        }

        [Test]
        public void Render_Hides_Single_Blacklisted_Frame() {
            FaultHub.BlacklistedFilePathRegexes.Add(@"framework\\", "Framework Internals");
            var exception = CreateFaultWithLogicalStack(
                new LogicalStackFrame("AppMethod", @"C:\app\logic.cs", 10),
                new LogicalStackFrame("InternalMethod", @"C:\framework\internal.cs", 20)
            );

            var report = exception.Render();

            report.ShouldContain("AppMethod");
            report.ShouldNotContain("InternalMethod");
            report.ShouldContain("... 1 frame(s) hidden ...");
            FaultHub.BlacklistedFilePathRegexes.Clear();
        }

        [Test]
        public void Render_Collapses_Consecutive_Blacklisted_Frames() {
            FaultHub.BlacklistedFilePathRegexes.Add(@"framework\\", "Framework Internals");
            var exception = CreateFaultWithLogicalStack(
                new LogicalStackFrame("AppMethod1", @"C:\app\ui.cs", 10),
                new LogicalStackFrame("InternalMethod1", @"C:\framework\core.cs", 20),
                new LogicalStackFrame("InternalMethod2", @"C:\framework\utils.cs", 30),
                new LogicalStackFrame("AppMethod2", @"C:\app\data.cs", 40)
            );

            var report = exception.Render();

            report.ShouldContain("AppMethod1");
            report.ShouldContain("AppMethod2");
            report.ShouldNotContain("InternalMethod1");
            report.ShouldNotContain("InternalMethod2");
            report.ShouldContain("... 2 frame(s) hidden ...");
            FaultHub.BlacklistedFilePathRegexes.Clear();
        }

        [Test]
        public void Render_Handles_Non_Consecutive_Blacklisted_Frames() {
            FaultHub.BlacklistedFilePathRegexes.Add(@"framework\\", "Framework Internals");
            var exception = CreateFaultWithLogicalStack(
                new LogicalStackFrame("InternalMethod1", @"C:\framework\core.cs", 10),
                new LogicalStackFrame("AppMethod1", @"C:\app\ui.cs", 20),
                new LogicalStackFrame("InternalMethod2", @"C:\framework\utils.cs", 30),
                new LogicalStackFrame("AppMethod2", @"C:\app\data.cs", 40)
            );

            var report = exception.Render();

            report.ShouldContain("AppMethod1");
            report.ShouldContain("AppMethod2");
            report.ShouldNotContain("InternalMethod1");
            report.ShouldNotContain("InternalMethod2");
            
            var lines = report.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);
            lines.Count(l => l.Contains("... 1 frame(s) hidden ...")).ShouldBe(2);
            FaultHub.BlacklistedFilePathRegexes.Clear();
        }

        [Test]
        public void Render_Falls_Back_When_Blacklist_Would_Hide_All_Frames() {
            FaultHub.BlacklistedFilePathRegexes.Add(@"framework\\", "Framework Internals");
            var exception = CreateFaultWithLogicalStack(
                new LogicalStackFrame("InternalMethod1", @"C:\framework\core.cs", 20),
                new LogicalStackFrame("InternalMethod2", @"C:\framework\utils.cs", 30)
            );

            var report = exception.Render();

            report.ShouldContain("InternalMethod1");
            report.ShouldContain("InternalMethod2");
            report.ShouldContain("... (Fallback: All frames shown as the blacklist would hide the entire stack) ...");
            FaultHub.BlacklistedFilePathRegexes.Clear();
        }
    
    
        [Test]
        public void RenderStack_Does_Not_Show_Empty_Parentheses_For_Null_Context() {
            var exception = CreateFaultWithLogicalStack(
                new LogicalStackFrame("MethodA", "fileA.cs", 10, null)
            );
            var tree = exception.NewOperationTree();

            var result = tree.RenderStack();

            result.ShouldNotContain("()");
            result.ShouldContain("at MethodA in fileA.cs:line 10");
        }
    }
}