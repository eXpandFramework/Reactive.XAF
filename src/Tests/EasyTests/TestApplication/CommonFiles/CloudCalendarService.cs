using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.Persistent.Base.General;
using DevExpress.Persistent.BaseImpl;
using Fasterflect;
using TestApplication;
using TestApplication.Module;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace ALL.Tests{
    public static class CloudCalendarService{
        public static SingleChoiceAction CloudCalendarOperation(this (TestApplicationModule module, Frame frame) tuple) 
            => tuple.frame.Action(nameof(CloudCalendarOperation)).As<SingleChoiceAction>();

        public static IObservable<Unit> ConnectCloudCalendarService(this ApplicationModulesManager manager) 
	        => manager.RegisterCloudCalendarOperation();

        public static IObservable<Unit> ConnectCloudCalendarService<TCloud>(this ApplicationModulesManager manager,
            Func<(IObservable<(TCloud cloud,IEvent local, MapAction mapAction, CallDirection callDirection)> updated,
                IObservable<IObservable<Unit>> deleteAll, IObservable<Unit> initializeModule)> config) 
            => config().initializeModule
                .Merge(manager.WhenApplication(application
                    => application.WhenViewOnFrame(typeof(Event),ViewType.DetailView)
                        .SelectMany(frame => frame.View.ObjectSpace.WhenCommiting().SelectMany(t => config().updated.TakeUntil(frame.WhenDisposingFrame()))
                            .Do(tuple => {
                                using (var objectSpace = frame.Application.CreateObjectSpace()) {
                                    var cloudOfficeObject = objectSpace.QueryCloudOfficeObject(tuple.cloud.GetPropertyValue("Id").ToString(), CloudObjectType.Event).First();
                                    var @event = objectSpace.GetObjectByKey<Event>(Guid.Parse(cloudOfficeObject.LocalId));
                                    @event.Description = tuple.mapAction.ToString();
                                    objectSpace.CommitChanges();
                                }
                            }))
                        .ToUnit()
                        .Merge(application.DeleteAllEntities<Task>(config().deleteAll))).ToUnit());

        private static IObservable<Unit> RegisterCloudCalendarOperation(this ApplicationModulesManager manager) 
            => manager.RegisterViewSingleChoiceAction(nameof(CloudCalendarOperation), action => {
                action.TargetObjectType = typeof(Event);
                action.ItemType=SingleChoiceActionItemType.ItemIsOperation;
                action.Items.Add(new ChoiceActionItem("New", "New"));
                action.Items.Add(new ChoiceActionItem("Update", "Update"));
                action.Items.Add(new ChoiceActionItem("Delete", "Delete"));
            }).ToUnit();


        public static IObservable<Unit> InitializeCloudCalendarModule(this ApplicationModulesManager manager,
            Func<IModelOffice, IModelCalendar> modelCalendarSelector, string serviceName){
            manager.Modules.OfType<TestApplicationModule>().First().AdditionalExportedTypes.Add(typeof(Event));
            return manager.WhenGeneratingModelNodes(application => application.Views)
                .Do(views => {
                    var modelCalendar = modelCalendarSelector(views.Application.ToReactiveModule<IModelReactiveModuleOffice>().Office);
                    var calendarItem = modelCalendar.Items.AddNode<IModelCalendarItem>();
                    calendarItem.ObjectView=(IModelObjectView) views.Application.Views[$"Event{serviceName}_DetailView"];
                    calendarItem.CallDirection=CallDirection.Out;
                    ((ModelNode) calendarItem).Id=$"{calendarItem.ObjectView.Id}-{calendarItem.SynchronizationType}-{calendarItem.CallDirection}";
                    calendarItem = modelCalendar.Items.AddNode<IModelCalendarItem>();
                    calendarItem.ObjectView=(IModelObjectView) views.Application.Views[$"Event{serviceName}_ListView"];
                    ((ModelNode) calendarItem).Id=$"{calendarItem.ObjectView.Id}-{calendarItem.SynchronizationType}-{calendarItem.CallDirection}";
                }).ToUnit();
        }

        public static IObservable<Unit> ExecuteCalendarCloudOperation<TAuthorize>(this XafApplication application, Type cloudEntityType,
            Func<TAuthorize> authorize, Func<TAuthorize, IObservable<object>> newOperation,
            Func<TAuthorize, CloudOfficeObject, IObservable<object>> update, Func<TAuthorize, CloudOfficeObject, IObservable<Unit>> delete, string serviceName) 
            => application.WhenViewOnFrame(typeof(Event),ViewType.ListView)
                .Where(frame => frame.View.Id.Contains(serviceName))
                .Select(frame => frame.Action<TestApplicationModule>().CloudCalendarOperation().WhenExecute()).Switch()
                .SelectMany(e => {
                    var authorizeService = authorize();
                    if (e.SelectedChoiceActionItem.Caption == "New"){
                        return newOperation(authorizeService).ObserveOn(SynchronizationContext.Current).ToUnit();
                    }
                    var objectSpace = e.Action.Application.CreateObjectSpace();
                    var cloudOfficeObject = objectSpace.QueryCloudOfficeObject(cloudEntityType,objectSpace.GetObjects<Event>().First()).First();
                    return e.SelectedChoiceActionItem.Caption == "Update"
                        ? update(authorizeService,cloudOfficeObject).ObserveOn(SynchronizationContext.Current).ToUnit()
                        : delete(authorizeService,cloudOfficeObject).ObserveOn(SynchronizationContext.Current);
                });

    }

}