using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static string RemoveDiacritics(this string s) 
            => s.Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .JoinString();

        public static string Remove(this string s,string stringToRemove) => s.Replace(stringToRemove, null);
        public static string RemoveQuotes(this string s) => s.Replace("\"", null);

        public static string RemoveSymbols(this string s) => Regex.Replace(s, @"[\uD800-\uDBFF][\uDC00-\uDFFF]", "");

        public static string RemoveComments(this string s) {
            var blockComments = @"/\*(.*?)\*/";
            var lineComments = @"//(.*?)\r?\n";
            var strings = @"""((\\[^\n]|[^""\n])*)""";
            var verbatimStrings = @"@(""[^""]*"")+";
            return Regex.Replace(s, blockComments + "|" + lineComments + "|" + strings + "|" + verbatimStrings,
                me => me.Value.StartsWith("/*") || me.Value.StartsWith("//")
                    ? me.Value.StartsWith("//") ? Environment.NewLine : "" : me.Value, RegexOptions.Singleline);
        }
    }
}