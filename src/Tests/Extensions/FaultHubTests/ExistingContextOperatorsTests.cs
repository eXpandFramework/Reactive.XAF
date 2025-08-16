
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;

namespace Xpand.Extensions.Tests.FaultHubTests {
    [TestFixture]
    public class ExistingContextOperatorsTests : FaultHubTestBase {

        [Test]
        public void PushStackFrame_Correctly_Appends_To_Existing_Logical_Stack() {
            
            IReadOnlyList<LogicalStackFrame> stackAtErrorTime = null;
            var initialFrame = new LogicalStackFrame("OuterScope", "test.cs", 1);

            
            FaultHub.LogicalStackContext.Value = [initialFrame];

            var stream = Observable.Throw<Unit>(new Exception("test"))
                .PushStackFrame("InnerScope"); 

            stream.Catch((Exception _) => {
                stackAtErrorTime = FaultHub.LogicalStackContext.Value;
                return Observable.Empty<Unit>();
            }).Subscribe();

            
            FaultHub.LogicalStackContext.Value = null;

            
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