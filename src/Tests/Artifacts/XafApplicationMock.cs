using System;
using System.Reactive.Linq;
using Moq;
using Ryder;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Agnostic.Tests.Artifacts{
    sealed class XafApplicationMock:Mock<MockedXafApplication> {

        internal XafApplicationMock(Platform platform=Platform.Agnostic) {
            CallBase = true;
            var original = typeof(XafApplicationExtensions).GetMethod(nameof(XafApplicationExtensions.GetPlatform));
            Redirection.Observe(original)
                .Select(context => {
                    context.ReturnValue=platform;
                    return platform;
                })
                .TakeUntil(Object.WhenDisposed())
                .Subscribe();
        }
    }
}