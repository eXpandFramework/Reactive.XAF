using System;
using Newtonsoft.Json.Linq;

namespace Xpand.Extensions.ObjectExtensions {
    public static partial class ObjectExtensions {
        public static bool IsValueNull(this object value)
            => value == null || value == DBNull.Value||value is JValue jValue&&jValue.Value.IsValueNull();
    }
}