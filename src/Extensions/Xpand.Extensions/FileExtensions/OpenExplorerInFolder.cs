using System.Diagnostics;
using System.IO;

namespace Xpand.Extensions.FileExtensions {
    public static partial class FileExtensions {
        public static Process SelectInExplorer(this FileInfo file) 
            => Process.Start("explorer.exe", "/select, \"" + file.FullName + "\"");
    }
}