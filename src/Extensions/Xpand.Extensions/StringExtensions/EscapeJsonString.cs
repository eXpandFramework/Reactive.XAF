namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static string EscapeJsonString(this string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}