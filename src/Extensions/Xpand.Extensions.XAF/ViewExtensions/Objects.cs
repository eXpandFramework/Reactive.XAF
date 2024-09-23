using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.XAF.CollectionSourceExtensions;

namespace Xpand.Extensions.XAF.ViewExtensions {
    public static partial class ViewExtensions {
        public static IEnumerable<T> Objects<T>(this View view) 
            => view is DetailView ? ((T)view.CurrentObject).YieldItem().WhereNotDefault().ToArray()
            : view.ToListView().CollectionSource.Objects<T>();
        
        
        public static IEnumerable<object> Objects(this View view) => view.Objects<object>();
    }
}