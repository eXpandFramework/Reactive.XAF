using System.IO;

namespace Xpand.Extensions.FileExtensions {
    public static partial class FileExtensions {
        public static bool IsDriveFull(this DriveInfo driveInfo, double threshold) 
            => ((double)(driveInfo.TotalSize - driveInfo.TotalFreeSpace) / driveInfo.TotalSize) * 100 >= threshold;
    }
}