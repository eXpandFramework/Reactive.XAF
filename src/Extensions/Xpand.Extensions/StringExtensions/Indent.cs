using System;
using System.Linq;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static string Indent(this string text, int indentCount, char indentChar = ' ') {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var indentString = new string(indentChar, indentCount);
            return string.Join(Environment.NewLine, text.Split(["\r\n", "\r", "\n"], StringSplitOptions.None)
                    .Select(line => indentString + line));
        }
    }
}