using System;
using DevExpress.ExpressApp;
using Fasterflect;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static IObjectSpace AdditionalObjectSpace(this IObjectSpace objectSpace, Type type)
            => (IObjectSpace)objectSpace.CallMethod("GetCertainObjectSpace", type);
    }
}