using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.ExceptionExtensions;
using Exception = System.Exception;

namespace Xpand.Extensions.Tests {
    [TestFixture]
    public class ExceptionExtensionsTests {
        [Test]
        public void Flatten_WithSingleException_ReturnsItself() {
            var singleException = new InvalidOperationException("Test");

            var result = singleException.SelectMany();

            result.ShouldHaveSingleItem().ShouldBe(singleException);
        }

        [Test]
        [SuppressMessage("ReSharper", "NotResolvedInText")]
        public void Flatten_WithNestedInnerException_ReturnsAllException() {
            var innermostException = new ArgumentNullException("param");
            var middleException = new InvalidOperationException("Middle", innermostException);
            var outerException = new Exception("Outer", middleException);

            var result = outerException.SelectMany().ToArray();

            result.Length.ShouldBe(3);
        }

        [Test]
        public void Flatten_WithSimpleAggregateException_ReturnsAllInnerExceptions() {
            var ex1 = new InvalidOperationException();
            var ex2 = new ArgumentNullException();
            var aggregateException = new AggregateException(ex1, ex2);

            var result = aggregateException.SelectMany().ToList();

            result.Count.ShouldBe(3);
            result.ShouldContain(ex1);
            result.ShouldContain(ex2);
        }

        [Test]
        public void Flatten_WithNestedAggregateException_ReturnsAllLeafExceptions() {
            var ex1 = new InvalidOperationException();
            var ex2 = new ArgumentNullException();
            var innerAggregate = new AggregateException(ex1, ex2);
            var ex3 = new FormatException();
            var outerAggregate = new AggregateException(innerAggregate, ex3);

            var result = outerAggregate.SelectMany().ToList();

            result.Count.ShouldBe(5);
            result.ShouldContain(ex1);
            result.ShouldContain(ex2);
            result.ShouldContain(ex3);
        }

        [Test]
        public void Flatten_WithMixedAggregateAndNestedException_ReturnsAllExceptions() {
            var ex1 = new InvalidOperationException();
            var nestedInnermost = new ApplicationException("Innermost");
            var exWithInner = new FormatException("Outer nested", nestedInnermost);
            var ex3 = new ArgumentNullException();
            var aggregateException = new AggregateException(ex1, exWithInner, ex3);

            var result = aggregateException.SelectMany().ToList();

            result.Count.ShouldBe(5);
            result.ShouldContain(ex1);
            result.ShouldContain(nestedInnermost);
            result.ShouldContain(ex3);
            result.ShouldContain(exWithInner);
            result.ShouldContain(aggregateException);
        }

        [Test]
        public void SelectMany_OnAggregateException_BehavesSameAsFlatten() {
            var ex1 = new InvalidOperationException();
            var ex2 = new ArgumentNullException();
            var innerAggregate = new AggregateException(ex1, ex2);
            var ex3 = new FormatException();
            var outerAggregate = new AggregateException(innerAggregate, ex3);

            var resultFromSelectMany = outerAggregate.SelectMany().ToList();
            var resultFromFlatten = outerAggregate.SelectMany().ToList();

            resultFromSelectMany.ShouldBe(resultFromFlatten, true);
        }

        [Test]
        public void Parent_ShouldReturnDirectParent_WhenTargetIsInnerException() {
            var child = new InvalidOperationException("Child");
            var parent = new Exception("Parent", child);

            var result = parent.Parent(child);

            result.ShouldBeSameAs(parent);
        }

        [Test]
        [SuppressMessage("ReSharper", "NotResolvedInText")]
        public void Parent_ShouldReturnAggregateException_WhenTargetIsInInnerExceptions() {
            var child1 = new InvalidOperationException("Child 1");
            var child2 = new ArgumentNullException("Child 2");
            var parent = new AggregateException("Parent", child1, child2);

            var result = parent.Parent(child2);

            result.ShouldBeSameAs(parent);        }


        [Test]
        public void Parent_ShouldReturnNull_WhenExceptionsAreUnrelated() {
            var unrelated = new InvalidOperationException("Unrelated");
            var root = new Exception("Root", new Exception("Child"));

            var result = root.Parent(unrelated);

            result.ShouldBeNull();
        }

        [Test]
        public void Parent_ShouldReturnNull_WhenTargetIsTheRootException() {
            var root = new Exception("Root", new Exception("Child"));

            var result = root.Parent(root);

            result.ShouldBeNull();
        }
        
        [Test]
        public void Parent_ShouldReturnNull_WhenImmediateParentIsTopLevelAggregate() {
            var child = new InvalidOperationException("Child");
            var topLevelAggregate = new AggregateException("Parent", child);

            var result = topLevelAggregate.Parent(child,exception => exception is AggregateException);

            result.ShouldBeNull();
        }

