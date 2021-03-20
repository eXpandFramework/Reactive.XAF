namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static string StringFormat(this object s, string format)
            => string.IsNullOrWhiteSpace(format) ? $"{s}" : string.Format(format, s);
    }
}