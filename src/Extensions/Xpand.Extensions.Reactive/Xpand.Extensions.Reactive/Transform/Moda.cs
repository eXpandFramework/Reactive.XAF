using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<IGrouping<int, int>> LiveModa(this IObservable<int> source) 
            => source.GroupBy(a => a).Select(a => a.LiveCount().Select(b => Tuple.Create(a.Key, b))).Merge()
                .Scan(default(KeyValuePair<int, ImmutableStack<int>>), (a, b) => a.Key > b.Item2 ? a :
                    a.Key == b.Item2 ? new KeyValuePair<int, ImmutableStack<int>>(b.Item2, a.Value.Push(b.Item1)) :
                    new KeyValuePair<int, ImmutableStack<int>>(b.Item2, ImmutableStack<int>.Empty.Push(b.Item1)))
                .Select(a => a.Value.AsGroup(a.Key));

        public static IObservable<IGrouping<int, decimal>> LiveModa(this IObservable<decimal> source) 
            => source.GroupBy(a => a).Select(a => a.LiveCount().Select(b => Tuple.Create(a.Key, b)))
                .Merge().Scan(default(KeyValuePair<int, ImmutableStack<decimal>>), (a, b) 
                    => a.Key > b.Item2 ? a : a.Key == b.Item2 ? new KeyValuePair<int, ImmutableStack<decimal>>(b.Item2, a.Value.Push(b.Item1))
                        : new KeyValuePair<int, ImmutableStack<decimal>>(b.Item2, ImmutableStack<decimal>.Empty.Push(b.Item1)))
                .Select(a => a.Value.AsGroup(a.Key));
        
        
    }
}