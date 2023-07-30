using System;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static object FindObject(this IObjectSpace objectSpace, Type objectType)
            => objectSpace.FindObject(objectType, null);
    }
}