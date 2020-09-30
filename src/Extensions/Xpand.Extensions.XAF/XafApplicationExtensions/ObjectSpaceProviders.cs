using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Fasterflect;

namespace Xpand.Extensions.XAF.XafApplicationExtensions{
    public static partial class XafApplicationExtensions{
        public static IEnumerable<IObjectSpaceProvider> ObjectSpaceProviders(this XafApplication application, params Type[] objectTypes) 
            => objectTypes.Select(type => application.CallMethod("GetObjectSpaceProviders", type)).Distinct().Cast<IObjectSpaceProvider>();
    }
}