using System;
using DevExpress.ExpressApp;
using Fasterflect;

namespace Xpand.Source.Extensions.XAF.Frame{
    internal static partial class FrameExtensions{
        public static Controller GetController(this DevExpress.ExpressApp.Frame frame, Type controllerType){
            return (Controller) frame.CallMethod(new[]{controllerType}, "GetController");
        }
    }
}