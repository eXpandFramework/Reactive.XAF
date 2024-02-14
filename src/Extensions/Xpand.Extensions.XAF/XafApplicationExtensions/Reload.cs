using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ApplicationBuilder;
using Fasterflect;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.XAF.XafApplicationExtensions {
    public static partial class XafApplicationExtensions {
        public static IEnumerable<IList<T>> ReloadObjects<T>(this IEnumerable<T[]> source, XafApplication application)
            => source.Select(application.ReloadObjects);
        
        public static IList<T> ReloadObjects<T>(this XafApplication application, params T[] objects) {
            var objectSpace = application.CreateObjectSpace();
            var keys = objects.Select(arg => objectSpace.GetKeyValue(arg)).ToArray();
            return objectSpace.GetObjects<T>(new InOperator($"{objectSpace.GetKeyPropertyName(typeof(T))}",keys));
        }
    }
}