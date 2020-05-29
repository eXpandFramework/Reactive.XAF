using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Xpand.Extensions.LinqExtensions{
    public static partial class LinqExtensions{
        public static BindingList<T> ToBindingList<T>(this IEnumerable<T> source) => new BindingList<T>(source.ToArray());
    }
}