using System;
using System.Linq;
using System.Linq.Expressions;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static int Count<T>(this IObjectSpace objectSpace, Expression<Func<T, bool>> expression=null)
            => objectSpace.GetObjectsQuery<T>().Where(expression??(arg =>true) ).Count();
    }
}