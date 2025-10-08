namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static bool IsNumeric(this string strString) 
            =>strString != null && decimal.TryParse(strString, out _);
        public static bool IsDigit(this char c) 
            => char.IsDigit(c);
    }
}