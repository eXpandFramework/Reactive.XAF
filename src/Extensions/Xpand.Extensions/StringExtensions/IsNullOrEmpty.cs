namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static bool IsNullOrEmpty(this string strString)
            => string.IsNullOrEmpty(strString);
        public static bool IsNotNullOrEmpty(this string strString)
            => !string.IsNullOrEmpty(strString);
    }
}