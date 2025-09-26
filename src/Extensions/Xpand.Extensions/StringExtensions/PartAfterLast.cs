using System;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static string PartAfterLast(this string source, string value,StringComparison stringComparison = StringComparison.Ordinal) {
            var lastIndexOf = source?.LastIndexOf(value, stringComparison)??-1;
            return lastIndexOf > -1 ? source?.Substring(lastIndexOf + value.Length) : source;
        }

        public static string PartBefore(this string source, string value, StringComparison stringComparison = StringComparison.Ordinal) {
            var lastIndexOf = source.LastIndexOf(value, stringComparison);
            return lastIndexOf > -1 ? source.Substring(0, lastIndexOf) : source;
        }
    }
}