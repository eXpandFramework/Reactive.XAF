using System;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;
using Xpand.XAF.Modules.Reactive.Tests.Common;

namespace Xpand.XAF.Modules.Reactive.Tests{
    public class CommitChangesSequentialTests:ReactiveCommonTest {
        [XpandTest]
        [TestCase(nameof(Platform.Win))]
        public void Retries_On_Errors(string platformName){
            var platform = GetPlatform(platformName);
            using var application = DefaultReactiveModule(platform).Application;

            int repeat = 0;
            
            var testObserver = application.CommitChangesSequential(_ => {
                repeat++;
                return new Exception().Throw<R>();
            }).Test();
            
            testObserver.AwaitDone(Timeout).ErrorCount.ShouldBe(1);
            repeat.ShouldBe(3);
        }

    }
}