using System.Diagnostics;
using System.Threading.Tasks;

namespace Xpand.Extensions.ProcessExtensions {
    public static partial class ProcessExtensions {
        public static Task<int> RunProcessAsync(this Process process) {
            process.EnableRaisingEvents = true;
            var tcs = new TaskCompletionSource<int>();
            process.Exited += (_, _) => {
                tcs.SetResult(process.ExitCode);
                process.Dispose();
            };
            process.Start();
            return tcs.Task;
        }
    }
}