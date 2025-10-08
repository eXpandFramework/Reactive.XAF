using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.NewDirectory1 {

    public class XpandScheduler(IScheduler scheduler,params IAsyncLocal[] locals) : IScheduler {
        

        private Func<IScheduler, TState, IDisposable> WrapAction<TState>(Func<IScheduler, TState, IDisposable> action) {
            
            
            var allLocals =  locals.Concat(Contexts).ToArray();
            LogFast($"ContextScheduler: Capturing {allLocals.Length} IAsyncLocal value(s).");
            var capturedValues = allLocals.Select(l => l.Value).ToArray();

            return (_, state) => {
                var originalValuesOnWorkerThread = allLocals.Select(l => l.Value).ToArray();
                try {
                    for (var i = 0; i < allLocals.Length; i++) {
                        allLocals[i].Value = capturedValues[i];
                    }
                    return action(this, state);
                }
                finally {
                    for (var i = 0; i < allLocals.Length; i++) {
                        allLocals[i].Value = originalValuesOnWorkerThread[i];
                    }
                }
            };
        }

        public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action) 
            => scheduler.Schedule(state, WrapAction(action));

        public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action) 
            => scheduler.Schedule(state, dueTime, WrapAction(action));

        public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action) 
            => scheduler.Schedule(state, dueTime, WrapAction(action));

        public DateTimeOffset Now => scheduler.Now;

        public static ConcurrentBag<IAsyncLocal> Contexts { get; } = new();
    }

}
