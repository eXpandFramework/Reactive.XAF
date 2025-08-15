// New File: Xpand.Extensions.Tests/FaultHubTests/ExistingContextOperatorsTests.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using System.Reactive;

namespace Xpand.Extensions.Tests.FaultHubTests {
    [TestFixture]
    public class ExistingContextOperatorsTests : FaultHubTestBase {
// MODIFICATION: The test is replaced with a simpler, more direct version
// that correctly isolates and verifies the behavior of PushStackFrame.
        [Test]
        public void PushStackFrame_Correctly_Appends_To_Existing_Logical_Stack() {
            // ARRANGE
            IReadOnlyList<LogicalStackFrame> stackAtErrorTime = null;
            var initialFrame = new LogicalStackFrame("OuterScope", "test.cs", 1);

            // ACT
            // 1. Manually set an initial context.
            FaultHub.LogicalStackContext.Value = new[] { initialFrame };

            var stream = Observable.Throw<Unit>(new Exception("test"))
                .PushStackFrame("InnerScope"); // 2. The operator under test.

            stream.Catch((Exception _) => {
                // 3. Capture the context at the exact moment of failure.
                stackAtErrorTime = FaultHub.LogicalStackContext.Value;
                return Observable.Empty<Unit>();
            }).Subscribe();

            // 4. Manually clean up the context to not interfere with other tests.
            FaultHub.LogicalStackContext.Value = null;

            // ASSERT
            stackAtErrorTime.ShouldNotBeNull("The logical stack was null at the time of the error.");
            var logicalStack = stackAtErrorTime.ToList();

            var outerFrameIndex = logicalStack.FindIndex(f => f.MemberName == "OuterScope");
            var innerFrameIndex = logicalStack.FindIndex(f => f.MemberName == "InnerScope");

            outerFrameIndex.ShouldNotBe(-1, "The outer frame was lost.");
            innerFrameIndex.ShouldNotBe(-1, "The inner frame was not added.");
            innerFrameIndex.ShouldBeLessThan(outerFrameIndex, "The inner frame should be prepended to the outer frame.");
        }
        
    }
}