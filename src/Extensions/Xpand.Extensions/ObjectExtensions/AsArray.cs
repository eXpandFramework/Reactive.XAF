using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.TypeExtensions;


namespace Xpand.Extensions.ObjectExtensions {
    public static partial class ObjectExtensions {
    public static TCurrent[] AsArray<TCurrent>(this object currentResult) {
        var lenghtMessage = currentResult.IsEnumerable()? $", Lenght: {((IEnumerable)currentResult).Cast<object>().Count()}":null;
        LogFast($"Entered. TCurrent: {typeof(TCurrent).Name}, Input Type: {currentResult?.GetType().Name}, Input Value: '{currentResult}'{lenghtMessage}");
        var result = currentResult switch {
            null => [], TCurrent[] typedArray => typedArray,
            IEnumerable enumerable when typeof(TCurrent) == typeof(object) => enumerable.Cast<TCurrent>().ToArray(),
            TCurrent collection when collection is IEnumerable && !typeof(TCurrent).IsArray => [collection],
            IEnumerable<TCurrent> collection => collection.ToArray(),
            IEnumerable<object> objectCollection => objectCollection
                .SelectMany(o => {
                    LogFast($"Processing item in IEnumerable<object>: '{o?.GetType().Name}'");
                    if (o is TCurrent tCurrent) {
                        LogFast($"Path 1: Item is directly castable to TCurrent ({typeof(TCurrent).Name}).");
                        return tCurrent.YieldItem();
                    }
                    if (o is IEnumerable<TCurrent> cast) {
                        LogFast($"Path 2: Item is IEnumerable<TCurrent>.");
                        return cast;
                    }

                    if (typeof(TCurrent).IsArray && typeof(TCurrent).GetElementType() == o?.GetType()) {
                        LogFast($"Path 3 (Element Promotion): TCurrent is array ({typeof(TCurrent).Name}) and item type ({o?.GetType().Name}) matches element type. Promoting item to single-element array.");
                        var array = Array.CreateInstance(o!.GetType(), 1);
                        array.SetValue(o, 0);
                        return ((TCurrent)(object)array).YieldItem();
                    }

                    if (!typeof(TCurrent).IsList()) {
                        LogFast($"Path 4: TCurrent is not a list. Attempting to cast item.");
                        return o.YieldItem().Cast<TCurrent>();
                    }

                    LogFast($"Path 5: TCurrent is a list. Creating instance and adding item.");
                    var instance = ((IList)Activator.CreateInstance(typeof(TCurrent)));
                    instance!.Add(o);
                    return ((TCurrent)instance).YieldItem();
                }).ToArray(),
            _ => [(TCurrent)currentResult]
        };
        LogFast($"Exiting. Returning array of type {result.GetType().Name} with {result.Length} elements.");
        return result;
    }

    }
}