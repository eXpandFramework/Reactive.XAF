using DevExpress.ExpressApp;
using Fasterflect;

namespace Xpand.Extensions.XAF.Frame{
    public static partial class FrameExtensions{
        public static Controller GetController(this DevExpress.ExpressApp.Frame frame, System.Type controllerType){
            return (Controller) frame.CallMethod(new[]{controllerType}, "GetController");
        }
    }
}