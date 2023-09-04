using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static string ToValidFileName(this string input) {
            var invalidChars = Path.GetInvalidFileNameChars();
            var validString = new string(input.Where(ch => !invalidChars.Contains(ch)).ToArray()).Replace(" ", "_");
            return Regex.Replace(validString.Length > 250 ? validString.Substring(0, 250) : validString, "[^a-zA-Z0-9_]", "");
        }
    }
}