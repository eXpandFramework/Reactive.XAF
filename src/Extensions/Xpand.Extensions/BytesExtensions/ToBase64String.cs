using System;

namespace Xpand.Extensions.BytesExtensions {
    public static partial class BytesExtensions {
        public static string ToBase64String(this byte[] bytes) => Convert.ToBase64String(bytes);
    }
}