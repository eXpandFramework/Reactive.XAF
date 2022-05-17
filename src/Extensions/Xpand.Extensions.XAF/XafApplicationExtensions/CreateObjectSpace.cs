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
            => application.CreateObjectSpace(true ,typeof(object),true);
        
        public static IObjectSpace CreateObjectSpace(this XafApplication application, bool useObjectSpaceProvider,Type type=null,bool nonSecuredObjectSpace=false) {
            if (!useObjectSpaceProvider)
                return application.CreateObjectSpace(type ?? typeof(object));
            var applicationObjectSpaceProvider = application.ObjectSpaceProviders(type ?? typeof(object)).First();
            return !nonSecuredObjectSpace ? applicationObjectSpaceProvider.CreateObjectSpace()
                : applicationObjectSpaceProvider is INonsecuredObjectSpaceProvider nonsecuredObjectSpaceProvider
                    ? nonsecuredObjectSpaceProvider.CreateNonsecuredObjectSpace() : applicationObjectSpaceProvider.CreateUpdatingObjectSpace(false);
        }
    }
    
}