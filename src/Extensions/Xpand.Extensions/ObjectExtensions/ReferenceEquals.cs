using System;

namespace Xpand.Extensions.ObjectExtensions {
    public static partial class ObjectExtensions {
        public new static bool ReferenceEquals(this object objA, object objB)
            => Object.ReferenceEquals(objA, objB);
    }
}