using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static long Val(this string value) {
            string returnVal = String.Empty;
            MatchCollection collection = Regex.Matches(value, "\\d+");
            returnVal = collection.Cast<Match>().Aggregate(returnVal, (current, match) => current + match.ToString());
            return Convert.ToInt64(returnVal);
        }

        public static int ValInt32(this string x) {
            var regex = new Regex("[-+]?\\b\\d+\\b", RegexOptions.Compiled);
            var match = regex.Match(x + "");
            return match.Success ? int.Parse(match.Value, NumberFormatInfo.InvariantInfo) : 0;
        }

        public static double ValDouble(this string x) {
            var regex = new Regex("[-+]?\\b(?:[0-9]*\\.)?[0-9]+\\b", RegexOptions.Compiled);
            var match = regex.Match(x + "");
            return match.Success ? double.Parse(match.Value, NumberFormatInfo.InvariantInfo) : 0;
        }
    }
}