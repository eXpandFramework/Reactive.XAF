using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class CollectionSourceService{
        public static IObservable<T> WhenCollectionReloaded<T>(this T collectionSourceBase) where T:CollectionSourceBase{
            return Observable.FromEventPattern<EventHandler, EventArgs>(
                    h => collectionSourceBase.CollectionReloaded += h, h => collectionSourceBase.CollectionReloaded -= h)
                .Select(_ => _.Sender).Cast<T>()
                .TraceRX();
        }

        public static IObservable<T> WhenCollectionChanged<T>(this T collectionSourceBase) where T:CollectionSourceBase{
            return Observable.FromEventPattern<EventHandler, EventArgs>(
                    h => collectionSourceBase.CollectionChanged += h, h => collectionSourceBase.CollectionChanged -= h)
                .Select(_ => _.Sender).Cast<T>()
                .TraceRX();
        }

    }
}