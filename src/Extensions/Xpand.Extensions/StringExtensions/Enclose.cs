using System.Linq;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static string Enclose(this string input, string start, string end = null) {
            if (input == null)
                return null;
            end ??= start;
            return input.StartsWith(start) && input.EndsWith(end) ? input : new[] { start, input, end }.Join();
        }

        public static string EncloseHTMLNonImportant(this string input,bool useItalic=false)
            => input.EncloseHTMLTag(!useItalic?"<span class=\"tg-spoiler\">":"i");
        public static string EncloseHTMLVeryImportant(this string input)
            => input.EncloseHTMLTag("b","u");
        public static string EncloseHTMLImportant(this string input)
            => input.EncloseHTMLTag("b");
        
        public static string EncloseHTMLTag(this string input, params string[] tags)
            => tags.Reverse().Aggregate(input, (current, tag) => {
                var tagParts = tag.Split(' ', 2);
                var tagName = tagParts[0];
                var attributes = tagParts.Length > 1 ? " " + tagParts[1] : "";
                return $"<{tagName}{attributes}>{current}</{tagName}>";
            });


        public static string EncloseParenthesis(this string input) => input.Enclose("(", ")");
    }
}