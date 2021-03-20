using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Fasterflect;

namespace Xpand.Extensions.LinqExtensions{
    public static partial class LinqExtensions{
        public static BindingList<T> ToBindingList<T>(this IEnumerable<T> source) 
            => new((IList<T>) (source is IList list?list:source.ToList()));

        public static IBindingList ToBindingList(this IEnumerable<object> source,Type objectType) {
            var bindingList = (IBindingList) typeof(BindingList<>).MakeGenericType(objectType).CreateInstance();
            foreach (object o in source) {
                bindingList.Add(o);
            }
            return bindingList;
        }
    }
}