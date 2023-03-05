using System.Linq;
using System.Text.RegularExpressions;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static string RemoveComments(this string s) 
            => s.ToLines().Select(line => Regex.Replace(line, "(\".*)(//.*)", "$1")).JoinNewLine();
    }
}