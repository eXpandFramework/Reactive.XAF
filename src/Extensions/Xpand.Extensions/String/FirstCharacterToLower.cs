namespace Xpand.Extensions.String{
    public static partial class StringExtensions{
        public static string FirstCharacterToLower(this string str){
            return string.IsNullOrEmpty(str) || char.IsLower(str, 0)
                ? str
                : char.ToLowerInvariant(str[0]) + str.Substring(1);
        }
    }
}