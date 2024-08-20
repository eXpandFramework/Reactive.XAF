using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Fasterflect;
using Xpand.Extensions.XAF.ViewExtensions;

namespace Xpand.Extensions.XAF.FrameExtensions {
    public partial class FrameExtensions {
        public static bool When<T>(this T frame, params Nesting[] nesting) where T : Frame 
            => nesting.Any(item => item == Nesting.Any || frame is NestedFrame && item == Nesting.Nested ||
                                         !(frame is NestedFrame) && item == Nesting.Root);
        public static bool When<T>(this T frame, params string[] viewIds) where T : Frame 
            => viewIds.Contains(frame.View?.Id);
        public static IEnumerable<DashboardViewItem> When(this IEnumerable<DashboardViewItem> source, params ViewType[] viewTypes) 
            => source.Where(item => viewTypes.All(viewType => item.InnerView.Is(viewType)));
        public static bool When<T>(this T frame, params ViewType[] viewTypes) where T : Frame 
            => viewTypes.Any(viewType =>viewType==ViewType.Any|| frame.View is CompositeView compositeView && compositeView.Is(viewType));
        
        public static bool When<T>(this T frame, params Type[] types) where T : Frame 
            => types.Any(item => frame.View is ObjectView objectView && objectView.Is(objectType:item));
    }
}