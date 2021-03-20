namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static string FirstCharacterToUpper(this string str) =>
            string.IsNullOrEmpty(str) || char.IsUpper(str, 0) ? str : char.ToUpperInvariant(str[0]) + str.Substring(1);
    }
}