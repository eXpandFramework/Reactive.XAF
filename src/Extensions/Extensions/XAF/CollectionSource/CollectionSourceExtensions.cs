using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;

namespace Xpand.Source.Extensions.XAF.CollectionSource{
    internal static class CollectionSourceExtensions{
        public static IEnumerable<T> Objects<T>(this CollectionSourceBase collectionSourceBase){
            if (collectionSourceBase.Collection is IEnumerable collection){
                return collection.Cast<T>();
            }

            if (collectionSourceBase.Collection is IListSource listSource){
                var list = listSource.GetList();
                var result = new List<object>();
                for (int i = 0; i < list.Count; i++){
                    result.Add(list[i]);
                }
                return result.Cast<T>();
            }

            throw new NotImplementedException($"{collectionSourceBase}");
        }
    }
}