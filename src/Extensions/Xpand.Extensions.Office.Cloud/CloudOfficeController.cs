// using DevExpress.ExpressApp;
// using DevExpress.ExpressApp.DC;
// using DevExpress.ExpressApp.Model;
// using DevExpress.Persistent.Base;
// using DevExpress.Persistent.Base.General;
// using Google.Apis.Calendar.v3;
// using Google.Apis.Tasks.v1;
// using Google.Apis.Tasks.v1.Data;
// using JetBrains.Annotations;
// using Microsoft.Graph;
// using PME_Affaire.Module.CloudOffice.Google;
// using PME_Affaire.Module.CloudOffice.Microsoft;
// using PME_Affaire.Module.PME_BUO.PME_Business.PME_Planning;
// using System;
// using System.Collections.Generic;
// using System.ComponentModel;
// using System.Configuration;
// using System.IO;
// using System.Linq;
// using System.Reactive;
// using System.Reactive.Linq;
// using System.Reactive.Subjects;
// using Xpand.Extensions.Office.Cloud;
// using Xpand.Extensions.Reactive.Filter;
// using Xpand.Extensions.Reactive.Transform;
// using Xpand.Extensions.XAF.Model;
// using Xpand.XAF.Modules.Reactive.Services;
// using Xpand.XAF.Modules.Reactive.Services.Controllers;
// using Event = Google.Apis.Calendar.v3.Data.Event;
// using CalendarExtensions = PME_Affaire.Module.CloudOffice.Google.CalendarExtensions;
// using ServiceProvider = PME_Affaire.Module.CloudOffice.Microsoft.ServiceProvider;
// using TodoExtensions = PME_Affaire.Module.CloudOffice.Google.TodoExtensions;
//
// namespace PME_Affaire.Module.CloudOffice{
//     public interface IModelOptionsCloudOffice : IModelNode{
//         IModelCloudOfficce CloudOfficce{ get; }
//     }
//
//     public interface IModelMicrosoftOffice : IModelNode{
//         [DefaultValue(PME_Affaire.Module.CloudOffice.Microsoft.CalendarExtensions.DefaultCalendarId)]
//         [Required]
//         string CalendarName{ get; [UsedImplicitly] set; }
//
//         [DefaultValue(PME_Affaire.Module.CloudOffice.Microsoft.TodoExtensions.DefaultTodoListId)]
//         [Required]
//         string TodoListName{ get; [UsedImplicitly] set; }
//
//         [DefaultValue(-12)]
//         int DeltaSnapshotStart{ get; [UsedImplicitly] set; }
//
//         [DefaultValue(60)]
//         int DeltaSnapshotEnd{ get; [UsedImplicitly] set; }
//
//         [DataSourceProperty(nameof(TimeZones))]
//         [Required]
//         [UsedImplicitly]
//         string TimeZone{ get; set; }
//
//         [Browsable(false)]
//         IEnumerable<string> TimeZones{ [UsedImplicitly] get; }
//     }
//
//     [DomainLogic(typeof(IModelMicrosoftOffice))]
//     public static class ModelMicrosoftOfficeLogic{
//         private static readonly TimeZoneInformation[] TimeZoneInformations;
//
//         static ModelMicrosoftOfficeLogic(){
//             var manifestResourceStream =
//                 typeof(ServiceProvider).Assembly.GetManifestResourceStream(
//                     $"{typeof(ServiceProvider).Namespace}.TimezoneInfo.txt");
//             var lines = new StreamReader(manifestResourceStream ?? throw new InvalidOperationException()).ReadToEnd()
//                 .Split(Environment.NewLine.ToCharArray()).Where(s => !string.IsNullOrWhiteSpace(s));
//             TimeZoneInformations = lines.Select(s => {
//                 var strings = s.Split('|');
//                 return new TimeZoneInformation(){DisplayName = strings[0], Alias = strings[1]};
//             }).ToArray();
//         }
//
//         [UsedImplicitly]
//         public static string TimeZoneAlias(this IModelMicrosoftOffice microsoftOffice) => TimeZoneInformations
//             .First(information => information.DisplayName == Get_TimeZone(microsoftOffice)).Alias;
//
//         [UsedImplicitly]
//         public static string Get_TimeZone(IModelMicrosoftOffice microsoftOffice) => TimeZoneInformations
//             .FirstOrDefault(information => information.DisplayName == TimeZoneInfo.Local.DisplayName)?.DisplayName;
//
//         [UsedImplicitly]
//         public static IEnumerable<string> Get_TimeZones(IModelMicrosoftOffice microsoftOffice) =>
//             TimeZoneInformations.Select(information => information.DisplayName);
//     }
//
//     public interface IModelGoogleOffice : IModelNode{
//         [DefaultValue(CalendarExtensions.DefaultCalendarId)]
//         [Required]
//         string CalendarName{ get; [UsedImplicitly] set; }
//
//         [DefaultValue(TodoExtensions.DefaultTodoListId)]
//         [Required]
//         string TodoListName{ get; [UsedImplicitly] set; }
//     }
//
//     public interface IModelCloudOfficce : IModelNode{
//         IModelGoogleOffice Google{ get; }
//         IModelMicrosoftOffice Microsoft{ get; }
//     }
//
//     [ModelAbstractClass]
//     public interface IModelObjectViewCloudSynch : IModelObjectView{
//         [ModelBrowsable(typeof(ModelObjectViewCloudSynchVisibilityCalculator))]
//         [UsedImplicitly]
//         bool CloudSynchEnabled{ get; set; }
//     }
//
//     public class ModelObjectViewCloudSynchVisibilityCalculator : IModelIsVisible{
//         public bool IsVisible(IModelNode node, string propertyName){
//             var modelObjectView = node.GetParent<IModelObjectView>();
//             return new[]{typeof(IEvent), typeof(ITask)}.Any(type =>
//                 type.IsAssignableFrom(modelObjectView.ModelClass.TypeInfo.Type));
//         }
//     }
//
//     [UsedImplicitly]
//     public class CloudOfficeController : ViewController<ObjectView>, IModelExtender{
//         private (IObservable<CalendarService> CalendarService, IObservable<TasksService> TaskService) _googleServices;
//
//         readonly Subject<GenericEventArgs<CloudOfficeObject>> _deleteGoogleEvent =
//             new Subject<GenericEventArgs<CloudOfficeObject>>();
//
//         readonly Subject<(Event target, IEvent source)> _updateGoogleEvent =
//             new Subject<(Event target, IEvent source)>();
//
//         readonly Subject<(Event target, IEvent source)> _insertGoogleEvent =
//             new Subject<(Event target, IEvent source)>();
//
//         readonly Subject<GenericEventArgs<CloudOfficeObject>> _deleteGoogleTask =
//             new Subject<GenericEventArgs<CloudOfficeObject>>();
//
//         readonly Subject<(Task target, ITask source)> _updateGoogleTask = new Subject<(Task target, ITask source)>();
//         readonly Subject<(Task target, ITask source)> _insertGoogleTask = new Subject<(Task target, ITask source)>();
//
//         readonly Subject<GenericEventArgs<CloudOfficeObject>> _deleteOutlookEvent =
//             new Subject<GenericEventArgs<CloudOfficeObject>>();
//
//         readonly Subject<(global::Microsoft.Graph.Event target1, IEvent source)> _updateOutlookEvent =
//             new Subject<(global::Microsoft.Graph.Event target1, IEvent source)>();
//
//         readonly Subject<(global::Microsoft.Graph.Event target1, IEvent source)> _insertOutlookEvent =
//             new Subject<(global::Microsoft.Graph.Event, IEvent)>();
//
//         readonly Subject<GenericEventArgs<CloudOfficeObject>> _deleteOutlookTask =
//             new Subject<GenericEventArgs<CloudOfficeObject>>();
//
//         readonly Subject<(OutlookTask target, ITask source)> _updateOutlookTask =
//             new Subject<(OutlookTask target, ITask source)>();
//
//         readonly Subject<(OutlookTask target, ITask source)> _insertOutlookTask =
//             new Subject<(OutlookTask target, ITask source)>();
//
//         private IObservable<GraphServiceClient> _msService;
//
//         [PublicAPI]
//         public IObservable<GenericEventArgs<CloudOfficeObject>> DeleteOutlookEvent =>
//             _deleteOutlookEvent.AsObservable();
//
//         [PublicAPI]
//         public IObservable<(global::Microsoft.Graph.Event target, IEvent source)> UpdateOutlookEvent =>
//             _updateOutlookEvent.AsObservable();
//
//         [PublicAPI]
//         public IObservable<(global::Microsoft.Graph.Event target, IEvent source)> InsertOutlookEvent =>
//             _insertOutlookEvent.AsObservable();
//
//         [PublicAPI]
//         public IObservable<GenericEventArgs<CloudOfficeObject>> DeleteOutlookTask => _deleteOutlookTask.AsObservable();
//
//         [PublicAPI]
//         public IObservable<(OutlookTask target, ITask source)> UpdateOutlookTask => _updateOutlookTask.AsObservable();
//
//         [PublicAPI]
//         public IObservable<(OutlookTask target, ITask source)> InsertOutlookTask => _insertOutlookTask.AsObservable();
//
//         [PublicAPI]
//         public IObservable<GenericEventArgs<CloudOfficeObject>> DeleteTask => _deleteGoogleTask.AsObservable();
//
//         [PublicAPI]
//         public IObservable<(Task target, ITask source)> UpdateGoogleTask => _updateGoogleTask.AsObservable();
//
//         [PublicAPI]
//         public IObservable<(Task target, ITask source)> InsertGoogleTask => _insertGoogleTask.AsObservable();
//
//         [PublicAPI]
//         public IObservable<(Event target, IEvent source)> UpdateGoogleEvent => _updateGoogleEvent.AsObservable();
//
//         [PublicAPI]
//         public IObservable<(Event target, IEvent source)> InsertGoogleEvent => _insertGoogleEvent.AsObservable();
//
//         [PublicAPI]
//         public IObservable<GenericEventArgs<CloudOfficeObject>> DeleteGoogleEvent => _deleteGoogleEvent.AsObservable();
//
//         public void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
//             extenders.Add<IModelObjectView, IModelObjectViewCloudSynch>();
//             extenders.Add<IModelOptions, IModelOptionsCloudOffice>();
//         }
//
//         protected override void OnActivated(){
//             base.OnActivated();
//             if (((IModelObjectViewCloudSynch) View.Model).CloudSynchEnabled){
//                 SynchronizeGoogle()
//                     .Merge(SynchronizeMicrosoft())
//                     .TakeUntil(this.WhenDeactivated())
//                     .SubscribeSafe();
//             }
//         }
//
//         private IObservable<Unit> SynchronizeMicrosoft(){
//             var modelCloudOfficce = ((IModelOptionsCloudOffice) Application.Model.Options).CloudOfficce.Microsoft;
//
//             var mapOutlookTasks = typeof(ITask).IsAssignableFrom(View.ObjectTypeInfo.Type)
//                 ? _msService.Select(client => client.Me.Outlook.TaskFolders[modelCloudOfficce.TodoListName])
//                     .SynchronizeCloud(ObjectSpace, Application.CreateObjectSpace, _deleteOutlookTask.OnNext,
//                         _insertOutlookTask.OnNext, _updateOutlookTask.OnNext).ToUnit()
//                 : Observable.Empty<Unit>();
//             var mapOutlookEvents = typeof(IEvent).IsAssignableFrom(View.ObjectTypeInfo.Type)
//                 ? _msService.Select(client => client.Me.Calendars[modelCloudOfficce.CalendarName])
//                     .SynchronizeCloud(ObjectSpace, Application.CreateObjectSpace, _deleteOutlookEvent.OnNext,
//                         _insertOutlookEvent.OnNext, _updateOutlookEvent.OnNext).ToUnit()
//                 : Observable.Empty<Unit>();
//             var timeZone = PME_Affaire.Module.CloudOffice.Microsoft.CalendarExtensions.CustomizeCloudSynchronization.Do(
//                 _ => {
//                     _.target.Start.TimeZone = modelCloudOfficce.TimeZoneAlias();
//                     _.target.End.TimeZone = modelCloudOfficce.TimeZoneAlias();
//                 }).ToUnit();
//             return mapOutlookEvents.Merge(mapOutlookTasks).Merge(timeZone);
//         }
//
//         IObservable<Unit> SynchronizeGoogle(){
//             var modelCloudOfficce = ((IModelOptionsCloudOffice) Application.Model.Options).CloudOfficce.Google;
//             var mapGoogleEvents = typeof(IEvent).IsAssignableFrom(View.ObjectTypeInfo.Type)
//                 ? _googleServices.CalendarService
//                     .SynchronizeCloud(ObjectSpace, Application.CreateObjectSpace, modelCloudOfficce.CalendarName,
//                         _deleteGoogleEvent.OnNext, _updateGoogleEvent.OnNext, _insertGoogleEvent.OnNext).ToUnit()
//                 : Observable.Empty<Unit>();
//             var mapGoogleTasks = typeof(ITask).IsAssignableFrom(View.ObjectTypeInfo.Type)
//                 ? _googleServices.TaskService
//                     .SynchronizeCloud(ObjectSpace, Application.CreateObjectSpace, modelCloudOfficce.TodoListName,
//                         _deleteGoogleTask.OnNext, _insertGoogleTask.OnNext, _updateGoogleTask.OnNext).ToUnit()
//                 : Observable.Empty<Unit>();
//             var synchronizeGoogleResources = _googleServices.CalendarService
//                 .SynchronizeLocal<PME_Event>(Application.CreateObjectSpace, (Guid) SecuritySystem.CurrentUserId)
//                 .ToUnit();
//             return mapGoogleEvents.Merge(mapGoogleTasks).Merge(synchronizeGoogleResources);
//         }
//
//         protected override void OnFrameAssigned(){
//             base.OnFrameAssigned();
//             if (Frame.Context == TemplateContext.ApplicationWindow){
//                 _msService = CreateMSService();
//                 _googleServices = CreateGoogleServices();
//                 var webHookAddress = ConfigurationManager.AppSettings["WebHookAddress"];
//                 Application.SynchronizeCloudSubscriptions(_googleServices, webHookAddress,
//                         CalendarExtensions.DefaultCalendarId)
//                     .Merge(WatchCloudEvents(webHookAddress))
//                     .SubscribeSafe();
//             }
//         }
//
//         private IObservable<GraphServiceClient> CreateMSService(){
//             var modelCloudOfficce = ((IModelOptionsCloudOffice) Application.Model.Options).CloudOfficce.Microsoft;
//             PME_Affaire.Module.CloudOffice.Microsoft.CalendarExtensions.DeltaSnapShotStartDateTime =
//                 DateTime.Now.AddMonths(modelCloudOfficce.DeltaSnapshotStart);
//             PME_Affaire.Module.CloudOffice.Microsoft.CalendarExtensions.DeltaSnapShotEndDateTime =
//                 DateTime.Now.AddMonths(modelCloudOfficce.DeltaSnapshotEnd);
//             var connection = ServiceProvider.ClientAppBuilder.Authorize(cache =>
//                     cache.SynchStorage(Application.CreateObjectSpace, (Guid) SecuritySystem.CurrentUserId))
//                 .Select(client => client)
//                 .SelectMany(client => client.Me.Outlook.TaskFolders.GetFolder(modelCloudOfficce.TodoListName, true)
//                     .Select(folder => client))
//                 .SelectMany(client => client.Me.Calendars.GetCalendar(modelCloudOfficce.CalendarName, true)
//                     .Select(folder => client))
//                 .Publish().RefCount()
//                 .HandleErrors()
//                 .Replay(1);
//             connection.Connect();
//             return connection;
//         }
//
//         private IObservable<Unit> WatchCloudEvents(string webHookAddress) => Frame.WhenViewChanged().FirstAsync()
//             .SelectMany(_ => _googleServices.CalendarService.WatchCloudEvents(Application.CreateObjectSpace,
//                 (Guid) SecuritySystem.CurrentUserId, webHookAddress, CalendarExtensions.DefaultCalendarId));
//
//         private (IObservable<CalendarService> CalendarService, IObservable<TasksService> TaskService)
//             CreateGoogleServices(){
//             var modelCloudOfficce = ((IModelOptionsCloudOffice) Application.Model.Options).CloudOfficce.Google;
//             var credentials = Application.Authorize().Select(credential => credential)
//                 .FirstOrDefaultAsync().WhenNotDefault().Replay(1).RefCount();
//
//
//             var taskService = credentials.NewService<TasksService>()
//                 .SelectMany(service => service.GetTaskList(modelCloudOfficce.TodoListName, true).Select(_ => service))
//                 .Publish().RefCount()
//                 .HandleErrors()
//                 .Replay(1);
//             taskService.Connect();
//
//             var calendarService = credentials.NewService<CalendarService>()
//                 .SelectMany(service => service.GetCalendar(modelCloudOfficce.CalendarName, true).Select(_ => service))
//                 .Publish().RefCount()
//                 .HandleErrors()
//                 .Replay(1);
//             calendarService.Connect();
//             return (calendarService, taskService);
//         }
//     }
// }