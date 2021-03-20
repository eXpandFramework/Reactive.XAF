namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static string XmlEncode(this string value)
            => value.TrimEnd((char) 1).Replace("&", "&amp;").Replace("'", "&apos;").Replace("\"", "&quot;")
                .Replace("<", "&lt;").Replace(">", "&gt;");
    }
}