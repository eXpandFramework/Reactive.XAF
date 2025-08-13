using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class CollectionSourceService {

        public static IObservable<CollectionSourceBase> WhenCriteriaApplied(this CollectionSourceBase collectionSourceBase)
            => collectionSourceBase.ProcessEvent(nameof(CollectionSourceBase.CriteriaApplied))
                .TakeUntil(collectionSourceBase.WhenDisposed()).To(collectionSourceBase);
        public static IObservable<ProxyCollection> WhenProxyCollectionChanged(this CollectionSourceBase collectionSourceBase) 
            => collectionSourceBase.Collection is not ProxyCollection proxyCollection ? Observable.Empty<ProxyCollection>()
                : proxyCollection.ProcessEvent(nameof(ProxyCollection.ListChanged)).TakeUntil(collectionSourceBase.WhenDisposed())
                    .Select(pattern => pattern).Cast<ProxyCollection>().TraceRX(_ => collectionSourceBase.ObjectTypeInfo.Type.FullName);

        public static IObservable<T> WhenCollectionReloaded<T>(this T collection) where T:CollectionSourceBase 
            => collection.ProcessEvent(nameof(CollectionSourceBase.CollectionReloaded))
                .TakeUntil(collection.WhenDisposed())
                .TraceRX(c => c.ObjectTypeInfo.Type.FullName);

        public static IObservable<T> WhenCollectionChanged<T>(this T collectionSourceBase) where T:CollectionSourceBase 
            => collectionSourceBase.ProcessEvent(nameof(CollectionSourceBase.CollectionChanged))
                .TakeUntil(collectionSourceBase.WhenDisposed());
        
        public static IObservable<FetchObjectsEventArgs> WhenFetchObjects<T>(this T collection) where T:DynamicCollection
            => collection.ProcessEvent<FetchObjectsEventArgs>(nameof(DynamicCollection.FetchObjects));
        
        public static IObservable<DynamicCollection> WhenLoaded(this DynamicCollection collection) 
            => collection.ProcessEvent(nameof(DynamicCollection.Loaded)).To(collection).TakeWhileInclusive(dynamicCollection => !dynamicCollection.IsDisposed);

        public static IObservable<T> WhenDisposed<T>(this T collectionSourceBase) where T:CollectionSourceBase 
            => collectionSourceBase.ProcessEvent(nameof(CollectionSourceBase.Disposed));

        public static NonPersistentPropertyCollectionSource NewSource(this CreateCustomPropertyCollectionSourceEventArgs e,object masterObject=null) 
            => new(e.ObjectSpace, e.MasterObjectType, masterObject??e.MasterObject, e.MemberInfo, e.DataAccessMode,e.Mode);
    }



}