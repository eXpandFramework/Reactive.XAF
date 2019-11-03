using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Filter{
    public static partial class Filter{
        public static IObservable<IList<TOut>> WhenNotEmpty<TOut>(this IObservable<IList<TOut>> source){
            return source.Where(outs => outs.Any());
        }
    }
}