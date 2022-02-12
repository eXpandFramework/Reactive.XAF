using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Xpand.Extensions.StreamExtensions{
    public static partial class StreamExtensions{
        public static string UnGzip(this Stream compressed){
            using var decompressed = new MemoryStream();
            using (var zip = new GZipStream(compressed, CompressionMode.Decompress, true)){
                zip.CopyTo(decompressed);
            }
            decompressed.Seek(0, SeekOrigin.Begin);
            return decompressed.ReadToEndAsString();
        }
        public static async Task<string> UnGzipAsync(this Stream compressed){
            using var decompressed = new MemoryStream();
            using (var zip = new GZipStream(compressed, CompressionMode.Decompress, true)){
                await zip.CopyToAsync(decompressed);
            }
            decompressed.Seek(0, SeekOrigin.Begin);
            return await decompressed.ReadToEndAsStringAsync();
        }
    }
}