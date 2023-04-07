using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static string EnsureEndWith(this string s, string end)
            => !s.EndsWith(end) ? new[] { s, end }.JoinConcat() : s;
    }
}