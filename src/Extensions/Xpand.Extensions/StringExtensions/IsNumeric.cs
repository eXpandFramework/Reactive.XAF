using System.Linq;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static bool IsNumeric(this string strString) 
            =>strString.IsNotNullOrEmpty() && strString.All(char.IsDigit );
        public static bool IsDigit(this char c) 
            => char.IsDigit(c);

        public static bool IsDigitOrDecimalSeparator(this char c) => char.IsDigit(c) || c == '.';
    }
}