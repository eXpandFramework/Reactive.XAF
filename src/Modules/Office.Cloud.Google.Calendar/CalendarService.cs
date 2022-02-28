using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base.General;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3.Data;
using JetBrains.Annotations;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Office.Cloud.BusinessObjects;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Office.Cloud.Google.Calendar{
    public static class CalendarService{
        private static readonly ISubject<(Frame frame, UserCredential userCredential)> CredentialsSubject=new Subject<(Frame frame, UserCredential client)>();
        public static IObservable<(Frame frame, UserCredential credential)> Credentials => CredentialsSubject.AsObservable();
        public const string DefaultCalendarId = "primary";
        static readonly Subject<(Event cloud,IEvent local, MapAction mapAction,CallDirection callDirection)> UpdatedSubject=new();
        public static IObservable<(Event cloud, IEvent local, MapAction mapAction, CallDirection callDirection)> Updated{ get; }=UpdatedSubject.AsObservable();
        static readonly Subject<GenericEventArgs<(Func<IObjectSpace> objectSpace, IEvent local, Event cloud, MapAction mapAction, CallDirection callDirection)>> CustomizeSynchronizationSubject =
            new Subject<GenericEventArgs<(Func<IObjectSpace> objectSpace, IEvent target, Event source, MapAction mapAction, CallDirection callDirection)>>();
        
        public static IObservable<GenericEventArgs<(Func<IObjectSpace> objectSpace, IEvent local, Event cloud, MapAction mapAction, CallDirection callDirection)>> CustomizeSynchronization 
            => CustomizeSynchronizationSubject.AsObservable();
        
        public static IObservable<(Event cloud, IEvent local, MapAction mapAction, CallDirection callDirection)> When(
            this IObservable<(Event cloud,IEvent local, MapAction mapAction,CallDirection callDirection)> source,
            MapAction mapAction, CallDirection callDirection = CallDirection.Both)
            => source.Where(_ => _.mapAction == mapAction&& (callDirection == CallDirection.Both || _.callDirection == callDirection));
        
        public static IObservable<GenericEventArgs<(Func<IObjectSpace> objectSpaceFactory, IEvent local, Event cloud, MapAction mapAction, CallDirection callDirection)>> When(
            this IObservable<GenericEventArgs<(Func<IObjectSpace> objectSpaceFactory, IEvent local, Event cloud, MapAction mapAction, CallDirection callDirection)>> source,
            MapAction mapAction, CallDirection callDirection = CallDirection.Both)
            => source.Where(_ => _.Instance.mapAction == mapAction&& (callDirection == CallDirection.Both || _.Instance.callDirection == callDirection));

        internal static IObservable<TSource> TraceGoogleCalendarModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, GoogleCalendarModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);

        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) 
	        => manager.WhenApplication(application => {
                var viewOnFrame = application.WhenViewOnFrame()
	                .When(_ => application.Model.ToReactiveModule<IModelReactiveModuleOffice>().Office
		                .Google().Calendar().Items.Select(item => item.ObjectView))
	                .Publish().RefCount();
                return viewOnFrame.Authorize()
                    .SynchronizeBoth()
                    .Merge(viewOnFrame.SelectMany(frame=>frame.GetController<RefreshController>().RefreshAction.WhenExecute()
                        .Select(_=>frame).TakeUntil(frame.View.WhenClosing())
                        .Authorize().SynchronizeLocal()
                        .ObserveOnWindows(SynchronizationContext.Current)
                        .Select(tuple => {
                            tuple.frame.View.ObjectSpace.Refresh();
                            return default((Event serviceObject, MapAction mapAction));
                        })))
                    .ToUnit();
            })
            .Merge(manager.ConfigureModel());

        private static IObservable<(Frame frame, UserCredential credential, CalendarListEntry calendarListEntry)> EnsureCalendar(this IObservable<(Frame frame, UserCredential credential)> source) 
            => source.Select(_ => {
                    var defaultCalendarName = _.frame.View.AsObjectView().Application().Model
                        .ToReactiveModule<IModelReactiveModuleOffice>().Office.Google().Calendar().DefaultCalendarName;
                    return Observable.Start(() => _.credential.NewService<global::Google.Apis.Calendar.v3.CalendarService>()
                            .GetCalendar(defaultCalendarName, true,defaultCalendarName==DefaultCalendarId)).Merge().Wait().ReturnObservable()
                        .Select(calendarListEntry => (_.frame, _.credential, calendarListEntry));
                })
                .Merge()
                .TraceGoogleCalendarModule(t => t.calendarListEntry.Summary);

        private static IObservable<(Frame frame, UserCredential userCredential, (IEvent target, Event source) calendar)> SynchronizeLocal(
            this IObservable<(Frame frame, UserCredential userCredential, CalendarListEntry calendar, IModelCalendarItem calerdarItem)> source)  
            => source
                .Where(t => t.calerdarItem.CallDirection!=CallDirection.Out)
                .SelectMany(_ => _.frame.Application
                .SelectMany(() => _.SynchronizeLocal(_.calerdarItem.SynchronizationType))
                .Select(calendar => (_.frame,_.userCredential,calendar))
                .TraceGoogleCalendarModule(folder => $"Event: {folder.calendar.source?.Id}"));

        private static IObservable<(IEvent target, Event source)> SynchronizeLocal(
            this (Frame frame, UserCredential userCredential, CalendarListEntry calendar, IModelCalendarItem calerdarItem) _, SynchronizationType synchronizationType){
            var newCloudEventType = _.frame.View.Model.Application.Calendar().NewCloudEvent.TypeInfo.Type;
            Func<IObjectSpace> objectSpaceFactory = _.frame.Application.CreateObjectSpace;
            
            return _.userCredential.NewService<global::Google.Apis.Calendar.v3.CalendarService>().ReturnObservable()
                .SynchronizeLocalEvent(objectSpaceFactory,synchronizationType,
                    Guid.Parse($"{_.frame.Application.Security.UserId}"), _.frame.View.ObjectTypeInfo.Type, newCloudEventType,_.calendar);
        }

        static IObservable<(Frame frame, UserCredential credential, CalendarListEntry calendarListEntry, IModelCalendarItem modelCalendarItem)> Authorize(this IObservable<Frame> source) 
            => source.AuthorizeGoogle()
                .EnsureCalendar()
                .Publish().RefCount()
                .WhenNotDefault(t => t.frame.Application)
                .Do(tuple => CredentialsSubject.OnNext((tuple.frame,tuple.credential)))
                .SelectMany(t => t.frame.Application.Model
                    .ToReactiveModule<IModelReactiveModuleOffice>().Office
                    .Google().Calendar().Items.Where(item => item.ObjectView == t.frame.View.Model)
                    .Select(item => (t.frame, t.credential, t.calendarListEntry, item)))
                .TraceGoogleCalendarModule(_ => _.frame.View.Id);

        private static IObservable<(Event serviceObject, MapAction mapAction)> SynchronizeBoth(
            this IObservable<(Frame frame, UserCredential credential, CalendarListEntry calendarListEntry, IModelCalendarItem modelCalendarItem)> source) 
            => source.SynchronizeCloud()
                .Merge(source.SynchronizeLocal().Select(_ => default((Event serviceObject, MapAction mapAction))).IgnoreElements());

        private static IObservable<(Event serviceObject, MapAction mapAction)> SynchronizeCloud(
            this IObservable<(Frame frame, UserCredential credential, CalendarListEntry
                calendar, IModelCalendarItem modelCalendarItem)> source) 
            => source.Select(t => t.credential.NewService<global::Google.Apis.Calendar.v3.CalendarService>().ReturnObservable().SynchronizeCloud(
                        t.modelCalendarItem.SynchronizationType, t.frame.View.ObjectSpace, t.frame.View.AsObjectView().Application().CreateObjectSpace,t.calendar)
                    .TakeUntil(t.frame.View.WhenClosing())
                ).Switch()
                .Do(tuple => UpdatedSubject.OnNext((tuple.serviceObject,null,tuple.mapAction,CallDirection.Out)))
                .TraceGoogleCalendarModule(_ => $"{_.mapAction} {_.serviceObject.Description}, {_.serviceObject.Status}, {_.serviceObject.Id}");

        private static IObservable<(Event serviceObject, MapAction mapAction)> SynchronizeCloud(this IObservable<global::Google.Apis.Calendar.v3.CalendarService> source,
            SynchronizationType synchronizationType, IObjectSpace objectSpace, Func<IObjectSpace> objectSpaceFactory,CalendarListEntry calendar) 
            => source.SelectMany(service => objectSpaceFactory.SynchronizeCloud<Event, IEvent>(synchronizationType,objectSpace,
                cloudId => RequestCustomization.Default(service.Events.Delete(calendar.Id, cloudId)).ToObservable<string>().ToUnit(),
                cloudEvent => RequestCustomization.Default(service.Events.Insert(cloudEvent, calendar.Id)).ToObservable<Event>(),
                t => RequestCustomization.Default(service.Events.Get(calendar.Id, t.cloudId)).ToObservable<Event>().Where(e => e.Status!="cancelled"),
                t => RequestCustomization.Default(service.Events.Update(t.cloudEntity, calendar.Id, t.cloudId)).ToObservable<Event>(),
                e => e.Handled=MapAction.Delete.CustomSynchronization(objectSpaceFactory, e.Instance.localEntinty, null, CallDirection.Out,synchronizationType,e.Instance.cloudOfficeObject).Handled,
                t => MapAction.Insert.CustomSynchronization(objectSpaceFactory, t.source, t.target, CallDirection.Out,synchronizationType),
                t => MapAction.Update.CustomSynchronization(objectSpaceFactory, t.source, t.target, CallDirection.Out,synchronizationType)));

        private static GenericEventArgs<(Func<IObjectSpace> objectSpace, IEvent local, Event cloud, MapAction mapAction,
                CallDirection callDirection)> CustomSynchronization(this MapAction mapAction, Func<IObjectSpace> objectSpaceFactory,
                IEvent target, Event source, CallDirection callDirection, SynchronizationType synchronizationType, CloudOfficeObject cloudOfficeObject = null){
            var e = new GenericEventArgs<(Func<IObjectSpace> objectSpace, IEvent local, Event cloud, MapAction mapAction,
                CallDirection callDirection)>((objectSpaceFactory, target, source, mapAction, callDirection));
            
            CustomizeSynchronizationSubject.OnNext(e);
            if (!e.Handled && e.Instance.callDirection == CallDirection.Out){
                var cloud = e.Instance.cloud;
                var local = e.Instance.local;
                if (e.Instance.mapAction!=MapAction.Delete&& (synchronizationType.IsCreate() || synchronizationType.IsUpdate())){
                    local.MapCloudEvent(cloud);
                }
                else{
                    using var objectSpace = objectSpaceFactory();
                    objectSpace.Delete(objectSpace.GetObject(cloudOfficeObject));
                    objectSpace.CommitChanges();
                }
            }
            return e;
        }

        public static void MapCloudEvent(this IEvent fromLocalEvent,Event toCloudEvent){
            toCloudEvent.Location = fromLocalEvent.Location;
            toCloudEvent.Start = new EventDateTime{
                DateTime = fromLocalEvent.StartOn,
            };
            toCloudEvent.End = new EventDateTime{
                DateTime = fromLocalEvent.EndOn,
            };
            toCloudEvent.Description = fromLocalEvent.Description;
            toCloudEvent.Summary = fromLocalEvent.Subject;
        }

        private static IObservable<(IEvent target, Event source)> SynchronizeLocalEvent(this IObservable<global::Google.Apis.Calendar.v3.CalendarService> source,
            Func<IObjectSpace> objectSpaceFactory, SynchronizationType synchronizationType, Guid currentUserId,
            Type eventType, Type newCloudEventType, CalendarListEntry calendarListEntry) 
            => source.SynchronizeLocalEvent(objectSpaceFactory, currentUserId, (service, store) 
                    => service.ListEvents( store,objectSpaceFactory,calendarListEntry.Id), eventType,"synctoken" )
                .Select(tuple => {
                    var args = new GenericEventArgs<(Func<IObjectSpace> objectSpace, IEvent local, Event cloud, MapAction
                            mapAction, CallDirection callDirection)>((objectSpaceFactory, tuple.local, tuple.cloud, tuple.mapAction, CallDirection.In));
                    CustomizeSynchronizationSubject.OnNext(args);
                    var localEvent=args.Instance.local;
                    if (!args.Handled){
                        using (var objectSpace = args.Instance.objectSpace()){
                            if (args.Instance.mapAction == MapAction.Delete&&synchronizationType.IsDelete()){
                                objectSpace.Delete(objectSpace.GetObject(tuple.local));
                            }
                            else{
                                if (args.Instance.mapAction == MapAction.Insert&&synchronizationType.IsCreate()){
                                    localEvent = (IEvent)objectSpace.CreateObject(newCloudEventType);
                                    objectSpace.CommitChanges();
                                    objectSpace.NewCloudObject(localEvent, args.Instance.cloud).Wait();
                                    localEvent.Update( args);
                                }
                                else if (args.Instance.mapAction == MapAction.Update&&synchronizationType.IsUpdate()){
                                    localEvent.Update( args);
                                }
                            }
                            objectSpace.CommitChanges();
                        }
                        UpdatedSubject.OnNext((args.Instance.cloud,args.Instance.local,args.Instance.mapAction,CallDirection.In));
                    }
                    return (localEvent, cloudEvent: tuple.cloud);
                })
                .TraceGoogleCalendarModule(tuple => $"cloud: {tuple.cloudEvent.Id}, local:{tuple.localEvent?.Subject}");

        public static IObservable<(Event e, MapAction mapAction)> PairMapAction(this IObservable<Event> source, Func<IObjectSpace> objectSpaceFactory) 
            => source.Select(e => {
                using var objectSpace = objectSpaceFactory();
                var mapAction = objectSpace.QueryCloudOfficeObject(e.Id, e.GetType().ToCloudObjectType()).Any()?MapAction.Update:MapAction.Insert;
                if (e.Status == "cancelled"){
                    mapAction=MapAction.Delete;
                }
                return (e, mapAction);
            });

        private static void Update(this IEvent localEvent, GenericEventArgs<(Func<IObjectSpace> objectSpace, IEvent local, Event cloud, MapAction mapAction, CallDirection callDirection)> args){
            localEvent.Location = args.Instance.cloud.Location;
            localEvent.StartOn = Convert.ToDateTime(args.Instance.cloud.Start?.DateTime);
            localEvent.EndOn = Convert.ToDateTime(args.Instance.cloud.End?.DateTime);
            localEvent.Subject = args.Instance.cloud.Summary;
            localEvent.Description = args.Instance.cloud.Description;
        }

        public static IObservable<CalendarListEntry> GetCalendar(this global::Google.Apis.Calendar.v3.CalendarService calendarService, string summary=null, bool createNew = false,bool returnDefault=false){
            if (returnDefault){
                return calendarService.CalendarList.Get("primary").ToObservable();
            }
            var addNew = createNew.ReturnObservable().WhenNotDefault()
                .SelectMany(_ => calendarService.Calendars.Insert(new global::Google.Apis.Calendar.v3.Data.Calendar() { Summary = summary}).ToObservable())
                .SelectMany(_ => calendarService.GetCalendar(summary));
            return calendarService.CalendarList.List().ToObservable().SelectMany(list => list.Items)
                .FirstOrDefaultAsync(entry => entry.Summary == summary).SwitchIfDefault(addNew);
        }


        private static IObservable<Unit> ConfigureModel(this ApplicationModulesManager manager) 
            => manager.WhenGeneratingModelNodes(modelApplication => modelApplication.BOModel)
                .Do(model => model.Application.OAuthGoogle()
                    .AddScopes(global::Google.Apis.Calendar.v3.CalendarService.Scope.CalendarEvents,global::Google.Apis.Calendar.v3.CalendarService.Scope.Calendar)).ToUnit();

        public static IObservable<(Event e, MapAction mapAction)> ListEvents(this global::Google.Apis.Calendar.v3.CalendarService service, ICloudOfficeToken store,
            Func<IObjectSpace> objectSpaceFactory,string calendarId) 
            => service.ListEvents(calendarId, store,tokenStore => tokenStore?.SaveToken(objectSpaceFactory)).SelectMany(events => events).SelectMany(events => events.Items)
                .PairMapAction(objectSpaceFactory);

        public static IObservable<Events[]> ListEvents(this global::Google.Apis.Calendar.v3.CalendarService calendarService,string calendarId, ICloudOfficeToken cloudOfficeToken = null,
            Action<ICloudOfficeToken> @finally = null,  int? maxResults = 2500, Func<GoogleApiException, bool> repeat = null) 
            => maxResults < 250
                ? throw new ArgumentOutOfRangeException($"{nameof(maxResults)} is less than 250")
                : calendarService.Events.List(calendarId).List(maxResults, cloudOfficeToken, @finally, repeat)
                    .Select(events => events.Select(t => {
                        t.Items ??= new List<Event>();
                        return t;
                    }).ToArray());

        public static IObservable<string> DeleteAllEvents(this global::Google.Apis.Calendar.v3.CalendarService calendarService, string calendarId) 
            => calendarService.ListEvents(calendarId)
                .SelectMany(events => events)
                .Where(events => events.Items != null)
                .SelectMany(events => events.Items)
                .SelectMany(_ => Observable.FromAsync(() => calendarService.Events.Delete(calendarId, _.Id).ExecuteAsync()))
                .LastOrDefaultAsync();

        

    }
}