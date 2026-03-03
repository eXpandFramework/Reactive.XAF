using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DevExpress.Data;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.CollectionSourceExtensions{
    public static partial class CollectionSourceExtensions{
        public static IEnumerable<object> Objects(this CollectionSourceBase collectionSourceBase) => collectionSourceBase.Objects<object>();

        public static IEnumerable<T> Objects<T>(this CollectionSourceBase collectionSourceBase) {
	        if (collectionSourceBase == null) {
		        return [];
	        }
	        if (collectionSourceBase.Collection is IEnumerable collection)
		        return collection.OfType<T>();
	        if (collectionSourceBase.Collection is IListSource listSource) {
		        var list = listSource.GetList();
		        return list is IListServer listServer ? listServer.ToList<T>() : list.OfType<T>();
	        }

	        if (collectionSourceBase is PropertyCollectionSource propertyCollectionSource) {
		        var masterObject = propertyCollectionSource.MasterObject;
		        return masterObject != null ? ((IEnumerable)propertyCollectionSource.MemberInfo.GetValue(masterObject)).OfType<T>() : [];
	        }
	        return collectionSourceBase.Collection is QueryableCollection queryableCollection
		        ? ((IEnumerable<T>)queryableCollection.Queryable).ToArray() : throw new NotImplementedException($"{collectionSourceBase}");
        }

        [SuppressMessage("ReSharper", "ForCanBeConvertedToForeach")]
        public static IEnumerable<T> ToList<T>(this IListServer listServer){
	        IList<T> list = new List<T>();
	        for (int i = 0; i < listServer.Count; i++) {
		        list.Add((T)listServer[i]);
	        }
	        return list;
        }
    }
}