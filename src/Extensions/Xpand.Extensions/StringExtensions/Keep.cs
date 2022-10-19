namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static string Keep(this string s, int characters) 
            =>characters<s.Length? s.Remove(0, characters):s;
    }
}