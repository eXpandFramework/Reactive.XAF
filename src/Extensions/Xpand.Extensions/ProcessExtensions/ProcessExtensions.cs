using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Xpand.Extensions.IntPtrExtensions;

namespace Xpand.Extensions.ProcessExtensions {
    public static partial class ProcessExtensions {
        public static void KillTree(this Process root) {
            if (root.HasExited) return;

            var snapshot = Process.GetProcesses().ToDictionary(p => p.Id);
            var children = new ConcurrentBag<Process>();

            void Walk(int pid) {
                foreach (var p in snapshot.Values.Where(p => ParentIdCross(p) == pid)) {
                    children.Add(p);
                    Walk(p.Id);
                }
            }

            Walk(root.Id);

            children.AsParallel().ForAll(p => {
                try {
                    p.Kill();
                }
                catch {
                    // ignored
                }
            });

            root.Kill();
            root.WaitForExit();
        }

        private static int ParentIdCross(Process p) {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return p.Handle.ParentProcess()?.Id ?? 0;

            var stat = File.ReadAllText($"/proc/{p.Id}/stat").Split(' ');
            return int.Parse(stat[3]);
        }
    }
}