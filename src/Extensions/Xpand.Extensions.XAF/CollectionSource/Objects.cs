using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.CollectionSource{
    public static class CollectionSourceExtensions{
        public static IEnumerable<object> Objects(this CollectionSourceBase collectionSourceBase){
            return collectionSourceBase.Objects<object>();
        }

        public static IEnumerable<T> Objects<T>(this CollectionSourceBase collectionSourceBase){
            if (collectionSourceBase.Collection is IEnumerable collection){
                return collection.Cast<T>();
            }
            if (collectionSourceBase.Collection is IListSource listSource){
                return listSource.GetList().Cast<T>();
            }

            if (collectionSourceBase is PropertyCollectionSource propertyCollectionSource){
                return ((IEnumerable) propertyCollectionSource.MemberInfo.GetValue(propertyCollectionSource.MasterObject)).Cast<T>();
            }
            throw new NotImplementedException($"{collectionSourceBase}");
        }
    }
}