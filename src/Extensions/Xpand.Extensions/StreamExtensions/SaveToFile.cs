using System;
using System.IO;

namespace Xpand.Extensions.StreamExtensions {
    public static partial class StreamExtensions {
        public static void SaveToFile(this Stream stream, string filePath) {
            var directory = Path.GetDirectoryName(filePath) + "";
            if (!Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }

            using var fileStream = File.OpenWrite(filePath);
            stream.CopyTo(fileStream);
        }
    }
}