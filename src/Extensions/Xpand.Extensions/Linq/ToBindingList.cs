using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Xpand.Extensions.Linq{
    public static partial class LinqExtensions{
        public static BindingList<T> ToBindingList<T>(this IEnumerable<T> source){
            return new BindingList<T>(source.ToArray());
        }
    }
}