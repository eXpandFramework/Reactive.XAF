using System.Linq;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static bool IsNumeric(this string strString) 
            =>strString != null && strString.All(char.IsDigit );
        public static bool IsDigit(this char c) 
            => char.IsDigit(c);
    }
}