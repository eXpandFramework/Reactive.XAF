using System;
using System.Text.RegularExpressions;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static bool IsNumeric(this string strString) 
            => Regex.IsMatch(strString, "\\A\\b\\d+\\b\\z");
    }
}