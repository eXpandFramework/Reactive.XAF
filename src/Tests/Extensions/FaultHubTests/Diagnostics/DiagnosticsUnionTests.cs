using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.FaultHub;

namespace Xpand.Extensions.Tests.FaultHubTests.Diagnostics {
    public class DiagnosticsUnionTests : FaultHubExtensionTestBase {
        [Test]
        public void Union_Merges_Two_Simple_Diverging_Paths() {
            var path1 = new OperationNode("Root", [], [new OperationNode("ChildA", [], [])
            ]);
            var path2 = new OperationNode("Root", [], [new OperationNode("ChildB", [], [])
            ]);
            var source = new[] { path1, path2 };

            var result = source.Union();

            result.ShouldNotBeNull();
            result.Name.ShouldBe("Root");
            result.Children.Count.ShouldBe(2);
            result.Children.ShouldContain(c => c.Name == "ChildA");
            result.Children.ShouldContain(c => c.Name == "ChildB");
        }

        [Test]
        public void Union_Merges_Complex_Nested_And_Branching_Paths() {
            var path1 = new OperationNode("Root", [], [new OperationNode("CommonChild", [], [new OperationNode("LeafA", [], [])])]);
            var path2 = new OperationNode("Root", [], [new OperationNode("CommonChild", [], [new OperationNode("LeafB", [], [])])]);
            var path3 = new OperationNode("Root", [], [new OperationNode("DifferentChild", [], [])]);
            var source = new[] { path1, path2, path3 };

            var result = source.Union();

            result.Name.ShouldBe("Root");
            result.Children.Count.ShouldBe(2);
            result.Children.ShouldContain(c => c.Name == "DifferentChild");
            
            var commonChild = result.Children.Single(c => c.Name == "CommonChild");
            commonChild.Children.Count.ShouldBe(2);
            commonChild.Children.ShouldContain(c => c.Name == "LeafA");
            commonChild.Children.ShouldContain(c => c.Name == "LeafB");
        }

        [Test]
        public void Union_Preserves_RootCauses_On_Diverging_Leaf_Nodes() {
            var ex1 = new InvalidOperationException("Failure A");
            var ex2 = new InvalidOperationException("Failure B");
            var path1 = new OperationNode("Root", [], [new OperationNode("FailingChildA", [], RootCause:ex1,Children: [])]);
            var path2 = new OperationNode("Root", [], [new OperationNode("FailingChildB", [], RootCause:ex2, Children:[])]);
            var source = new[] { path1, path2 };

            var result = source.Union();

            result.RootCause.ShouldBeNull();

            var childA = result.Children.Single(c => c.Name == "FailingChildA");
            childA.GetRootCause().ShouldBe(ex1);
            
            var childB = result.Children.Single(c => c.Name == "FailingChildB");
            childB.GetRootCause().ShouldBe(ex2);
        }
        
        [Test]
        public void Union_Creates_Virtual_Root_When_Roots_Are_Different() {
            var tree1 = new OperationNode("RootA", [], []);
            var tree2 = new OperationNode("RootB", [], []
                
                );
            var source = new[] { tree1, tree2 };

            var result = source.Union();

            result.ShouldNotBeNull();
            result.Name.ShouldBe("Multiple Operations");
            result.Children.Count.ShouldBe(2);
            result.Children.ShouldContain(tree1);
            result.Children.ShouldContain(tree2);
        }
        
        [Test]
        [SuppressMessage("ReSharper", "ExpressionIsAlwaysNull")]
        public void Union_Returns_Null_For_Empty_Or_Null_Input() {
            OperationNode[] emptySource = [];
            OperationNode[] nullSource = null;

            var resultFromEmpty = emptySource.Union();
            var resultFromNull = nullSource.Union();

            resultFromEmpty.ShouldBeNull();
            resultFromNull.ShouldBeNull();
        }

        [Test]
        public void Union_Returns_Original_Node_For_Single_Tree_Input() {
            var singleTree = new OperationNode("Root", [], []);
            var source = new[] { singleTree };

            var result = source.Union();

            result.ShouldBeSameAs(singleTree);
        }    
    }
}