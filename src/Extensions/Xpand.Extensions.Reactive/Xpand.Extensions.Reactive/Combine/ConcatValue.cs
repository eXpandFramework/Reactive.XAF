using System;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Combine {
    public static partial class Combine {
        public static IObservable<TTarget> ConcatValue<TSource,TTarget>(this IObservable<TSource> source, TTarget value) 
            => source.Select(_ => default(TTarget)).WhenNotDefault().Concat(value.ReturnObservable());
    }
}