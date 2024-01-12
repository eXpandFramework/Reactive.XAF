using System;
using System.IO;
using System.Linq;

namespace Xpand.Extensions.FileExtensions {
    public static partial class FileExtensions {
        public static FileInfo EnsurePath(this FileInfo fileInfo,bool randomizeAlways=false)
            => !fileInfo.IsFileLocked()&&!randomizeAlways ? fileInfo
                : new FileInfo($"{fileInfo.DirectoryName}/{fileInfo.Name}_{Guid.NewGuid():N}{fileInfo.Extension}");
    }
}