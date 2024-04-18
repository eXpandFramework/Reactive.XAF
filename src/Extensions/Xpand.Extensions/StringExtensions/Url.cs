using System.Web;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static string UrlEncode(this string s) 
            => HttpUtility.UrlEncode(s);
        public static string UrlDecode(this string s) 
            => HttpUtility.UrlDecode(s);
    }
}