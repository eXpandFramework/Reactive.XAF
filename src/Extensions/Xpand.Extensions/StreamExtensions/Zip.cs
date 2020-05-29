using System.IO;
using System.IO.Compression;

namespace Xpand.Extensions.StreamExtensions{
    public static partial class StremExtensions{
        public static byte[] Zip(this Stream stream){
            using (var mso = new MemoryStream()){
                using (var gs = new GZipStream(mso, CompressionMode.Compress)){
                    stream.CopyTo( gs);
                }
                return mso.ToArray();
            }
        }
    }
}