using System;
using System.Linq;
using DevExpress.ExpressApp;
using Xpand.Extensions.XAF.ViewExtensions;

namespace Xpand.Extensions.XAF.FrameExtensions {
    public partial class FrameExtensions {
        public static bool Is<T>(this T frame, params Nesting[] nesting) where T : Frame 
            => nesting.Any(item => item == Nesting.Any || frame is NestedFrame && item == Nesting.Nested ||
                                         !(frame is NestedFrame) && item == Nesting.Root);

        public static bool Is<T>(this T frame, params ViewType[] viewTypes) where T : Frame 
            => viewTypes.Any(item =>item==ViewType.Any|| frame.View is ObjectView objectView && objectView.Is(item));
        
        public static bool Is<T>(this T frame, params Type[] types) where T : Frame 
            => types.Any(item => frame.View is ObjectView objectView && objectView.Is(objectType:item));
    }
}