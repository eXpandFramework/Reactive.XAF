using System;
using System.Text.RegularExpressions;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
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