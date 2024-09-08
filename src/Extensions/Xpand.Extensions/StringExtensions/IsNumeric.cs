namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static bool IsNumeric(this string strString) 
            => strString.RegexMatch( "\\A\\b\\d+\\b\\z");
    }
}