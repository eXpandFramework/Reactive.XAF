using System;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static byte[] FromBase64String(this string strString)
            => Convert.FromBase64String(strString);
    }
}