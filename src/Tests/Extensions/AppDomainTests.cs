using System;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Transform.System;
using Xpand.TestsLib;
using Xpand.TestsLib.Common.Attributes;

namespace Xpand.Extensions.Tests {
    using System;
    using NUnit.Framework;
    using Shouldly;

    public class AppDomainTests:BaseTest {
        
        [Test][XpandTest()]
        public void ExecuteOnce() {
            var testObserver = AppDomain.CurrentDomain.ExecuteOnce().Test();
            
            testObserver.ItemCount.ShouldBe(1);
            
            testObserver = AppDomain.CurrentDomain.ExecuteOnce().Test();
            
            testObserver.ItemCount.ShouldBe(0);
        }

    }
}