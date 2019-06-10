using System;
using System.CodeDom.Compiler;
using System.Text.RegularExpressions;

namespace Xpand.Source.Extensions.System.String{
    internal static partial class StringExtensions{
        public static string CleanCodeName(this string s) {
            var regex = new Regex(@"[^\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nd}\p{Nl}\p{Mn}\p{Mc}\p{Cf}\p{Pc}\p{Lm}]");
            string ret = regex.Replace(s + "", "");
            if (!(global::System.String.IsNullOrEmpty(ret)) && !Char.IsLetter(ret, 0) && !CodeDomProvider.CreateProvider("C#").IsValidIdentifier(ret))
                ret = global::System.String.Concat("_", ret);
            return ret;

        }

    }
}