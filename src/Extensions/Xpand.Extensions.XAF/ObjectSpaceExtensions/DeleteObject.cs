using System;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static void DeleteObject(this IObjectSpace objectSpace, object obj)
            => objectSpace.Delete(objectSpace.GetObject(obj));
    }
}