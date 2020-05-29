using System;
using System.CodeDom.Compiler;
using System.Text.RegularExpressions;

namespace Xpand.Extensions.StringExtensions{
    public static partial class StringExtensions{
        public static string CleanCodeName(this string s) {
            var regex = new Regex(@"[^\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nd}\p{Nl}\p{Mn}\p{Mc}\p{Cf}\p{Pc}\p{Lm}]");
            string ret = regex.Replace(s + "", "");
            if (!(string.IsNullOrEmpty(ret)) && !Char.IsLetter(ret, 0) && !CodeDomProvider.CreateProvider("C#").IsValidIdentifier(ret))
                ret = string.Concat("_", ret);
            return ret;

        }

    }
}