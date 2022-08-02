using System.IO;

namespace Xpand.Extensions.FileExtensions {
    public static partial class FileExtensions {
        public static DirectoryInfo ParentExists(this DirectoryInfo directoryInfo) {
            do {
                directoryInfo = directoryInfo?.Parent;
            } while (!Directory.Exists(directoryInfo?.Parent?.FullName));

            return directoryInfo;
        }
    }
}