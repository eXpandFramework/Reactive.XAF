using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.ObjectExtensions;

namespace Xpand.Extensions.XAF.FrameExtensions{
    public partial class FrameExtensions {
        
        public static void ExecuteRefreshAction(this Frame frame) => frame.GetController<RefreshController>().RefreshAction.DoExecute();

        public static T ParentObject<T>(this Frame frame) => frame.As<NestedFrame>().ViewItem.View.CurrentObject.As<T>(); 
        public static NestedFrame AsNestedFrame(this Frame frame) => frame.As<NestedFrame>(); 
        public static NestedFrame ToNestedFrame(this Frame frame) => frame.To<NestedFrame>(); 
        public static ActionBase Action(this Frame frame, string id) 
            => frame.Actions(id).FirstOrDefault();
        public static SimpleAction SimpleAction(this Frame frame, string id) 
            => frame.Actions<SimpleAction>(id).FirstOrDefault();
        public static SingleChoiceAction SingleChoiceAction(this Frame frame, string id) 
            => frame.Actions<SingleChoiceAction>(id).FirstOrDefault();
        public static ParametrizedAction ParametrizedAction(this Frame frame, string id) 
            => frame.Actions<ParametrizedAction>(id).FirstOrDefault();
        public static T Action<T>(this Frame frame, string id) where T:ActionBase
            => frame.Actions<T>(id).FirstOrDefault();

        public static IEnumerable<ActionBase> Actions(this Frame frame,params string[] actionsIds) 
            => frame.Actions<ActionBase>(actionsIds);
        public static (TModule module,Frame frame) Action<TModule>(this Frame frame) where TModule:ModuleBase 
            => (frame.Application.Modules.FindModule<TModule>(),frame);

        public static IEnumerable<T> Actions<T>(this Frame frame,params string[] actionsIds) where T : ActionBase 
            => frame.Controllers.Cast<Controller>().SelectMany(controller => controller.Actions).OfType<T>()
                .Where(_ => !actionsIds.Any()|| actionsIds.Any(s => s==_.Id));
    }
}