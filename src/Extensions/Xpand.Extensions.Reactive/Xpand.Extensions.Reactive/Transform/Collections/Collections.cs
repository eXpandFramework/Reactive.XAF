using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform.Collections {
    public static class Collections {
        public static IObservable<(T sender, ListChangedEventArgs e)> WhenListChanged<T>(this T source,params ListChangedType[] changedTypes) where T:IBindingList
            => Observable.FromEventPattern<ListChangedEventHandler,ListChangedEventArgs>(h => source.ListChanged+=h,h => source.ListChanged-=h,Scheduler.Immediate)
                .TransformPattern<ListChangedEventArgs,T>()
                .Where(t =>!changedTypes.Any()|| changedTypes.Contains(t.e.ListChangedType));
    }
}
