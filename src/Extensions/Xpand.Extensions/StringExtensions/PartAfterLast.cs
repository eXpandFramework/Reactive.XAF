using System;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static string PartAfterLast(this string source, string value,StringComparison stringComparison = StringComparison.Ordinal) 
            => source?.Substring(source.LastIndexOf(value, stringComparison)+value.Length);
        
        public static string PartBefore(this string source, string value, StringComparison stringComparison = StringComparison.Ordinal)
            => source?.Substring(0, source.LastIndexOf(value, stringComparison));

    }
}