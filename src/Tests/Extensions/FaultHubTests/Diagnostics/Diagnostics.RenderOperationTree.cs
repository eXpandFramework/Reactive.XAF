using System;
using System.Diagnostics.CodeAnalysis;
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
    }
}