using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;

namespace Xpand.Extensions.XAF.Frame{
    public partial class FrameExtensions{
        public static IEnumerable<ActionBase> Actions(this DevExpress.ExpressApp.Frame frame){
            return frame.Actions<ActionBase>();
        }

        public static IEnumerable<T> Actions<T>(this DevExpress.ExpressApp.Frame frame) where T : ActionBase{
            return frame.Controllers.Cast<Controller>().SelectMany(controller => controller.Actions).OfType<T>();
        }
    }
}