using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;

namespace Xpand.Extensions.XAF.Frame{
    public partial class FrameExtensions{
        public static ActionBase Action(this DevExpress.ExpressApp.Frame frame, string id) => frame.Actions(id).FirstOrDefault();

        public static IEnumerable<ActionBase> Actions(this DevExpress.ExpressApp.Frame frame,params string[] actiondIds) => frame.Actions<ActionBase>(actiondIds);
        
        public static (TModule module,DevExpress.ExpressApp.Frame frame) Action<TModule>(this DevExpress.ExpressApp.Frame frame) where TModule:ModuleBase => 
            (frame.Application.Modules.FindModule<TModule>(),frame);

        public static IEnumerable<T> Actions<T>(this DevExpress.ExpressApp.Frame frame,params string[] actiondIds) where T : ActionBase =>
            frame.Controllers.Cast<Controller>().SelectMany(controller => controller.Actions).OfType<T>()
                .Where(_ => !actiondIds.Any()|| actiondIds.Any(s => s==_.Id));
    }
}