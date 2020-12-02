using System.IO;
using System.IO.Compression;

namespace Xpand.Extensions.StreamExtensions{
    public static partial class StreamExtensions{
        public static byte[] GZip(this Stream decompressed,CompressionLevel compressionLevel=CompressionLevel.Optimal){
            using var compressed = new MemoryStream();
            using (var zip = new GZipStream(compressed, compressionLevel, true)){
                decompressed.CopyTo(zip);
            }
            compressed.Seek(0, SeekOrigin.Begin);
            return compressed.ToArray();
        }
    }
}