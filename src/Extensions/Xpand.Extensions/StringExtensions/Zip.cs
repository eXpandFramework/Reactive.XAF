using System.IO;
using Xpand.Extensions.StreamExtensions;

namespace Xpand.Extensions.StringExtensions{
    public static partial class StringExtensions{
        public static byte[] Zip(this string s){
            using (var memoryStream = new MemoryStream(s.Bytes())){
                return memoryStream.Zip();
            }
        }
    }
}