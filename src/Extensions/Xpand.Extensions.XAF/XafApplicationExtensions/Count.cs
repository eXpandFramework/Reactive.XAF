using System;
using System.Linq;
using System.Linq.Expressions;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.XafApplicationExtensions {
    public static partial class XafApplicationExtensions {
        public static int Count<T>(this XafApplication application, Expression<Func<T, bool>> expression = null)
            where T : class {
            using var objectSpace = application.CreateObjectSpace();
            return objectSpace.GetObjectsQuery<T>().Count(expression ?? (arg => true));
        }
    }
}