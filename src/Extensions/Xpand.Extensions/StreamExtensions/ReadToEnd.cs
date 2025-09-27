using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Xpand.Extensions.StreamExtensions{
    public static partial class StreamExtensions{
        public static string ReadToEnd(this Stream stream){
            using var streamReader = new StreamReader(stream);
            return streamReader.ReadToEnd();
        }
        public static string ReadToEndAsString(this Stream stream,Encoding encoding=null){
            using var streamReader = new StreamReader(stream);
            return streamReader.ReadToEnd();
        }

        public static async Task<string> ReadToEndAsStringAsync(this Stream stream){
            using var streamReader = new StreamReader(stream);
            return await streamReader.ReadToEndAsync();
        }
    }
}