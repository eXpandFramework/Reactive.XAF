using System.Linq;

namespace Xpand.Extensions.StringExtensions{
    public static partial class StringExtensions{
        public static string Repeat(this string s, int nummber,string seperator="") => string.Join(seperator,Enumerable.Repeat(s, nummber));
    }
}