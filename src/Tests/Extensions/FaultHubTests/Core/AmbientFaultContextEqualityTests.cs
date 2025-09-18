using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Relay;

namespace Xpand.Extensions.Tests.FaultHubTests.Core {
    [TestFixture]
    public class AmbientFaultContextEqualityTests {
        [Test]
        public void Contexts_With_Identical_Primitive_Properties_Should_Be_Equal() {
            var context1 = new AmbientFaultContext { BoundaryName = "Test", UserContext = ["data"], Tags = ["tag"] };
            var context2 = new AmbientFaultContext { BoundaryName = "Test", UserContext = ["data"], Tags = ["tag"] };

            context1.ShouldBe(context2);
            context1.GetHashCode().ShouldBe(context2.GetHashCode());
        }

        [Test]
        public void Contexts_With_Identical_But_Separately_Created_LogicalStackFrames_Should_Be_Equal() {
            var stack1 = new[] { new LogicalStackFrame("MethodA", "file.cs", 10, "context") };
            var stack2 = new[] { new LogicalStackFrame("MethodA", "file.cs", 10, "context") };

            var context1 = new AmbientFaultContext { BoundaryName = "Test", LogicalStackTrace = stack1 };
            var context2 = new AmbientFaultContext { BoundaryName = "Test", LogicalStackTrace = stack2 };

            context1.ShouldBe(context2);
            context1.GetHashCode().ShouldBe(context2.GetHashCode());
        }

        [Test]
        public void Contexts_With_Identical_Hierarchies_Of_InnerContexts_Should_Be_Equal() {
            var inner1 = new AmbientFaultContext { BoundaryName = "Inner" };
            var outer1 = new AmbientFaultContext { BoundaryName = "Outer", InnerContext = inner1 };

            var inner2 = new AmbientFaultContext { BoundaryName = "Inner" };
            var outer2 = new AmbientFaultContext { BoundaryName = "Outer", InnerContext = inner2 };
            
            inner1.ShouldBe(inner2);
            outer1.ShouldBe(outer2);
            outer1.GetHashCode().ShouldBe(outer2.GetHashCode());
        }

        [Test]
        public void Contexts_Should_Be_Unequal_If_InnerContexts_Differ() {
            var inner1 = new AmbientFaultContext { BoundaryName = "Inner1" };
            var outer1 = new AmbientFaultContext { BoundaryName = "Outer", InnerContext = inner1 };

            var inner2 = new AmbientFaultContext { BoundaryName = "Inner2" };
            var outer2 = new AmbientFaultContext { BoundaryName = "Outer", InnerContext = inner2 };

            outer1.ShouldNotBe(outer2);
        }
    }
}