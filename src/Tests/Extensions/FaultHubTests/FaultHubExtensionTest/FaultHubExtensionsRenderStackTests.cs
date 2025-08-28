using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;

namespace Xpand.Extensions.Tests.FaultHubTests.FaultHubExtensionTest{
    public class FaultHubExtensionsRenderStackTests : FaultHubExtensionTestBase {
        private FaultHubException CreateFaultWithLogicalStack(params LogicalStackFrame[] frames) {
            var context = new AmbientFaultContext {
                BoundaryName = "TestOperation",
                LogicalStackTrace = frames
            };
            return new FaultHubException("Test Failure", new InvalidOperationException("Root Cause"), context);
        }

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
    }
}