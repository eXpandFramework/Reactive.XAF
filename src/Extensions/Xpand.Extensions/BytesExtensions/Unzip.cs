using System.IO;
using Xpand.Extensions.StreamExtensions;

namespace Xpand.Extensions.BytesExtensions{
    public static partial class BytesExtensions{
	    public static string Unzip(this byte[] bytes){
            using (var mso = new MemoryStream(bytes)){
                return mso.Unzip();
            }
        }
    }
}