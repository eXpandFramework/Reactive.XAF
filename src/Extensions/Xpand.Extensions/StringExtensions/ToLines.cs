using System.Collections.Generic;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static IEnumerable<string> ToLines(this string input) {
            using System.IO.StringReader reader = new System.IO.StringReader($"{input}");
            while (reader.ReadLine() is { } line) {
                yield return line;
            }
        }
    }
}