using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.CollectionSourceExtensions{
    public static partial class CollectionSourceExtensions{
        public static IEnumerable<object> Objects(this CollectionSourceBase collectionSourceBase) => collectionSourceBase.Objects<object>();

        public static IEnumerable<T> Objects<T>(this CollectionSourceBase collectionSourceBase) {
	        if (collectionSourceBase.Collection is IEnumerable collection)
		        return collection.Cast<T>();
	        if (collectionSourceBase.Collection is IListSource listSource)
		        return listSource.GetList().Cast<T>();
	        if (collectionSourceBase is PropertyCollectionSource propertyCollectionSource) {
		        var masterObject = propertyCollectionSource.MasterObject;
		        return masterObject != null ? ((IEnumerable)propertyCollectionSource.MemberInfo.GetValue(masterObject)).Cast<T>() : Enumerable.Empty<T>();
	        }
	        return collectionSourceBase.Collection is QueryableCollection queryableCollection
		        ? ((IEnumerable<T>)queryableCollection.Queryable).ToArray() : throw new NotImplementedException($"{collectionSourceBase}");
        }
    }
}