using System.Net;
using System.Security;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static SecureString ToSecureString(this string s)
            => new NetworkCredential("", s).SecurePassword;
        
        public static string GetString(this SecureString secureString)
            => new NetworkCredential("",secureString).Password;
        
    }
}