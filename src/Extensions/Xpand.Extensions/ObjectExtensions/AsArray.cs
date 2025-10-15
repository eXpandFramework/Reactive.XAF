using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.TypeExtensions;


namespace Xpand.Extensions.ObjectExtensions {
    public static partial class ObjectExtensions {
    public static TCurrent[] AsArray<TCurrent>(this object currentResult) 
        => currentResult switch {
            null => [], TCurrent[] typedArray => typedArray,
            IEnumerable enumerable when typeof(TCurrent) == typeof(object) => enumerable.Cast<TCurrent>().ToArray(),
            TCurrent collection when collection is IEnumerable && !typeof(TCurrent).IsArray => [collection],
            IEnumerable<TCurrent> collection => collection.ToArray(),
            IEnumerable<object> objectCollection => objectCollection
                .SelectMany(o => {
                    if (o is TCurrent tCurrent) {
                        return tCurrent.YieldItem();
                    }
                    if (o is IEnumerable<TCurrent> cast) {
                        return cast;
                    }

                    if (typeof(TCurrent).IsArray && typeof(TCurrent).GetElementType() == o?.GetType()) {
                        var array = Array.CreateInstance(o!.GetType(), 1);
                        array.SetValue(o, 0);
                        return ((TCurrent)(object)array).YieldItem();
                    }

                    if (!typeof(TCurrent).IsList()) {
                        return o.YieldItem().Cast<TCurrent>();
                    }
                    var instance = ((IList)Activator.CreateInstance(typeof(TCurrent)));
                    instance!.Add(o);
                    return ((TCurrent)instance).YieldItem();
                }).ToArray(),
            _ => [(TCurrent)currentResult]
        };
    }
}