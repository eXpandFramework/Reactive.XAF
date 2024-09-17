using System;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static bool Contains(this string source, string toCheck, StringComparison comp) 
            => source?.IndexOf(toCheck, comp) >= 0;
    }
}