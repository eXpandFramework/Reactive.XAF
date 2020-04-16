using System.Linq;

namespace Xpand.Extensions.String{
    public static partial class StringExtensions{
        public static string Repeat(this string s, int nummber,string seperator=""){
            return string.Join(seperator,Enumerable.Repeat(s, nummber));
        }
    }
}