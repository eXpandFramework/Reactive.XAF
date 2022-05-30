using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<decimal> LiveRange(this IObservable<decimal> source) 
            => source.Publish(p => p.LiveMax().Zip(p.LiveMin(), (a, b) => a - b).Skip(1));

        public static IObservable<int> LiveRange(this IObservable<int> source) 
            => source.Publish(p => p.LiveMax().Zip(p.LiveMin(), (a, b) => a - b).Skip(1));

        public static IObservable<double> LiveRange(this IObservable<double> source) 
            => source.Publish(p => p.LiveMax().Zip(p.LiveMin(), (a, b) => a - b).Skip(1));
        
    }
}