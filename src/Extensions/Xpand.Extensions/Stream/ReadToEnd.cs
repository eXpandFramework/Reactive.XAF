using System.IO;

namespace Xpand.Extensions.Stream{
    public static partial class StremExtensions{
        public static string ReadToEnd(this System.IO.Stream stream){
            using (var streamReader = new StreamReader(stream)){
                return streamReader.ReadToEnd();
            }
        }
    }
}