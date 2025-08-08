using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform.Collections {
    public static class Collections {
        public static IObservable<ListChangedEventArgs> WhenListChanged<T>(this T source,params ListChangedType[] changedTypes) where T:IBindingList
            => source.ProcessEvent<ListChangedEventArgs>(nameof(IBindingList.ListChanged))
                .Where(args =>!changedTypes.Any()|| changedTypes.Contains(args.ListChangedType));
    }
}
