using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static string Enclose(this string input, string start, string end = null) {
            if (input == null)
                return null;
            end ??= start;
            return input.StartsWith(start) && input.EndsWith(end) ? input : new[] { start, input, end }.Join();
        }
    }
}