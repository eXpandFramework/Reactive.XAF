using System;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static ConfiguredTaskAwaitable<T> ToTaskWithoutConfigureAwait<T>(this IObservable<T> source) => source.ToTask().ConfigureAwait(false);
    }
}