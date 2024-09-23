using System;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Reactive.Services.Controllers;

namespace Xpand.XAF.Modules.Reactive.Services{
    static class ShowInstanceDetailViewService {
        internal static IObservable<Unit> ShowInstanceDetailView(this XafApplication application,params  Type[] objectTypes) 
            => application.WhenFrameCreated().WhenViewControllersActivated()
                .WhenFrame(objectTypes).WhenFrame(ViewType.ListView).Where(frame => frame.View.Model.ToListView().MasterDetailMode==MasterDetailMode.ListViewAndDetailView)
                .SelectMany(frame => frame.View.ToListView().WhenCreateCustomCurrentObjectDetailView()
                    .DoWhen(e =>e.ListViewCurrentObject!=null,e => 
                        e.DetailView = frame.View.ToListView().NewDetailView(e.ListViewCurrentObject)))
                .MergeToUnit(application.WhenViewOnFrame().WhenFrame(objectTypes).WhenFrame(ViewType.ListView)
                    // .Where(frame => frame.View.ObjectTypeInfo.FindAttribute<ShowInstanceDetailViewAttribute>().Property!=null)
                    .Where(frame => frame.View.Model.ToListView().MasterDetailMode==MasterDetailMode.ListViewOnly)
                    .WhenIsNotOnLookupPopupTemplate().ToController<ListViewProcessCurrentObjectController>()
                    .CustomProcessSelectedItem(OverwriteShowInstanceDetailView)
                    .Where(e => e.View().ObjectTypeInfo.Type.IsInstanceOfType(e.View().CurrentObject))
                    .Do(e => e.ShowViewParameters.CreatedView = e.View().ToListView().NewDetailView(e.Action.View().CurrentObject)));

        private static bool OverwriteShowInstanceDetailView(SimpleActionExecuteEventArgs e) 
            => e.View().ObjectTypeInfo.Type!=e.View().CurrentObject.GetType()||e.View().ObjectTypeInfo.FindAttribute<ShowInstanceDetailViewAttribute>().Property!=null;

        static DetailView NewDetailView(this ListView listView,object o) {
            if (o == null) return null;
            var property = o.GetType().ToTypeInfo().FindAttribute<ShowInstanceDetailViewAttribute>().Property;
            o = property is { } prop ? o.GetType().ToTypeInfo().FindMember(prop).GetValue(o) : o;
            var objectSpace = property==null?listView.ObjectSpace:listView.Application().CreateObjectSpace();
            return listView.Application().CreateDetailView(objectSpace, o.GetType().GetModelClass().DefaultDetailView,property != null || listView.IsRoot, objectSpace.GetObject(o));
        }
    }
}