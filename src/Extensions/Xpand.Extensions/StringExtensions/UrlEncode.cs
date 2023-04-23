using System.Web;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static string UrlEncode(this string s) 
            => HttpUtility.UrlEncode(s);
    }
}