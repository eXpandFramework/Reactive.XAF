using System.IO;
using System.IO.Compression;
using Xpand.Extensions.StreamExtensions;

namespace Xpand.Extensions.StringExtensions{
    public static partial class StringExtensions{
        public static byte[] GZip(this string s,CompressionLevel compressionLevel=CompressionLevel.Optimal){
            using var memoryStream = new MemoryStream(s.Bytes());
            return memoryStream.GZip(compressionLevel);
        }
    }
}