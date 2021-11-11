using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;

namespace Xpand.Extensions.XAF.XafApplicationExtensions {
    public static partial class XafApplicationExtensions {
        public static IObjectSpace CreateObjectSpace(this XafApplication application, Type objectType, bool nonSecuredObjectSpace)
            => application.ObjectSpaceProviders(objectType)
                .Select(provider => nonSecuredObjectSpace ? (provider is INonsecuredObjectSpaceProvider nonsecuredObjectSpaceProvider
                        ? nonsecuredObjectSpaceProvider.CreateNonsecuredObjectSpace() : provider.CreateUpdatingObjectSpace(false))
                    : provider.CreateObjectSpace())
                .First();

        public static IObjectSpace CreateNonSecuredObjectSpace(this XafApplication application)
            => application.CreateObjectSpace(true, true);
        public static IObjectSpace CreateObjectSpace(this XafApplication application, bool useObjectSpaceProvider,bool nonSecuredObjectSpace=false)
            => useObjectSpaceProvider ? !nonSecuredObjectSpace ? application.ObjectSpaceProvider.CreateObjectSpace()
                    : application.ObjectSpaceProvider is INonsecuredObjectSpaceProvider nonsecuredObjectSpaceProvider
                        ? nonsecuredObjectSpaceProvider.CreateNonsecuredObjectSpace() : application.ObjectSpaceProvider.CreateUpdatingObjectSpace(false)
                : application.CreateObjectSpace();
    }
}