using System;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;

namespace Xpand.Extensions.Tests.FaultHubTests.FaultHubExtensionTest{
    public class FaultHubExtensionsNewOperationTreeTests:FaultHubExtensionTestBase {
         [Test]
        public void Parses_Simple_Linear_Chain_Correctly() {
            var rootCause = new InvalidOperationException("DB Error");
            var ctxC = new AmbientFaultContext { BoundaryName = "DataAccessLayer" };
            var fhC = new FaultHubException("C failed", rootCause, ctxC);
            var ctxB = new AmbientFaultContext { BoundaryName = "BusinessLogicLayer" };
            var fhB = new FaultHubException("B failed", fhC, ctxB);
            var ctxA = new AmbientFaultContext { BoundaryName = "ApiLayer" };
            var fhA = new FaultHubException("A failed", fhB, ctxA);

            var result = fhA.NewOperationTree();

            result.Name.ShouldBe("ApiLayer");
            result.Children.ShouldHaveSingleItem();
            var nodeB = result.Children.Single();
            nodeB.Name.ShouldBe("BusinessLogicLayer");
            nodeB.Children.ShouldHaveSingleItem();
            var nodeC = nodeB.Children.Single();
            nodeC.Name.ShouldBe("DataAccessLayer");
            nodeC.GetRootCause().ShouldBe(rootCause);
        }

        [Test]
        public void Parses_Branching_AggregateException_Correctly() {
            var exA = new InvalidOperationException("Failure A");
            var ctxA = new AmbientFaultContext { BoundaryName = "BranchA" };
            var fhA = new FaultHubException("A failed", exA, ctxA);

            var exB = new InvalidOperationException("Failure B");
            var ctxB = new AmbientFaultContext { BoundaryName = "BranchB" };
            var fhB = new FaultHubException("B failed", exB, ctxB);

            var aggEx = new AggregateException(fhA, fhB);
            var ctxRoot = new AmbientFaultContext { BoundaryName = "RootOperation" };
            var fhRoot = new FaultHubException("Root failed", aggEx, ctxRoot);

            var result = fhRoot.NewOperationTree();

            result.Name.ShouldBe("RootOperation");
            result.Children.Count.ShouldBe(2);

            var branchA = result.Children.Single(c => c.Name == "BranchA");
            branchA.GetRootCause().ShouldBe(exA);

            var branchB = result.Children.Single(c => c.Name == "BranchB");
            branchB.GetRootCause().ShouldBe(exB);
        }

        [Test]
        public void Parses_Production_Scenario_Structure_Correctly() {
            var exception = CreateProductionScenarioException();

            var result = exception.NewOperationTree();

            result.Name.ShouldBe("ScheduleLaunchPadParse");
            var parseLaunchPad = result.Children.Single();
            parseLaunchPad.Name.ShouldBe("ParseLaunchPad");
            var connect = parseLaunchPad.Children.Single();
            connect.Name.ShouldBe("ConnectLaunchPad");
            var parseUpcoming = connect.Children.Single();
            parseUpcoming.Name.ShouldBe("ParseUpComing");

            parseUpcoming.Children.Count.ShouldBe(2);

            var whenUrls = parseUpcoming.Children.Single(c => c.Name == "WhenUpcomingUrls");
            var webSite = whenUrls.Children.Single();
            webSite.Name.ShouldBe("WebSiteUrls");
            webSite.GetRootCause().Message.ShouldBe("Upcoming");

            var parseProjects = parseUpcoming.Children.Single(c => c.Name == "ParseUpcomingProjects");
            var startParsing = parseProjects.GetChild("WhenExistingProjectPageParsed")
                                            .GetChild("ProjectParseTransaction")
                                            .GetChild("StartParsing");
            startParsing.GetRootCause().Message.ShouldBe("StartParsing");
        }

        [Test]
        public void Ignores_Nodes_Without_BoundaryName() {
            var rootCause = new InvalidOperationException("DB Error");
            var ctxC = new AmbientFaultContext { BoundaryName = "FinalStep" };
            var fhC = new FaultHubException("C failed", rootCause, ctxC);
            var ctxNoName = new AmbientFaultContext { BoundaryName = null };
            var fhNoName = new FaultHubException("NoName failed", fhC, ctxNoName);
            var ctxA = new AmbientFaultContext { BoundaryName = "FirstStep" };
            var fhA = new FaultHubException("A failed", fhNoName, ctxA);

            var result = fhA.NewOperationTree();

            result.Name.ShouldBe("FirstStep");
            result.Children.ShouldHaveSingleItem();
            var finalStep = result.Children.Single();
            finalStep.Name.ShouldBe("FinalStep");
            finalStep.GetRootCause().ShouldBe(rootCause);
        }