        [Test]
        public void Parent_ShouldSkipSingleAggregate_AndReturnGrandparent() {
            var child = new InvalidOperationException("Child");
            var middleAggregate = new AggregateException("Middle", child);
            var grandparent = new Exception("Grandparent", middleAggregate);

            var result = grandparent.Parent(child,exception => exception is AggregateException);

            result.ShouldBeSameAs(grandparent);
        }

        [Test]
        public void Parent_ShouldSkipMultipleAggregates_AndReturnFirstNonAggregateAncestor() {
            var child = new InvalidOperationException("Child");
            var innerAggregate = new AggregateException("Inner", child);
            var outerAggregate = new AggregateException("Outer", innerAggregate);
            var ancestor = new Exception("Ancestor", outerAggregate);

            var result = ancestor.Parent(child,exception => exception is AggregateException);

            result.ShouldBeSameAs(ancestor);
        }

        [Test]
        public void FailurePath_WithSimpleInnerException_ReturnsParent() {
            var child = new InvalidOperationException("Child");
            var parent = new Exception("Parent", child);

            var path = parent.FailurePath(child).ToList();

            path.ShouldHaveSingleItem();
            path[0].ShouldBeSameAs(parent);
        }

        [Test]
        [SuppressMessage("ReSharper", "NotResolvedInText")]
        public void FailurePath_WithMultiLevelInnerException_ReturnsPathToRoot() {
            var innermost = new ArgumentNullException("Innermost");
            var middle = new InvalidOperationException("Middle", innermost);
            var outer = new Exception("Outer", middle);

            var path = outer.FailurePath(innermost).ToList();

            path.Count.ShouldBe(2);
            path[0].ShouldBeSameAs(middle);
            path[1].ShouldBeSameAs(outer);
        }

        [Test]
        [SuppressMessage("ReSharper", "NotResolvedInText")]
        public void FailurePath_WithAggregateException_ReturnsParent() {
            var child1 = new InvalidOperationException("Child 1");
            var child2 = new ArgumentNullException("Child 2");
            var parent = new AggregateException("Parent", child1, child2);

            var path = parent.FailurePath(child2).ToList();

            path.ShouldHaveSingleItem();
            path[0].ShouldBeSameAs(parent);
        }

        [Test]
        [SuppressMessage("ReSharper", "NotResolvedInText")]
        public void FailurePath_WithMixedAggregateAndInnerException_ReturnsPathToRoot() {
            var leaf = new FormatException("Leaf");
            var child1 = new InvalidOperationException("Child 1", leaf);
            var child2 = new ArgumentNullException("Child 2");
            var middleAggregate = new AggregateException("Middle", child1, child2);
            var outer = new Exception("Outer", middleAggregate);

            var path = outer.FailurePath(leaf).ToList();

            path.Count.ShouldBe(3);
            path[0].ShouldBeSameAs(child1);
            path[1].ShouldBeSameAs(middleAggregate);
            path[2].ShouldBeSameAs(outer);
        }

        [Test]
        public void FailurePath_WhenRootCauseIsTopLevel_ReturnsEmptyPath() {
            var child = new InvalidOperationException("Child");
            var parent = new Exception("Parent", child);

            var path = parent.FailurePath(parent).ToList();

            path.ShouldBeEmpty();
        }

        [Test]
        public void FailurePath_WhenRootCauseIsNotInGraph_ReturnsEmptyPath() {
            var unrelated = new ApplicationException("Unrelated");
            var child = new InvalidOperationException("Child");
            var parent = new Exception("Parent", child);

            var path = parent.FailurePath(unrelated).ToList();

            path.ShouldBeEmpty();
        }
        
        [Test]
        public void FailurePath_WithTypesToExclude_CorrectlyFiltersPath() {
            var leaf = new InvalidOperationException("Leaf");
            var innerAggregate = new AggregateException("Inner", leaf);
            var middleWrapper = new TargetInvocationException("Middle", innerAggregate); 
            var outer = new Exception("Outer", middleWrapper);

            var defaultPath = outer.FailurePath(leaf,exception => exception is AggregateException).ToList();
    
            defaultPath.Count.ShouldBe(2, "Default behavior should exclude only AggregateException");
            defaultPath[0].ShouldBeSameAs(middleWrapper);
            defaultPath[1].ShouldBeSameAs(outer);

            var customPath = outer.FailurePath(leaf,exception => exception is AggregateException or TargetInvocationException).ToList();
    
            customPath.ShouldHaveSingleItem("Custom path should exclude both specified types");
            customPath[0].ShouldBeSameAs(outer);

            var fullPath = outer.FailurePath(leaf).ToList();
    
            fullPath.Count.ShouldBe(3, "Passing an empty array should result in no exclusions");
            fullPath[0].ShouldBeSameAs(innerAggregate);
            fullPath[1].ShouldBeSameAs(middleWrapper);
            fullPath[2].ShouldBeSameAs(outer);
        }
    }
}