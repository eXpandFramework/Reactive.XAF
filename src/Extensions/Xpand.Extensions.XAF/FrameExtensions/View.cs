using System;
using DevExpress.ExpressApp;
using Xpand.Extensions.XAF.XafApplicationExtensions;

namespace Xpand.Extensions.XAF.FrameExtensions{
    public partial class FrameExtensions{
        public static void SetDetailView(this Frame frame, Type objectType) 
            => frame.SetView(frame.Application.NewView(frame.Application.FindDetailViewId(objectType)));
    }
}