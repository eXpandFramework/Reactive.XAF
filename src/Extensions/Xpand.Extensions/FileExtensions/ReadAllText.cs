using System.IO;

namespace Xpand.Extensions.FileExtensions {
    public static partial class FileExtensions {
        public static string ReadAllText(this FileInfo info) 
            => File.ReadAllText(info.FullName);
        public static byte[] ReadAllBytes(this FileInfo info) 
            => File.ReadAllBytes(info.FullName);
    }
}