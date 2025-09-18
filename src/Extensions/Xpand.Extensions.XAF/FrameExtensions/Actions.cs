using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.ViewExtensions;

namespace Xpand.Extensions.XAF.FrameExtensions{
    public partial class FrameExtensions {
        public static IEnumerable<TViewType> DashboardViewItems<TViewType>(this Frame frame,params Type[] objectTypes) where TViewType:View
            => frame.DashboardViewItems(objectTypes).Select(item => item.Frame.View as TViewType).WhereNotDefault();
        
        public static IEnumerable<DashboardViewItem> DashboardViewItems(this Frame frame,Type objectType,params ViewType[] viewTypes) 
            => frame.DashboardViewItems(objectType.YieldItem().ToArray()).When(viewTypes);
        
        public static IEnumerable<DashboardViewItem> DashboardViewItems(this Frame frame,params ViewType[] viewTypes) 
            => frame.DashboardViewItems(typeof(object).YieldItem().ToArray()).When(viewTypes);
        public static IEnumerable<DashboardViewItem> DashboardViewItems(this Frame frame,ViewType viewType,params Type[] objectTypes) 
            => frame.DashboardViewItems(objectTypes).When(viewType);
        
        public static IEnumerable<DashboardViewItem> DashboardViewItems(this Frame frame,params Type[] objectTypes) 
            => frame.View.ToCompositeView().GetItems<DashboardViewItem>().Where(item => item.InnerView.Is(objectTypes));
        
        public static void ExecuteRefreshAction(this Frame frame) => frame.GetController<RefreshController>().RefreshAction.DoExecute();
        public static object ParentObject(this Frame frame) => frame.ParentObject<object>() ;
        public static T ParentObject<T>(this Frame frame) where T : class
            => frame.AsNestedFrame()?.ViewItem.CurrentObject as T;
        
        public static bool ParentIsNull(this Frame frame)  => frame.ParentObject<object>()==null;
        public static NestedFrame AsNestedFrame(this Frame frame) => frame.As<NestedFrame>(); 
        public static NestedFrame ToNestedFrame(this Frame frame) => (NestedFrame)frame; 
        public static ActionBase Action(this Frame frame, string id) 
            => frame.Actions(id).FirstOrDefault();
        public static SimpleAction SimpleAction(this Frame frame, string id) 
            => frame.Actions<SimpleAction>(id).FirstOrDefault();
        public static PopupWindowShowAction PopupWindowShowAction(this Frame frame, string id) 
            => frame.Actions<PopupWindowShowAction>(id).FirstOrDefault();
        public static SingleChoiceAction SingleChoiceAction(this Frame frame, string id) 
            => frame.Actions<SingleChoiceAction>(id).FirstOrDefault();
        public static ParametrizedAction ParametrizedAction(this Frame frame, string id) 
            => frame.Actions<ParametrizedAction>(id).FirstOrDefault();
        
        public static T Action<T>(this Frame frame, string id) where T:ActionBase
            => frame.Actions<T>(id).FirstOrDefault();

        public static IEnumerable<ActionBase> AvailableActions(this Frame frame, params string[] actionsIds)
            => frame.Actions(actionsIds).Where(action => action.Available());
        
        public static IEnumerable<ActionBase> ActiveActions(this Frame frame, params string[] actionsIds)
            => frame.Actions(actionsIds).Where(action => action.Active);
        public static IEnumerable<ActionBase> Actions(this Frame frame,params string[] actionsIds) 
            => frame.Actions<ActionBase>(actionsIds);
        public static (TModule module,Frame frame) Action<TModule>(this Frame frame) where TModule:ModuleBase 
            => (frame.Application.Modules.FindModule<TModule>(),frame);

        public static IEnumerable<T> Actions<T>(this Frame frame,params string[] actionsIds) where T : ActionBase 
            => frame.Controllers.Cast<Controller>().SelectMany(controller => controller.Actions).OfType<T>()
                .Where(actionBase => !actionsIds.Any()|| actionsIds.Any(s => s==actionBase.Id));

        public static SingleChoiceAction NewObjectAction(this Frame frame) 
            => frame.GetController<NewObjectViewController>().NewObjectAction;
        public static SimpleAction SaveAction(this Frame frame) 
            => frame.GetController<ModificationsController>().SaveAction;
        public static SimpleAction RefreshAction(this Frame frame) 
            => frame.GetController<RefreshController>().RefreshAction;
        
        public static SimpleAction SaveAndCloseAction(this Frame frame) 
            => frame.GetController<ModificationsController>().SaveAndCloseAction;
    }
}