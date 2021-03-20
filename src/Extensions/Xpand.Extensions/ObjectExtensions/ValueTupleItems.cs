using System;
using System.Collections.Generic;

namespace Xpand.Extensions.ObjectExtensions {
    public static partial class ObjectExtensions {
        public static object[] ValueTupleItems(this object obj) {
            var type = obj.GetType();
            return type.IsGenericType && GetItems.TryGetValue(type.GetGenericTypeDefinition(), out var itemGetter)
                ? itemGetter(obj) : new object[0];
        }

        static readonly IDictionary<Type,Func<object,object[]>> GetItems = new Dictionary<Type,Func<object,object[]>> {
            [typeof(ValueTuple<>)] = o => new object[] {((dynamic)o).Item1}
            ,   [typeof(ValueTuple<,>)] = o => new object[] {((dynamic)o).Item1, ((dynamic)o).Item2}
            ,   [typeof(ValueTuple<,,>)] = o => new object[] {((dynamic)o).Item1, ((dynamic)o).Item2, ((dynamic)o).Item3}
            ,   [typeof(ValueTuple<,,,>)] = o => new object[] {((dynamic)o).Item1, ((dynamic)o).Item2, ((dynamic)o).Item3,((dynamic)o).Item4}
            
        };

    }
}