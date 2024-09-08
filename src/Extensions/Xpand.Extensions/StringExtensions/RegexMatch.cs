using System.Text.RegularExpressions;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static bool RegexMatch(this string strString,string pattern)
            =>strString.IsNotNullOrEmpty()&&pattern.IsNotNullOrEmpty() && Regex.IsMatch(strString, pattern);
    }
}