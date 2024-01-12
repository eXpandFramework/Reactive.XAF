using System.IO;
using System.Linq;

namespace Xpand.Extensions.FileExtensions {
    public static partial class FileExtensions {
        public static string FindFolderInPathUpwards(this DirectoryInfo current, string folderName) {
            var directory = current;
            while (directory.Parent != null) {
                if (directory.GetDirectories(folderName).Any()) {
                    return Path.GetRelativePath(current.FullName, Path.Combine(directory.FullName, folderName));
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException(
                $"Folder '{folderName}' not found up the tree from '{current.FullName}'");
        }
    }
}