using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<T> SampleSometimes<T>(this IObservable<T> source, TimeSpan sampleTime, IObservable<bool> isSamplingOn) 
            => source.Sometimes(obs => obs.Sample(sampleTime), isSamplingOn);
        
    }
}