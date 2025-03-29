using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.ProcessExtensions {
    public static partial class ProcessExtensions {
        public static IEnumerable<(int Port, string Protocol)> ListeningPorts(this Process process) {
            var lines = Process.Start(new ProcessStartInfo("netstat", "-ano") {
                    RedirectStandardOutput = true,
                    UseShellExecute = false, CreateNoWindow = true
                })!
                .StandardOutput.ReadToEnd().Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            return lines.SkipWhile(l => !l.Trim().StartsWith("Proto")).Skip(1).Where(l => l.Contains("LISTENING"))
                .Select(l => {
                    var parts = Regex.Split(l.Trim(), @"\s+");
                    var proto = parts[0];
                    var localAddress = parts[1];
                    var pid = parts[^1];
                    if (int.Parse(pid) != process.Id) return default;
                    var port = int.Parse(localAddress.Split(':')[^1]);
                    return (port, proto);
                })
                .WhereNotDefault();
        }
    }
}