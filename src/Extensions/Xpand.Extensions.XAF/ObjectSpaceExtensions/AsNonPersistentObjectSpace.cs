using System;
using DevExpress.ExpressApp;
using Fasterflect;
using Xpand.Extensions.ObjectExtensions;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    
    public static partial class ObjectSpaceExtensions {
        public static NonPersistentObjectSpace AsNonPersistentObjectSpace(this IObjectSpace objectSpace)
            => objectSpace as NonPersistentObjectSpace;
        public static NonPersistentObjectSpace ToNonPersistentObjectSpace(this IObjectSpace objectSpace)
            => objectSpace.To<NonPersistentObjectSpace>();
    }
}