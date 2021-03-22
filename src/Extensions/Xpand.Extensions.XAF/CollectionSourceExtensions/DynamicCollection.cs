using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Fasterflect;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.XAF.CollectionSourceExtensions {
    public static partial class CollectionSourceExtensions {
        public static List<object> Objects<T>(this T collection) where T : DynamicCollection 
            => ((List<object>) collection.GetPropertyValue("Objects"));

        public static T AddObjects<T,T2>(this T collection, T2[] objects,bool notifyLoaded=false) where T : DynamicCollection {
            if (objects.Any()) {
                var list = collection.Objects();
                list.AddRange(objects.SkipLastN(1).Cast<object>());
                collection.Add(objects.Last());
            }
            if (notifyLoaded) {
                collection.CallMethod("RaiseLoaded");
            }
            return collection;
        }
    }
}