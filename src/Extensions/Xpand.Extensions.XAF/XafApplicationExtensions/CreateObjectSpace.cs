using System;
using System.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using Xpand.Extensions.XAF.TypesInfoExtensions;

namespace Xpand.Extensions.XAF.XafApplicationExtensions {
    public static partial class XafApplicationExtensions {
        public static IObjectSpace CreateObjectSpace(this XafApplication application, Type objectType, bool nonSecuredObjectSpace)
            => application.ObjectSpaceProviders(objectType)
                .Select(provider => nonSecuredObjectSpace ? (provider is INonsecuredObjectSpaceProvider nonsecuredObjectSpaceProvider
                        ? nonsecuredObjectSpaceProvider.CreateNonsecuredObjectSpace() : provider.CreateUpdatingObjectSpace(false))
                    : provider.CreateObjectSpace())
                .First();

        public static IObjectSpace CreateNonSecuredObjectSpace(this XafApplication application,Type objectType)
            => application.CreateObjectSpace(true ,objectType,true);
        
        public static IObjectSpace CreateObjectSpace(this XafApplication application, bool useObjectSpaceProvider,Type type=null,bool nonSecuredObjectSpace=false,
            [CallerMemberName] string caller = "") {
            if (type != null) {
                if (type.IsArray) {
                    type = type.GetElementType();
                }
                if (!type.ToTypeInfo().IsPersistent) {
                    throw new InvalidOperationException($"{caller} {type?.FullName} is not a persistent object");
                }
            }
            
            if (!useObjectSpaceProvider)
                return application.CreateObjectSpace(type ?? typeof(object));
            var applicationObjectSpaceProvider = application.ObjectSpaceProviders(type ?? typeof(object)).First();
            if (!nonSecuredObjectSpace)
                return applicationObjectSpaceProvider.CreateObjectSpace();
            else if (applicationObjectSpaceProvider is INonsecuredObjectSpaceProvider nonsecuredObjectSpaceProvider)
                return nonsecuredObjectSpaceProvider.CreateNonsecuredObjectSpace();
            else
                return applicationObjectSpaceProvider.CreateUpdatingObjectSpace(false);
        }
    }
    
}