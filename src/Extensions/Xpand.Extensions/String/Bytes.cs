using System.Text;

namespace Xpand.Extensions.String{
    public static partial class StringExtensions{
        public static byte[] Bytes(this string s){
            return Encoding.UTF8.GetBytes(s);
        }
    }
}