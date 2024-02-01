using System;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static bool IsEqualIgnoreCase(this string s, string other) 
            => StringComparer.InvariantCultureIgnoreCase.Equals(s, other);

        public static bool IsEqual(this string s, string other) 
            => StringComparer.InvariantCulture.Equals(s, other);
    }
}