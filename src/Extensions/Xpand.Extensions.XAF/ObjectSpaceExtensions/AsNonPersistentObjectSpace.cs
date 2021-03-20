using System.Collections.Generic;
using DevExpress.ExpressApp;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static NonPersistentObjectSpace AsNonPersistentObjectSpace(this IObjectSpace objectSpace)
            => objectSpace as NonPersistentObjectSpace;
    }
}