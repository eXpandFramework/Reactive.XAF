using System.IO;
using Xpand.Extensions.Stream;

namespace Xpand.Extensions.Bytes{
    public static class BytesExtensions{
        public static string Unzip(this byte[] bytes){
            using (var mso = new MemoryStream(bytes)){
                return mso.Unzip();
            }
        }
    }
}