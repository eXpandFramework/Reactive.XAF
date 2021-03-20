using System.Text;

namespace Xpand.Extensions.StringExtensions{
    public static partial class StringExtensions{
        public static byte[] Bytes(this string s) 
            => Encoding.UTF8.GetBytes(s);

    }
}