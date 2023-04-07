namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static string EnsureEmpty(this string source)
            => source ?? string.Empty;
    }
}