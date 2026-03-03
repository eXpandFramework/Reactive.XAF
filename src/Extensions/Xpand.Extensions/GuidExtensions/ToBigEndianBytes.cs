using System;

namespace Xpand.Extensions.GuidExtensions {
    public static partial class GuidExtensions {
        public static byte[] ToBigEndianBytes(this Guid guid) {
            var bytes = guid.ToByteArray();
            Array.Reverse(bytes, 0, 4);
            Array.Reverse(bytes, 4, 2);
            Array.Reverse(bytes, 6, 2);
            return bytes;
        }
    }
}