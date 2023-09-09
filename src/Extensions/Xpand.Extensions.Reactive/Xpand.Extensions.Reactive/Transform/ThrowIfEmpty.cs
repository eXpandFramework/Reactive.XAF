using System;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.Reactive.Combine;

namespace Xpand.Extensions.Reactive.Transform {
    public class SequenceIsEmptyException:Exception {
        public SequenceIsEmptyException(string message) : base(message){
        }
    }
    public static partial class Transform {
        public static IObservable<T> ThrowIfEmpty<T>(this IObservable<T> source,[CallerMemberName]string caller="")
            => source.SwitchIfEmpty(Observable.Defer(() => new SequenceIsEmptyException($"source is empty {caller}").Throw<T>()));
    }
}