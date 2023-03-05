using System;

namespace Xpand.Extensions.ObjectExtensions {
    public static partial class ObjectExtensions {
        public static bool IsValueNull(this object value)
            => value == null || value == DBNull.Value;
    }
}