using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using JetBrains.Annotations;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Filter{
    public static partial class Filter{
        public static IObservable<TSource> WhenDefault<TSource>(this IObservable<TSource> source){
            return source.Where(_ => {
                var def = default(TSource);
                return def != null && def.Equals(_);
            });
        }
    }
}