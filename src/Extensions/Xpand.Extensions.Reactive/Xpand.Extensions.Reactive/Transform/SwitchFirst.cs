using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<TSource> SwitchFirst<TSource>(this IObservable<IObservable<TSource>> source) 
            => Observable.Create<TSource>(o => {
                    bool free = true;
                    return source.Where(_ => free).Do(_ => free = false)
                        .Select(el => el.Finally(() => free = true)).Switch()
                        .Subscribe(o);
                }
            );
    }
}