using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Microsoft.Graph;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.Office.Cloud.Microsoft;
using Xpand.XAF.Modules.Office.Cloud.Microsoft.Calendar;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Event = DevExpress.Persistent.BaseImpl.Event;

namespace TestApplication.MicrosoftCalendarService{
    public static class MicrosoftCalendarService{
        public static IObservable<Unit> ConnectMicrosoftCalendarService(this ApplicationModulesManager manager) =>
            manager.InitializeModule().Merge(manager.WhenApplication(UpdateCalendarDescription).ToUnit());

        private static IObservable<(Microsoft.Graph.Event serviceObject, MapAction mapAction, CallDirection callDirection)> UpdateCalendarDescription(this XafApplication application) =>
            CalendarService.Updated
                .Do(tuple => {
                    // using (var objectSpace = application.CreateObjectSpace()){
                    //     var cloudOfficeObject = objectSpace.QueryCloudOfficeObject(tuple.serviceObject.Id, CloudObjectType.Event).First();
                    //     var @event = objectSpace.GetObjectByKey<Event>(Guid.Parse(cloudOfficeObject.LocalId));
                    //     @event.Description = tuple.mapAction.ToString();
                    //     objectSpace.CommitChanges();
                    // }
                });

        private static IObservable<Unit> InitializeModule(this ApplicationModulesManager manager){
            manager.Modules.OfType<AgnosticModule>().First().AdditionalExportedTypes.Add(typeof(Event));
            return manager.WhenCustomizeTypesInfo().Do(_ => _.e.TypesInfo.FindTypeInfo(typeof(Event)).AddAttribute(new DefaultClassOptionsAttribute())).ToUnit()
                .FirstAsync()
                .Concat(manager.WhenGeneratingModelNodes(application => application.Views)
                    .Do(views => {
                        var modelCalendar = views.Application.ToReactiveModule<IModelReactiveModuleOffice>().Office.Microsoft().Calendar();
                        modelCalendar.DefaultCaledarName = "TestApplication";
                        var objectViewDependency = modelCalendar.ObjectViews().AddNode<IModelObjectViewDependency>();
                        objectViewDependency.ObjectView=views.Application.BOModel.GetClass(typeof(Event)).DefaultDetailView;
                        objectViewDependency = modelCalendar.ObjectViews().AddNode<IModelObjectViewDependency>();
                        objectViewDependency.ObjectView=views.Application.BOModel.GetClass(typeof(Event)).DefaultListView;
                    }).ToUnit());
        }
    }
}