using System.IO;

namespace Xpand.Extensions.BytesExtensions{
    public static partial class BytesExtensions{
        public static void Save(this byte[] bytes, string path) 
            => File.WriteAllBytes(path, bytes);
    }
}