using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tests.FaultHubTests.FaultHubExtensionTest;

namespace Xpand.Extensions.Tests.FaultHubTests.Diagnostics {
    public class DiagnosticsNewOperationTreeTests:FaultHubExtensionTestBase {
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
        public void Parses_Web_Scraping_Scenario_Structure_Correctly() {
            var exception = CreateWebScrapingScenarioException();
            var result = exception.NewOperationTree();

            result.Name.ShouldBe("ScheduleWebScraping");
            var parseHomePage = result.Children.Single();
            parseHomePage.Name.ShouldBe("ParseHomePage");
            var navigateToPage = parseHomePage.Children.Single();
            navigateToPage.Name.ShouldBe("NavigateToPage");
            var extractAndProcess = navigateToPage.Children.Single();
            extractAndProcess.Name.ShouldBe("ExtractAndProcessLinks");
            extractAndProcess.Children.Count.ShouldBe(2);

            var getPageLinks = extractAndProcess.Children.Single(c => c.Name == "GetPageLinks");
            var fetchUrls = getPageLinks.Children.Single();
            fetchUrls.Name.ShouldBe("FetchInitialUrls");
            fetchUrls.GetRootCause().Message.ShouldBe("Failed to fetch URLs");
            var scrapeData = extractAndProcess.Children.Single(c => c.Name == "ScrapeDataFromLinks");
            var extractContent = scrapeData.GetChild("WhenLinkScraped")
                                            .GetChild("DataExtractionTransaction")
                                            .GetChild("ExtractContent");
            extractContent.GetRootCause().Message.ShouldBe("Failed to extract content");
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

        private FaultHubException CreateWebScrapingScenarioException() {
            var urlsEx = new InvalidOperationException("Failed to fetch URLs");
            var ctxFetchUrls = new AmbientFaultContext { BoundaryName = "FetchInitialUrls" };
            var fhFetchUrls = new FaultHubException("FetchInitialUrls failed", urlsEx, ctxFetchUrls);
            var ctxGetPageLinks = new AmbientFaultContext { BoundaryName = "GetPageLinks" };
            var fhGetPageLinks = new FaultHubException("GetPageLinks failed", fhFetchUrls, ctxGetPageLinks);
            var extractContentEx = new InvalidOperationException("Failed to extract content");

            var ctxExtractContent = new AmbientFaultContext { BoundaryName = "ExtractContent" };
            var fhExtractContent = new FaultHubException("ExtractContent failed", extractContentEx, ctxExtractContent);
            var ctxDataExtractionTx = new AmbientFaultContext { BoundaryName = "DataExtractionTransaction" };
            var fhDataExtractionTx = new FaultHubException("DataExtractionTransaction failed", fhExtractContent, ctxDataExtractionTx);
            var ctxWhenLinkScraped = new AmbientFaultContext { BoundaryName = "WhenLinkScraped" };
            var fhWhenLinkScraped = new FaultHubException("WhenLinkScraped... failed", fhDataExtractionTx, ctxWhenLinkScraped);
            var ctxScrapeData = new AmbientFaultContext { BoundaryName = "ScrapeDataFromLinks" };
            var fhScrapeData = new FaultHubException("ScrapeDataFromLinks failed", fhWhenLinkScraped, ctxScrapeData);

            var aggEx = new AggregateException(fhGetPageLinks, fhScrapeData);
            var ctxExtractAndProcess = new AmbientFaultContext { BoundaryName = "ExtractAndProcessLinks" };
            var fhExtractAndProcess = new FaultHubException("ExtractAndProcessLinks failed", aggEx, ctxExtractAndProcess);
            var ctxNavigateToPage = new AmbientFaultContext { BoundaryName = "NavigateToPage" };
            var fhNavigateToPage = new FaultHubException("Navigate... failed", fhExtractAndProcess, ctxNavigateToPage);
            var ctxParseHomePage = new AmbientFaultContext { BoundaryName = "ParseHomePage" };
            var fhParseHomePage = new FaultHubException("ParseHomePage failed", fhNavigateToPage, ctxParseHomePage);
            var ctxSchedule = new AmbientFaultContext { BoundaryName = "ScheduleWebScraping" };
            
            return new FaultHubException("ScheduleWebScraping failed", fhParseHomePage, ctxSchedule);
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> Level3_Fails() =>
            Observable.Throw<Unit>(new InvalidOperationException("Root Cause"))
                .PushStackFrame()
                .ChainFaultContext();
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> Level2_Calls_Level3() =>
            Level3_Fails()
                .PushStackFrame()
                .ChainFaultContext();
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> Level1_Calls_Level2() =>
            Level2_Calls_Level3()
                .PushStackFrame()
                .ChainFaultContext();
        [Test]
        public void NewOperationTree_Builds_Full_Hierarchy_From_Live_ChainFaultContext_Operators() {
            FaultHubException capturedFault = null;
            Level1_Calls_Level2()
                .Catch((FaultHubException ex) => {
                    capturedFault = ex;
                    return Observable.Empty<Unit>();
                })
                .Subscribe();
            var fault = capturedFault.ShouldNotBeNull();
            
            var tree = fault.NewOperationTree();

            tree.ShouldNotBeNull();
            
            tree.Name.ShouldBe(nameof(Level1_Calls_Level2));
            
            var nodeB = tree.Children.ShouldHaveSingleItem();
            nodeB.Name.ShouldBe(nameof(Level2_Calls_Level3));

            var nodeC = nodeB.Children.ShouldHaveSingleItem();
            nodeC.Name.ShouldBe(nameof(Level3_Fails));
            nodeC.GetRootCause().ShouldBeOfType<InvalidOperationException>();
        }

        [Test]
        public void NewOperationTree_Handles_Nested_AggregateExceptions_And_Coalesces_Redundant_Nodes() {
            var exA = new InvalidOperationException("Failure A");
            var ctxLeafA = new AmbientFaultContext { BoundaryName = "LeafA" };
            var fhLeafA = new FaultHubException("A failed", exA, ctxLeafA);
            var ctxRedundant1 = new AmbientFaultContext { BoundaryName = "RedundantNode", InnerContext = fhLeafA.Context };
            var fhRedundant1 = new FaultHubException("Redundant 1", fhLeafA, ctxRedundant1);

            var ctxRedundant2 = new AmbientFaultContext { BoundaryName = "RedundantNode", InnerContext = fhRedundant1.Context };
            var fhRedundant2 = new FaultHubException("Redundant 2", fhRedundant1, ctxRedundant2);

            var exB = new InvalidOperationException("Failure B");
            var ctxLeafB = new AmbientFaultContext { BoundaryName = "LeafB" };
            var fhLeafB = new FaultHubException("B failed", exB, ctxLeafB);
            var aggEx = new AggregateException(fhRedundant2, fhLeafB);
            var ctxTop = new AmbientFaultContext { BoundaryName = "TopLevel" };
            var topException = new FaultHubException("Top Level Failed", aggEx, ctxTop);

            var result = topException.NewOperationTree();

            result.ShouldNotBeNull();
            result.Name.ShouldBe("TopLevel");

            result.Children.Count.ShouldBe(2);
            var branchA = result.Children.Single(n => n.Name == "RedundantNode");
            var leafA = branchA.Children.ShouldHaveSingleItem();
            leafA.Name.ShouldBe("LeafA");
            leafA.GetRootCause().ShouldBe(exA);
            var branchB = result.Children.Single(n => n.Name == "LeafB");
            branchB.GetRootCause().ShouldBe(exB);
        }
        
        [Test]
        public void NewOperationTree_Creates_Root_Node_With_Stack_But_No_Children_From_Single_Boundary() {
            var rootCause = new InvalidOperationException("DB Failure");
            var logicalStack = new[] {
                new LogicalStackFrame("InnerWork", "data.cs", 50),
                new LogicalStackFrame("GetCustomer", "logic.cs", 25, "CustomerID: 42")
            };
            var context = new AmbientFaultContext {
                BoundaryName = "FetchOperation",
                LogicalStackTrace = logicalStack,
                InnerContext = null
            };
            var exception = new FaultHubException("Operation Failed", rootCause, context);

            var result = exception.NewOperationTree();

            result.ShouldNotBeNull();
            result.Name.ShouldBe("FetchOperation");
            result.Children.ShouldBeEmpty("A single context without an InnerContext should not produce child nodes.");
            result.GetRootCause().ShouldBe(rootCause);

            var resultStack = result.GetLogicalStack();
            resultStack.ShouldBe(logicalStack);
        }
        
        [Test]
        public void NewOperationTree_Does_Not_Pollute_Parallel_Branches_With_Parent_Stack() {
            var exA = new InvalidOperationException("Failure A");
            var fhA = new FaultHubException("A failed", exA, new AmbientFaultContext {
                BoundaryName = "BranchA",
                LogicalStackTrace = [new LogicalStackFrame("FrameA", "", 0)]
            });
            var exB = new InvalidOperationException("Failure B");
            var fhB = new FaultHubException("B failed", exB, new AmbientFaultContext {
                BoundaryName = "BranchB",
                LogicalStackTrace = [new LogicalStackFrame("FrameB", "", 0)]
            });
            var aggEx = new AggregateException(fhA, fhB);
            var topException = new FaultHubException("Root failed", aggEx, new AmbientFaultContext {
                BoundaryName = "RootOperation",
                LogicalStackTrace = [new LogicalStackFrame("RootFrame", "", 0)]
            });
            var result = topException.NewOperationTree();

            var branchA = result.Children.Single(c => c.Name == "BranchA");
            var branchAStack = branchA.GetLogicalStack();
            branchAStack.ShouldNotContain(f => f.MemberName == "RootFrame",
                "The stack for Branch A was polluted by the root's stack frame.");
            branchAStack.ShouldContain(f => f.MemberName == "FrameA");

            var branchB = result.Children.Single(c => c.Name == "BranchB");
            var branchBStack = branchB.GetLogicalStack();
            branchBStack.ShouldNotContain(f => f.MemberName == "RootFrame",
                "The stack for Branch B was polluted by the root's stack frame.");
            branchBStack.ShouldContain(f => f.MemberName == "FrameB");
        }
        
        [Test]
        public void Parses_Tags_From_Nested_Contexts_Correctly() {
            var stepEx = new InvalidOperationException("Step Failure");
            var stepCtx = new AmbientFaultContext { BoundaryName = "MyStep", Tags = ["Step"] };
            var fhStep = new FaultHubException("Step failed", stepEx, stepCtx);

            var txCtx = new AmbientFaultContext { BoundaryName = "MyTransaction", Tags = ["Transaction", "RunToEnd"], InnerContext = fhStep.Context };
            var fhTx = new FaultHubException("Transaction failed", fhStep, txCtx);

            var result = fhTx.NewOperationTree();

            result.ShouldNotBeNull();
            result.Name.ShouldBe("MyTransaction");
            result.Tags.ShouldBe(["Transaction", "RunToEnd"]);

            var stepNode = result.Children.ShouldHaveSingleItem();
            stepNode.Name.ShouldBe("MyStep");
            stepNode.Tags.ShouldBe(["Step"]);
        }
        
        [Test]
        public void NewOperationTree_Correctly_Collapses_Nodes_With_Prefixed_Names() {
            var innerEx = new InvalidOperationException("Root Cause");
            var innerCtx = new AmbientFaultContext { BoundaryName = "ExtractAndProcessLinks" };
            var fhInner = new FaultHubException("Inner fail", innerEx, innerCtx);
            var outerCtx = new AmbientFaultContext { BoundaryName = "scraperService.ExtractAndProcessLinks", InnerContext = fhInner.Context };
            var fhOuter = new FaultHubException("Outer fail", fhInner, outerCtx);

            var result = fhOuter.NewOperationTree();

            result.ShouldNotBeNull();
            result.Name.ShouldBe("scraperService.ExtractAndProcessLinks");
            result.Children.ShouldBeEmpty("The child node 'ExtractAndProcessLinks' should have been collapsed into its parent 'scraperService.ExtractAndProcessLinks'.");
            result.GetRootCause().ShouldBe(innerEx);
        }
        
        [Test]
        public void NewOperationTree_Collapses_Inferred_Prefixed_Step_Name() {
            var innerEx = new InvalidOperationException("Inner Failure");
            var innerCtx = new AmbientFaultContext { BoundaryName = "InnerOperation" };
            var fhInner = new FaultHubException("Inner fail", innerEx, innerCtx);
            var outerCtx = new AmbientFaultContext { BoundaryName = "service.InnerOperation", InnerContext = fhInner.Context };
            var fhOuter = new FaultHubException("Step fail", fhInner, outerCtx);

            var result = fhOuter.NewOperationTree();

            result.ShouldNotBeNull();
            result.Name.ShouldBe("service.InnerOperation");
            result.Children.ShouldBeEmpty("The child node 'InnerOperation' should have been collapsed into its parent 'service.InnerOperation'.");
            result.GetRootCause().ShouldBe(innerEx);
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit[]> InnerFailingTransaction_For_Tagging()
            => Observable.Throw<Unit>(new InvalidOperationException("Inner Failure"))
                .BeginWorkflow("InnerTx")
                .RunToEnd();
        [Test]
        public async Task Nested_Transaction_As_Step_Is_Tagged_As_Both_Step_And_Transaction() {
            var transaction = Observable.Return(Unit.Default)
                .BeginWorkflow("OuterTx")
                .Then(_ => InnerFailingTransaction_For_Tagging())
                .RunToEnd();
            await transaction.PublishFaults().Capture();
            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();

            var tree = finalFault.NewOperationTree();

            var nestedNode = tree.Children.ShouldHaveSingleItem();
            nestedNode.Name.ShouldBe(nameof(InnerFailingTransaction_For_Tagging));
            nestedNode.Tags.ShouldContain(Transaction.StepNodeTag, "The node should be tagged as a Step because it was used in a .Then() clause.");
            nestedNode.Tags.ShouldContain(Transaction.TransactionNodeTag, "The node should be tagged as a Transaction because it was created with BeginWorkflow().");
        }

    }
}