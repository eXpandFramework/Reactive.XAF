using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class CollectionSourceService {

        public static IObservable<ProxyCollection> WhenProxyCollectionChanged(this CollectionSourceBase collectionSourceBase) 
            => collectionSourceBase.Collection is not ProxyCollection proxyCollection ? Observable.Empty<ProxyCollection>()
                : Observable.FromEventPattern<ListChangedEventHandler, EventArgs>(h => proxyCollection.ListChanged += h,
                        h => proxyCollection.ListChanged -= h, ImmediateScheduler.Instance)
                    .Select(_ => _.Sender).Cast<ProxyCollection>().TraceRX(_ => collectionSourceBase.ObjectTypeInfo.Type.FullName);

        public static IObservable<T> WhenCollectionReloaded<T>(this T collectionSourceBase) where T:CollectionSourceBase 
            => Observable.FromEventPattern<EventHandler, EventArgs>(
                    h => collectionSourceBase.CollectionReloaded += h, h => collectionSourceBase.CollectionReloaded -= h,ImmediateScheduler.Instance)
                .Select(_ => _.Sender).Cast<T>()
                .TraceRX(c => c.ObjectTypeInfo.Type.FullName);

        public static IObservable<T> WhenCollectionChanged<T>(this T collectionSourceBase) where T:CollectionSourceBase 
            => Observable.FromEventPattern<EventHandler, EventArgs>(
                    h => collectionSourceBase.CollectionChanged += h, h => collectionSourceBase.CollectionChanged -= h,ImmediateScheduler.Instance)
                .Select(_ => _.Sender).Cast<T>()
                .TraceRX();
        
        public static IObservable<(T sender, FetchObjectsEventArgs e)> WhenFetchObjects<T>(this T collection) where T:DynamicCollection
            => Observable.FromEventPattern<EventHandler<FetchObjectsEventArgs>, FetchObjectsEventArgs>(
                    h => collection.FetchObjects += h, h => collection.FetchObjects -= h,ImmediateScheduler.Instance)
                .TransformPattern<FetchObjectsEventArgs,T>();
        
        public static IObservable<DynamicCollection> WhenLoaded(this DynamicCollection collection) 
            => Observable.FromEventPattern<EventHandler, EventArgs>(
                    h => collection.Loaded += h, h => collection.Loaded -= h,ImmediateScheduler.Instance)
                .Select(_ => collection);

        public static IObservable<T> WhenDisposed<T>(this T collectionSourceBase) where T:CollectionSourceBase 
            => Observable.FromEventPattern<EventHandler, EventArgs>(
                    h => collectionSourceBase.Disposed += h, h => collectionSourceBase.Disposed -= h,ImmediateScheduler.Instance)
                .Select(_ => _.Sender).Cast<T>()
                .TraceRX();
    }
    public class NonPersistentPropertyCollectionSource : PropertyCollectionSource{
        readonly Subject<GenericEventArgs<IEnumerable<object>>> _datasourceSubject=new();
        public NonPersistentPropertyCollectionSource(IObjectSpace objectSpace, Type masterObjectType, object masterObject, IMemberInfo memberInfo, CollectionSourceDataAccessMode dataAccessMode, CollectionSourceMode mode) : base(objectSpace, masterObjectType, masterObject, memberInfo, dataAccessMode, mode){
        }

        public NonPersistentPropertyCollectionSource(IObjectSpace objectSpace, Type masterObjectType, object masterObject, IMemberInfo memberInfo, bool isServerMode, CollectionSourceMode mode) : base(objectSpace, masterObjectType, masterObject, memberInfo, isServerMode, mode){
        }

        public NonPersistentPropertyCollectionSource(IObjectSpace objectSpace, Type masterObjectType, object masterObject, IMemberInfo memberInfo, CollectionSourceMode mode) : base(objectSpace, masterObjectType, masterObject, memberInfo, mode){
        }

        public NonPersistentPropertyCollectionSource(IObjectSpace objectSpace, Type masterObjectType, object masterObject, IMemberInfo memberInfo) : base(objectSpace, masterObjectType, masterObject, memberInfo){
        }

        public IObservable<GenericEventArgs<IEnumerable<object>>> Datasource => _datasourceSubject.AsObservable();

        protected override object CreateCollection(){
            var handledEventArgs = new GenericEventArgs<IEnumerable<object>>();
            _datasourceSubject.OnNext(handledEventArgs);
            return handledEventArgs.Handled ? handledEventArgs.Instance : base.CreateCollection();
        }
    }


    public class ReactiveCollection<T> : DynamicCollection,IList<T> {
        public ReactiveCollection(IObjectSpace objectSpace) : base(objectSpace, typeof(T), null, null, false) {
        }

        public IEnumerator<T> GetEnumerator() => ((IEnumerable) this).GetEnumerator().Cast<T>();

        public void Add(T item) => base.Add(item);

        public bool Contains(T item) => base.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => base.CopyTo(array, arrayIndex);

        public bool Remove(T item) {
            if (Objects.IndexOf(item)>-1) {
                base.Remove(item);
                return true;
            }

            return false;
        }

        public int IndexOf(T item) => base.IndexOf(item);

        public void Insert(int index, T item) => base.Insert(index, item);

        T IList<T>.this[int index] {
            get => (T) base[index];
            set => base[index]=value;
        }
    }

}