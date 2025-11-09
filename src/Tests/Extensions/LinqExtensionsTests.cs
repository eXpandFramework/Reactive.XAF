using System;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.Tests {
    public class FromHierarchyAllTests {
        private class Node {
            public string Name { get; init; }
            public Node[] Parents { get; set; } = [];
        }

        [Test]
        public void Returns_Empty_When_Circular_Dependency_Is_Detected_And_Strategy_Is_Ignore() {
            var nodeA = new Node { Name = "A" };
            var nodeB = new Node { Name = "B" };
            nodeA.Parents = [nodeB];
            nodeB.Parents = [nodeA];

            Node[] ParentSelector(Node n) => n.Parents;

            var paths = nodeA.FromHierarchyAll((Func<Node, Node[]>)ParentSelector, cycleDetectionStrategy: CycleDetectionStrategy.Ignore).ToList();

            paths.ShouldBeEmpty();
        }

        [Test]
        public void Throws_CircularDependencyException_When_Circular_Dependency_Is_Detected_And_Strategy_Is_Throw() {
            var nodeA = new Node { Name = "A" };
            var nodeB = new Node { Name = "B" };
            nodeA.Parents = [nodeB];
            nodeB.Parents = [nodeA];

            Func<Node, Node[]> parentSelector = n => n.Parents;

            Should.Throw<ExceptionExtensions.ExceptionExtensions.CircularDependencyException>(() => {
                nodeA.FromHierarchyAll(parentSelector, cycleDetectionStrategy: CycleDetectionStrategy.Throw).Enumerate();
            });
        }
        [Test]
        public void Finds_Correct_Path_In_Simple_Linear_Hierarchy() {
            var nodeC = new Node { Name = "C" };
            var nodeB = new Node { Name = "B", Parents = [nodeC] };
            var nodeA = new Node { Name = "A", Parents = [nodeB] };

            Node[] ParentSelector(Node n) => n.Parents;

            var paths = nodeA.FromHierarchyAll((Func<Node, Node[]>)ParentSelector).ToList();

            paths.ShouldHaveSingleItem();
            var path = paths.Single();
            path.Select(n => n.Name).ShouldBe(["A", "B", "C"]);
        }

        [Test]
        public void Correctly_Handles_Multiple_Paths_And_Shared_Dependencies_In_DAG() {
            var nodeE = new Node { Name = "E" };
            var nodeF = new Node { Name = "F" };
            var nodeC = new Node { Name = "C", Parents = [nodeE] };
            var nodeD = new Node { Name = "D", Parents = [nodeF, nodeC] };
            var nodeB = new Node { Name = "B", Parents = [nodeC, nodeD] };
            var nodeA = new Node { Name = "A", Parents = [nodeB] };

            Node[] ParentSelector(Node n) => n.Parents;

            var paths = nodeA.FromHierarchyAll((Func<Node, Node[]>)ParentSelector).ToList();

            paths.Count.ShouldBe(3);
            paths.Select(p => string.Join("->", p.Select(n => n.Name))).ShouldBe(["A->B->C->E", "A->B->D->F", "A->B->D->C->E"], ignoreOrder: true);
        }
    }
}