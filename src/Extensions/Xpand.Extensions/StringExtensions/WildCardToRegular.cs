using System;
using System.Text.RegularExpressions;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static String WildCardToRegular(this String value) 
            => "^" + Regex.Escape(value).Replace("\\*", ".*") + "$";
    }
}