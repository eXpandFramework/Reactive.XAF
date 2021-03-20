namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static string XmlDecode(this string value) 
            => value.Replace("&amp;", "&").Replace("&apos;", "'").Replace("&quot;", "\"").Replace("&lt;", "<").Replace("&gt;", ">");
    }
}