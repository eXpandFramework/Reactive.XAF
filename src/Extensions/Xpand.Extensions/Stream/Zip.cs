using System.IO;
using System.IO.Compression;

namespace Xpand.Extensions.Stream{
    public static partial class StremExtensions{
        public static byte[] Zip(this System.IO.Stream stream){
            using (var mso = new MemoryStream()){
                using (var gs = new GZipStream(mso, CompressionMode.Compress)){
                    CopyTo(stream, gs);
                }

                return mso.ToArray();
            }
        }
    }
}