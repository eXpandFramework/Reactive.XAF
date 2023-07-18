using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static IObservable<TDisposable> Disposed<TDisposable>(this IObservable<TDisposable> source) where TDisposable:IComponent 
            => source.SelectMany(item => item.WhenDisposed());

        public static IObservable<TDisposable> WhenDisposed<TDisposable>(this TDisposable source,[CallerMemberName]string caller="") where TDisposable : IComponent {
            if (new[] {
                    // "WhenModelChanged" ,
                    // "Register_Parameterized_Action_For_Windows",
                    "WhenDatabaseVersionMismatch1"
                }.Contains(caller)) {
                return Observable.Empty<TDisposable>();
            }
            return Observable.FromEventPattern(source, nameof(source.Disposed), ImmediateScheduler).Take(1).To(source);
        }
    }
}