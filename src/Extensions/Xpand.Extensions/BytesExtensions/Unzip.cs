using System.IO;
using System.Threading.Tasks;
using Xpand.Extensions.StreamExtensions;

namespace Xpand.Extensions.BytesExtensions{
    public static partial class BytesExtensions{
	    public static string Unzip(this byte[] bytes){
            using var mso = new MemoryStream(bytes);
            return mso.UnGzip();
        }
        public static Task<string> UnzipAsync(this byte[] bytes){
            using var mso = new MemoryStream(bytes);
            return mso.UnGzipAsync();
        }
    }
}