namespace Xpand.Extensions.StringExtensions{
    public static partial class StringExtensions{
        public static string FirstCharacterToLower(this string str) =>
            string.IsNullOrEmpty(str) || char.IsLower(str, 0) ? str : char.ToLowerInvariant(str[0]) + str.Substring(1);
    }
}