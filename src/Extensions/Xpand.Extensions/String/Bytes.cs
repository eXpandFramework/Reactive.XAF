using System.IO;
using System.Linq;
using System.Text;
using Xpand.Extensions.Stream;

namespace Xpand.Extensions.String{
    public static partial class StringExtensions{
        public static byte[] Bytes(this string s){
            return Encoding.UTF8.GetBytes(s);
        }
    }
}