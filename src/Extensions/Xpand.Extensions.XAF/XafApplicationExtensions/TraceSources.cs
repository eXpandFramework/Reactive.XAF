using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DevExpress.ExpressApp;
using Fasterflect;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.XAF.XafApplicationExtensions {
    public static partial class XafApplicationExtensions {
        public static IEnumerable<TraceSource> TraceSources(this XafApplication application)
            => application.Modules.SelectMany(m => m.GetType().Properties(Flags.StaticPublic)
                .Where(info => info.PropertyType == typeof(TraceSource))
                .Select(info => info.GetValue(null))).Cast<TraceSource>();

        public static IEnumerable<TraceSource> SourceLevel(this IEnumerable<TraceSource> sources,SourceLevels sourceLevel) 
            => sources.Execute(source => source.Switch.Level=sourceLevel);
    }
}