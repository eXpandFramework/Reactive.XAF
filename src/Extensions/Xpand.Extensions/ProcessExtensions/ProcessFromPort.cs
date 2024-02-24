using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Xpand.Extensions.ProcessExtensions {
    public static partial class ProcessExtensions {
        public static Process ProcessFromPort(this int port) {
            var psi = new ProcessStartInfo {
                FileName = "cmd.exe",
                Arguments = "/c netstat -aon | findstr LISTENING | findstr :" + port,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            using StreamReader reader = process!.StandardOutput;
            var line = reader.ReadToEnd().Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();

            return line != null
                ? Process.GetProcessById(
                    int.Parse(line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[^1]))
                : null;
        }
    }
}