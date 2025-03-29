using System.Linq;
using System.Text.RegularExpressions;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static Match RegexMatch(this string strString,string pattern,RegexOptions regexOptions=RegexOptions.None)
            =>Regex.Match(strString, pattern,regexOptions);
        public static Match[] RegexMatches(this string strString,string pattern,RegexOptions regexOptions=RegexOptions.None)
            =>Regex.Matches(strString, pattern,regexOptions).ToArray();
        
        public static bool RegexIsMatch(this string strString,string pattern,RegexOptions regexOptions=RegexOptions.None)
            =>strString.IsNotNullOrEmpty()&&pattern.IsNotNullOrEmpty() && Regex.IsMatch(strString, pattern,regexOptions);
    }
}