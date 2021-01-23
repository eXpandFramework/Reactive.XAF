using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Xpand.Extensions.Threading {
    public sealed class SingleThreadedSynchronizationContext : SynchronizationContext {
        private readonly BlockingCollection<(SendOrPostCallback d, object state)> _queue =
            new BlockingCollection<(SendOrPostCallback, object)>();

        public override void Post(SendOrPostCallback d, object state) => _queue.Add((d, state));

        public static void Await(Func<Task> invoker) {
            var originalContext = Current;
            try {
                var context = new SingleThreadedSynchronizationContext();
                SetSynchronizationContext(context);

                var task = invoker.Invoke();
                task.ContinueWith(_ => context._queue.CompleteAdding());

                while (context._queue.TryTake(out var work, Timeout.Infinite))
                    work.d.Invoke(work.state);

                task.GetAwaiter().GetResult();
            }
            finally {
                SetSynchronizationContext(originalContext);
            }
        }
    }
}