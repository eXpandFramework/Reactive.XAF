using System.IO;
using System.IO.Compression;
using System.Text;

namespace Xpand.Extensions.Stream{
    public static partial class StremExtensions{
        public static string Unzip(this System.IO.Stream stream){
            using (var mso = new MemoryStream()){
                using (var gs = new GZipStream(stream, CompressionMode.Decompress)){
                    CopyTo(gs, mso);
                }

                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }
    }
}