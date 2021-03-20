using System.IO;
using System.Linq;

namespace Xpand.Extensions.FileExtensions {
    public static partial class FileExtensions {
        public static string GetParentFolder(this DirectoryInfo directoryInfo, string folderName) {
            folderName = folderName.ToLower();
            while (directoryInfo != null &&
                   directoryInfo.GetDirectories().All(info => info.Name.ToLower() != folderName)) {
                directoryInfo = directoryInfo.Parent;
            }

            return directoryInfo?.GetDirectories().First(info => info.Name.ToLower() == folderName).FullName;
        }
    }
}