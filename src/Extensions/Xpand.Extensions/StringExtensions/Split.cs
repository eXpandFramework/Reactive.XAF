using System;

namespace Xpand.Extensions.StringExtensions{
    public static partial class StringExtensions{
        public static string[] Split(this string str, char separator) 
            => str.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
        
        public static string SplitCamelCase(this string input) 
            => System.Text.RegularExpressions.Regex.Replace(input, "([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.Compiled).Trim();
    }
}