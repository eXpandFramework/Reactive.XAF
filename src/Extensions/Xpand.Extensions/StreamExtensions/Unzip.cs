using System.IO;
using System.IO.Compression;
using System.Text;

namespace Xpand.Extensions.StreamExtensions{
    public static partial class StremExtensions{
        public static string Unzip(this Stream stream){
            using (var mso = new MemoryStream()){
                using (var gs = new GZipStream(stream, CompressionMode.Decompress)){
                    gs.CopyTo( mso);
                }
                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }
    }
}