using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static IObservable<TResult> ExhaustMap<TSource, TResult>(this IObservable<TSource> source, Func<TSource, Task<TResult>> function){
            return source.Scan(Task.FromResult<TResult>(default),
                    (previousTask, item) => !previousTask.IsCompleted ? previousTask : HideIdentity(function(item)))
                .DistinctUntilChanged()
                .Concat();
            async Task<TResult> HideIdentity(Task<TResult> task) => await task;
        }
    }
}