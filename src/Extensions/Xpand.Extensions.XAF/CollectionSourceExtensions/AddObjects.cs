using System.Linq;
using DevExpress.ExpressApp;
using Fasterflect;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.ObjectExtensions;

namespace Xpand.Extensions.XAF.CollectionSourceExtensions {
    public static partial class CollectionSourceExtensions {
        public static T AddObjects<T,T2>(this T collection, T2[] objects,bool notifyLoaded=false) where T : DynamicCollection {
            if (objects.Any()) {
                var list = collection.GetPropertyValue("Objects").ToList<T2>();
                list.AddRange(objects.SkipLastN(1),true);
                list.Add(objects.Last(),true);
            }
            if (notifyLoaded) {
                collection.CallMethod("RaiseLoaded");
            }
            return collection;
        }
    }
}