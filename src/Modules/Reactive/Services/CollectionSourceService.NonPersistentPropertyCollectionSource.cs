using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using Xpand.Extensions.EventArgExtensions;

namespace Xpand.XAF.Modules.Reactive.Services {
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

}