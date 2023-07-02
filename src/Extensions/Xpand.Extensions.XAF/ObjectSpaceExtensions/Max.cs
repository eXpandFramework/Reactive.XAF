using System;
using System.Linq;
using System.Linq.Expressions;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static int Max<T>(this IObjectSpace objectSpace, Expression<Func<T, bool>> expression, Expression<Func<T, int>> maxExpression)
            => objectSpace.GetObjectsQuery<T>().Where(expression).Max(maxExpression);
    }
}