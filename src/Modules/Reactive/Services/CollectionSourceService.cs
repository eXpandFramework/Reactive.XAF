using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using Xpand.Extensions.EventArg;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class CollectionSourceService{
        public static IObservable<T> WhenCollectionReloaded<T>(this T collectionSourceBase) where T:CollectionSourceBase{
            return Observable.FromEventPattern<EventHandler, EventArgs>(
                    h => collectionSourceBase.CollectionReloaded += h, h => collectionSourceBase.CollectionReloaded -= h,ImmediateScheduler.Instance)
                .Select(_ => _.Sender).Cast<T>()
                .TraceRX(c => c.ObjectTypeInfo.Type.FullName);
        }

        public static IObservable<T> WhenCollectionChanged<T>(this T collectionSourceBase) where T:CollectionSourceBase{
            return Observable.FromEventPattern<EventHandler, EventArgs>(
                    h => collectionSourceBase.CollectionChanged += h, h => collectionSourceBase.CollectionChanged -= h,ImmediateScheduler.Instance)
                .Select(_ => _.Sender).Cast<T>()
                .TraceRX();
        }

    }
    public class NonPersistePropertyCollectionSource : PropertyCollectionSource{
        readonly Subject<GenericEventArgs<IEnumerable<object>>> _datasourceSubject=new Subject<GenericEventArgs<IEnumerable<object>>>();
        public NonPersistePropertyCollectionSource(IObjectSpace objectSpace, Type masterObjectType, object masterObject, IMemberInfo memberInfo, CollectionSourceDataAccessMode dataAccessMode, CollectionSourceMode mode) : base(objectSpace, masterObjectType, masterObject, memberInfo, dataAccessMode, mode){
        }

        public NonPersistePropertyCollectionSource(IObjectSpace objectSpace, Type masterObjectType, object masterObject, IMemberInfo memberInfo, bool isServerMode, CollectionSourceMode mode) : base(objectSpace, masterObjectType, masterObject, memberInfo, isServerMode, mode){
        }

        public NonPersistePropertyCollectionSource(IObjectSpace objectSpace, Type masterObjectType, object masterObject, IMemberInfo memberInfo, CollectionSourceMode mode) : base(objectSpace, masterObjectType, masterObject, memberInfo, mode){
        }

        public NonPersistePropertyCollectionSource(IObjectSpace objectSpace, Type masterObjectType, object masterObject, IMemberInfo memberInfo) : base(objectSpace, masterObjectType, masterObject, memberInfo){
        }

        public IObservable<GenericEventArgs<IEnumerable<object>>> Datasource => _datasourceSubject.AsObservable();

        protected override object CreateCollection(){
            var handledEventArgs = new GenericEventArgs<IEnumerable<object>>();
            _datasourceSubject.OnNext(handledEventArgs);
            return handledEventArgs.Handled ? handledEventArgs.Instance : base.CreateCollection();
        }
    }

}