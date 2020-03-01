using System.IO;
using Xpand.Extensions.Stream;

namespace Xpand.Extensions.String{
    public static partial class StringExtensions{
        public static byte[] Zip(this string s){
            using (var memoryStream = new MemoryStream(s.Bytes())){
                return memoryStream.Zip();
            }
        }
    }
}