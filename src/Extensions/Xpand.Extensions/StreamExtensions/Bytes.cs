using System.IO;

namespace Xpand.Extensions.StreamExtensions{
    public static partial class StreamExtensions{
        public static byte[] Bytes(this Stream stream){
            if (stream is MemoryStream memoryStream){
                return memoryStream.ToArray();
            }

            using MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
    }
}