using System;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.FaultHub;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Reactive.Services.Controllers;

namespace Xpand.XAF.Modules.Reactive.Services{
    static class ShowInstanceDetailViewService {
        #region High-Level Logical Operations
        internal static IObservable<Unit> ShowInstanceDetailView(this XafApplication application,params  Type[] objectTypes) 
            => application.WhenFrameCreated().WhenViewControllersActivated()
                .WhenFrame(objectTypes).WhenFrame(ViewType.ListView).Where(frame => frame.View.Model.ToListView().MasterDetailMode==MasterDetailMode.ListViewAndDetailView)
                .SelectMany(frame => frame.View.ToListView().WhenCreateCustomCurrentObjectDetailView()
                    .DoWhen(e =>e.ListViewCurrentObject!=null,e => 
                        e.DetailView = frame.View.ToListView().NewDetailView(e.ListViewCurrentObject)))
                .MergeToUnit(application.WhenViewOnFrame().WhenFrame(objectTypes).WhenFrame(ViewType.ListView)
                    .Where(frame => frame.View.Model.ToListView().MasterDetailMode==MasterDetailMode.ListViewOnly)
                    .WhenIsNotOnLookupPopupTemplate().ToController<ListViewProcessCurrentObjectController>()
                    .CustomProcessSelectedItem(OverwriteShowInstanceDetailView)
                    .Where(e => e.View().ObjectTypeInfo.Type.IsInstanceOfType(e.View().CurrentObject))
                    .Do(e => e.ShowViewParameters.CreatedView = e.View().ToListView().NewDetailView(e.Action.View().CurrentObject)))
                .PushStackFrame();
        #endregion

        #region Low-Level Plumbing
        private static bool OverwriteShowInstanceDetailView(SimpleActionExecuteEventArgs e) {
            var objectTypeInfo = e.View().ObjectTypeInfo;
            var property = objectTypeInfo.FindAttribute<ShowInstanceDetailViewAttribute>().Property;
            if (objectTypeInfo.Type != e.View().CurrentObject.GetType()) return property==null||objectTypeInfo.FindMember(property).GetValue(e.Action.View().CurrentObject) != null;
            return property != null && objectTypeInfo.FindMember(property).GetValue(e.Action.View().CurrentObject) != null;
        }

        static DetailView NewDetailView(this ListView listView,object o) {
            if (o == null) return null;
            var property = o.GetType().ToTypeInfo().FindAttribute<ShowInstanceDetailViewAttribute>().Property;
            o = property is { } prop ? o.GetType().ToTypeInfo().FindMember(prop).GetValue(o) : o;
            if (o==null) return null;
            var objectSpace = property==null? (listView.MasterDetailMode==MasterDetailMode.ListViewOnly?listView.Application().CreateObjectSpace():listView.ObjectSpace):listView.Application().CreateObjectSpace();
            var modelClass = o.GetType().GetModelClass();
            return modelClass==null ? null : listView.Application().CreateDetailView(objectSpace, modelClass.DefaultDetailView,property != null || listView.IsRoot, objectSpace.GetObject(o));
        }
        #endregion
    }
}