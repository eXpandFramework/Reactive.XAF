using System;
using System.Threading;
using System.Threading.Tasks;

namespace Xpand.Extensions.Task{
    public static partial class TaskExtensions{
        public static async Task<TResult> Timeout<TResult>(this Task<TResult> task, TimeSpan timeout){
            using (var timeoutCancellationTokenSource = new CancellationTokenSource()){
                var completedTask = await System.Threading.Tasks.Task.WhenAny(task,
                    System.Threading.Tasks.Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask == task){
                    timeoutCancellationTokenSource.Cancel();
                    return await task;
                }

                throw new TimeoutException("The operation has timed out.");
            }
        }
    }
}