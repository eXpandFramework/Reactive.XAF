using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Relay;

namespace Xpand.Extensions.Tests.FaultHubTests.Diagnostics {
    [TestFixture]
    public class DiagnosticsRenderOperationTreeTests : FaultHubExtensionTestBase {
        [Test]
        public void Render_Correctly_Formats_Linear_Tree() {
            var exception = CreateNestedFault(
                ("TopLevelOperation", null),
                ("MidLevelLogic", null),
                ("LowLevelData", null)
            );
            var tree = exception.OperationTree();
            var expected = string.Join(Environment.NewLine,
                "└ Top Level Operation",
                "  └ Mid Level Logic",
                "    └ Low Level Data"
            );

            var result = tree.Render();

            result.ShouldBe(expected);
        }

        [Test]
        public void Render_Correctly_Formats_Branching_Tree() {
            var leaf2A = new OperationNode("Leaf2A", [], []);
            var child1 = new OperationNode("Child1", [], []);
            var child2 = new OperationNode("Child2", [], [leaf2A]);
            var child3 = new OperationNode("Child3", [], []);
            var root = new OperationNode("RootNode", [], [child1, child2, child3]);
    
            var expected = string.Join(Environment.NewLine,
                "└ Root Node",
                "  ├ Child1",
                "  ├ Child2",
                "  │ └ Leaf2 A",
                "  └ Child3"
            );

            var result = root.Render();

            result.ShouldBe(expected);
        }

        [Test]
        public void Render_Correctly_Formats_Single_Node_Tree() {
            var tree = new OperationNode("SingleOperation", [], []);
            var expected = "└ Single Operation";

            var result = tree.Render();

            result.ShouldBe(expected);
        }

        [Test]
        [SuppressMessage("ReSharper", "ExpressionIsAlwaysNull")]
        public void Render_Returns_Empty_String_For_Null_Input() {
            OperationNode tree = null;

            var result = tree.Render();

            result.ShouldBeEmpty();
        }
        
        [Test]
        public void Render_Does_Not_Display_Empty_Parentheses_For_ValueTuple_Context() {
            var headerContext = new AmbientFaultContext {
                BoundaryName = "TestOperation",
                UserContext = [ValueTuple.Create()]
            };
            var exception = new FaultHubException("Test Failure", 
                new FaultHubException("Inner", new InvalidOperationException("Root Cause"), 
                    new AmbientFaultContext{ LogicalStackTrace = [
                        new LogicalStackFrame("MethodWithValueTupleContext", "file.cs", 10, [ValueTuple.Create()]) 
                    ]}), 
                headerContext);

            var report = exception.Render();
            Console.WriteLine(report);

            var reportLines = report.Split([Environment.NewLine], StringSplitOptions.None);
    
            var headerLine = reportLines.First();
            headerLine.ShouldBe("Test Operation completed with errors <Root Cause>");
            headerLine.ShouldNotContain("()");

            var frameLine = reportLines.Single(l => l.Contains("MethodWithValueTupleContext"));
            frameLine.Trim().ShouldStartWith("at MethodWithValueTupleContext");
            frameLine.ShouldNotContain("()");
        }

    }
}