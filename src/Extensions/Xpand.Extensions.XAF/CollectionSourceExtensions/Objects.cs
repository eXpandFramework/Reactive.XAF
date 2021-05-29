using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.CollectionSourceExtensions{
    public static partial class CollectionSourceExtensions{
        public static IEnumerable<object> Objects(this CollectionSourceBase collectionSourceBase) => collectionSourceBase.Objects<object>();

        public static IEnumerable<T> Objects<T>(this CollectionSourceBase collectionSourceBase) 
	        => collectionSourceBase.Collection is IEnumerable collection ? collection.Cast<T>() : collectionSourceBase.Collection is IListSource listSource
			        ? listSource.GetList().Cast<T>() : collectionSourceBase is PropertyCollectionSource propertyCollectionSource
				        ? ((IEnumerable) propertyCollectionSource.MemberInfo.GetValue(propertyCollectionSource.MasterObject)).Cast<T>()
				        : collectionSourceBase.Collection is QueryableCollection queryableCollection
					        ? ((IEnumerable<T>) queryableCollection.Queryable).ToArray()
					        : throw new NotImplementedException($"{collectionSourceBase}");
    }
}