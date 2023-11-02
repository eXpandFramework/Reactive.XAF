using System.Threading;
using System.Threading.Tasks;

namespace Xpand.Extensions.TaskExtensions {
    public static partial class TaskExtensions {
        public static CancellationToken AsCancelable(this Task task) {
            var cts = new CancellationTokenSource();
            task.ContinueWith(_ => {
                cts.Cancel();
                cts.Dispose();
            }, TaskScheduler.Default);
            return cts.Token;
        }
    }
}