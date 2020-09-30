using System.IO;

namespace Xpand.Extensions.StreamExtensions{
    public static partial class StremExtensions{
        public static string ReadToEnd(this Stream stream){
            using var streamReader = new StreamReader(stream);
            return streamReader.ReadToEnd();
        }
    }
}