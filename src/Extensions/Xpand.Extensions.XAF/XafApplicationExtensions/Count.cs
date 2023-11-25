using System;
using System.Linq.Expressions;
using DevExpress.ExpressApp;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;

namespace Xpand.Extensions.XAF.XafApplicationExtensions {
    public static partial class XafApplicationExtensions {
        public static int Count<T>(this XafApplication application, Expression<Func<T, bool>> expression = null)
            where T : class {
            using var objectSpace = application.CreateObjectSpace();
            return objectSpace.Count(expression ?? (arg => true));
        }
        public static int ProviderCount<T>(this XafApplication application, Expression<Func<T, bool>> expression = null)
            where T : class {
            using var objectSpace = application.ObjectSpaceProvider.CreateObjectSpace();
            return objectSpace.Count(expression ?? (arg => true));
        }
    }
}