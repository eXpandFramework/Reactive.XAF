namespace Xpand.Extensions.String{
    public static partial class StringExtensions{
        public static string SplitCamelCase(string input){
            return System.Text.RegularExpressions.Regex
                .Replace(input, "([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.Compiled).Trim();
        }
    }
}