        private FaultHubException CreateProductionScenarioException() {
            var upcomingEx = new InvalidOperationException("Upcoming");
            var ctxWebSite = new AmbientFaultContext { BoundaryName = "WebSiteUrls" };
            var fhWebSite = new FaultHubException("WebSiteUrls failed", upcomingEx, ctxWebSite);
            var ctxWhenUpcoming = new AmbientFaultContext { BoundaryName = "WhenUpcomingUrls" };
            var fhWhenUpcoming = new FaultHubException("WhenUpcomingUrls failed", fhWebSite, ctxWhenUpcoming);
            
            var startParsingEx = new InvalidOperationException("StartParsing");

            var ctxStartParsing = new AmbientFaultContext { BoundaryName = "StartParsing" };
            var fhStartParsing = new FaultHubException("StartParsing failed", startParsingEx, ctxStartParsing);
            var ctxProjectTx = new AmbientFaultContext { BoundaryName = "ProjectParseTransaction" };
            var fhProjectTx = new FaultHubException("ProjectParseTransaction failed", fhStartParsing, ctxProjectTx);
            var ctxWhenExisting = new AmbientFaultContext { BoundaryName = "WhenExistingProjectPageParsed" };
            var fhWhenExisting = new FaultHubException("WhenExisting... failed", fhProjectTx, ctxWhenExisting);
            var ctxParseProjects = new AmbientFaultContext { BoundaryName = "ParseUpcomingProjects" };
            var fhParseProjects = new FaultHubException("ParseUpcomingProjects failed", fhWhenExisting, ctxParseProjects);

            var aggEx = new AggregateException(fhWhenUpcoming, fhParseProjects);
            var ctxParseUpcoming = new AmbientFaultContext { BoundaryName = "ParseUpComing" };
            var fhParseUpcoming = new FaultHubException("ParseUpComing failed", aggEx, ctxParseUpcoming);
            var ctxConnect = new AmbientFaultContext { BoundaryName = "ConnectLaunchPad" };
            var fhConnect = new FaultHubException("Connect... failed", fhParseUpcoming, ctxConnect);
            var ctxParseLaunchPad = new AmbientFaultContext { BoundaryName = "ParseLaunchPad" };
            var fhParseLaunchPad = new FaultHubException("ParseLaunchPad failed", fhConnect, ctxParseLaunchPad);
            var ctxSchedule = new AmbientFaultContext { BoundaryName = "ScheduleLaunchPadParse" };
            
            return new FaultHubException("ScheduleLaunchPadParse failed", fhParseLaunchPad, ctxSchedule);
        }
        [Test]
        public void RootCause_Returns_Exception_When_Called_From_Root_Node() {
            var exception = CreateNestedFault(("Level 1", null), ("Level 2", null));
            var tree = exception.NewOperationTree();

            var rootCause = tree.GetRootCause();

            rootCause.ShouldNotBeNull();
            rootCause.ShouldBeOfType<InvalidOperationException>();
            rootCause.Message.ShouldBe("Innermost failure");
        }
        [Test]
        public void RootCause_Returns_Null_When_No_Exception_Is_Present_In_Tree() {
            var leaf = new OperationNode("Leaf", [], []);
            var root = new OperationNode("Root", [], [leaf]);

            var rootCause = root.GetRootCause();

            rootCause.ShouldBeNull();
        }

        [Test]
        public void Builds_Correct_Linear_Tree_From_Nested_Exception() {
            var exception = CreateNestedFault(
                ("Level 1 Operation", null),
                ("Level 2 Business Logic", null),
                ("Level 3 Data Access", null)
            );

            var result = exception.NewOperationTree();

            result.ShouldNotBeNull();
            result.Name.ShouldBe("Level 1 Operation");
            result.Children.ShouldHaveSingleItem();

            var level2Node = result.Children.Single();
            level2Node.Name.ShouldBe("Level 2 Business Logic");
            level2Node.Children.ShouldHaveSingleItem();

            var level3Node = level2Node.Children.Single();
            level3Node.Name.ShouldBe("Level 3 Data Access");
            level3Node.Children.ShouldBeEmpty();
        }

        [Test]
        public void Populates_ContextData_Correctly_In_Tree_Nodes() {
            var exception = CreateNestedFault(
                ("Process Order", ["OrderID: 123"]),
                ("Validate Customer", ["CustomerID: 456", true])
            );

            var result = exception.NewOperationTree();

            result.ShouldNotBeNull();
            result.Name.ShouldBe("Process Order");
            result.ContextData.ShouldBe(["OrderID: 123"]);

            var childNode = result.Children.Single();
            childNode.Name.ShouldBe("Validate Customer");
            childNode.ContextData.ShouldBe(["CustomerID: 456", true]);
        }

        [Test]
        public void Returns_Single_Node_For_Non_Nested_Exception() {
            var exception = CreateNestedFault(
                ("Single Operation", ["Data"])
            );

            var result = exception.NewOperationTree();

            result.ShouldNotBeNull();
            result.Name.ShouldBe("Single Operation");
            result.ContextData.ShouldHaveSingleItem();
            result.Children.ShouldBeEmpty();
        }

        [Test]
        public void Returns_Null_If_No_Valid_Context_Is_Found() {
            var context = new AmbientFaultContext { BoundaryName = null, UserContext = null };
            var exception = new FaultHubException("Test", new Exception(), context);

            var result = exception.NewOperationTree();

            result.ShouldBeNull();
        }

        [Test]
        public void NewOperationTree_Preserves_Nested_Logical_Stack_Instead_Of_Overwriting() {
            var innerEx = new InvalidOperationException("Root Cause");
            var innerStack = new[] { new LogicalStackFrame("InnerWork", "inner.cs", 100) };
            var innerCtx = new AmbientFaultContext { BoundaryName = "InnerBoundary", LogicalStackTrace = innerStack };
            var fhInner = new FaultHubException("Inner fail", innerEx, innerCtx);

            var outerStack = new[] { new LogicalStackFrame("OuterWork", "outer.cs", 20) };
            var outerCtx = new AmbientFaultContext { BoundaryName = "OuterBoundary", LogicalStackTrace = outerStack, InnerContext = fhInner.Context};
            var fhOuter = new FaultHubException("Outer fail", fhInner, outerCtx);

            var result = fhOuter.NewOperationTree();

            result.ShouldNotBeNull();
            result.Name.ShouldBe("OuterBoundary");
            
            var innerNode = result.Children.ShouldHaveSingleItem();
            innerNode.Name.ShouldBe("InnerBoundary");

            var logicalStack = innerNode.GetLogicalStack();

            var expectedStack = innerStack.Concat(outerStack).ToList();
            logicalStack.ShouldBe(expectedStack);
        }
    }
}