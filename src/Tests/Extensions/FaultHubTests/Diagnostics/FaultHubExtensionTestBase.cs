using System;
using System.Linq;
using Xpand.Extensions.Reactive.FaultHub;

namespace Xpand.Extensions.Tests.FaultHubTests.Diagnostics{
    public abstract class FaultHubExtensionTestBase:FaultHubTestBase {
        protected FaultHubException CreateNestedFault(params (string Name, object[] Context)[] operations) {
            if (operations == null || !operations.Any()) {
                return new FaultHubException("Test", new Exception(), new AmbientFaultContext());
            }

            Exception currentException = new InvalidOperationException("Innermost failure");
            AmbientFaultContext currentContext = null;

            foreach (var op in operations.Reverse()) {
                var context = new AmbientFaultContext { BoundaryName = op.Name, UserContext = op.Context, InnerContext = currentContext };
                currentException = new FaultHubException($"Failure in {op.Name}", currentException, context);
                currentContext = context;
            }

            return currentException as FaultHubException;
        }

    }
}