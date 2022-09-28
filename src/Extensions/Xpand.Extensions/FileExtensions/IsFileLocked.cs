using System.IO;

namespace Xpand.Extensions.FileExtensions {
    public static partial class FileExtensions {
        public static bool IsFileLocked(this FileInfo file) {
            FileStream stream = null;
            if (!file.Exists) {
                return false;
            }
            try {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException) {
                return true;
            }
            finally {
                stream?.Close();
            }

            return false;
        }
    }
}