using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base.General;

using Microsoft.Graph;
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
using CallDirection = Xpand.Extensions.Office.Cloud.CallDirection;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft.Calendar{
    public static class CalendarService{
        private static readonly ISubject<(Frame frame, GraphServiceClient client)> ClientSubject=new Subject<(Frame frame, GraphServiceClient client)>();
        public static IObservable<(Frame frame, GraphServiceClient client)> Client => ClientSubject.AsObservable();
        private static IUserRequestBuilder Me(this IBaseRequestBuilder builder) => builder.Client.Me();
        public static IUserRequestBuilder Me(this IBaseClient client) => new UserRequestBuilder(((GraphServiceClient)client).BaseUrl + "/me", client);
        static readonly Subject<(Event cloud,IEvent local, MapAction mapAction,CallDirection callDirection)> UpdatedSubject=new();
        static readonly Subject<GenericEventArgs<(Func<IObjectSpace> objectSpace, IEvent local, Event cloud, MapAction mapAction, CallDirection callDirection)>> CustomizeSynchronizationSubject =
            new Subject<GenericEventArgs<(Func<IObjectSpace> objectSpace, IEvent target, Event source, MapAction mapAction, CallDirection callDirection)>>();
        
        
        public static IObservable<GenericEventArgs<(Func<IObjectSpace> objectSpaceFactory, IEvent local, Event cloud, MapAction mapAction, CallDirection callDirection)>> CustomizeSynchronization 
            => CustomizeSynchronizationSubject.AsObservable();
        public static IObservable<(Event cloud,IEvent local, MapAction mapAction,CallDirection callDirection)> Updated{ get; }=UpdatedSubject.AsObservable();
        
        public static DateTime DeltaSnapShotStartDateTime { get; set; } = DateTime.Now.AddMonths(-12);
        
        public static DateTime DeltaSnapShotEndDateTime { get; set; } = DateTime.Now.AddMonths(12 * 5);
        
        
        public static IObservable<(Event cloud, IEvent local, MapAction mapAction, CallDirection callDirection)> When(
            this IObservable<(Event cloud,IEvent local, MapAction mapAction,CallDirection callDirection)> source,
            MapAction mapAction, CallDirection callDirection = CallDirection.Both)
            => source.Where(_ => _.mapAction == mapAction&& (callDirection == CallDirection.Both || _.callDirection == callDirection));

        
        public static IObservable<GenericEventArgs<(Func<IObjectSpace> objectSpaceFactory, IEvent local, Event cloud, MapAction mapAction, CallDirection callDirection)>> When(
            this IObservable<GenericEventArgs<(Func<IObjectSpace> objectSpaceFactory, IEvent local, Event cloud, MapAction mapAction, CallDirection callDirection)>> source,
            MapAction mapAction, CallDirection callDirection = CallDirection.Both)
            => source.Where(_ => _.Instance.mapAction == mapAction&& (callDirection == CallDirection.Both || _.Instance.callDirection == callDirection));

        internal static IObservable<TSource> TraceMicrosoftCalendarModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, MicrosoftCalendarModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);
        
        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) =>
            manager.WhenApplication(application => {
                var viewOnFrame = application.WhenViewOnFrame()
                    .When(_ => application.Model.ToReactiveModule<IModelReactiveModuleOffice>().Office
                        .Microsoft().Calendar().Items.Select(item => item.ObjectView))
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
            });

        private static IObservable<(Frame frame, GraphServiceClient client, global::Microsoft.Graph.Calendar calendar)> EnsureCalendar(this IObservable<(Frame frame, GraphServiceClient client)> source) 
            => source.Select(_ => _.frame.Application.SelectMany(() => _.client.Me().Calendars
                        .GetCalendar(_.frame.View.AsObjectView().Application().Model.ToReactiveModule<IModelReactiveModuleOffice>().Office.Microsoft().Calendar().DefaultCalendarName, true))
                    .Select(calendar => (_.frame,_.client,calendar))
                ).Merge()
                .TraceMicrosoftCalendarModule(tuple => tuple.calendar.Name)
                .Select(tuple => (tuple.frame,tuple.client,tuple.calendar));

        private static IObservable<(Frame frame, GraphServiceClient client, (IEvent target, Event source) calendar)> SynchronizeLocal(
                this IObservable<(Frame frame, GraphServiceClient client, global::Microsoft.Graph.Calendar calendar, IModelCalendarItem calerdarItem)> source)  
            => source
                .Where(t => t.calerdarItem.CallDirection!=CallDirection.Out)
                .SelectMany(t => t.frame.Application
                    .SelectMany(() => t.SynchronizeLocal(t.calerdarItem.SynchronizationType))
                    .Select(calendar => (t.frame,t.client,calendar))
                    .TraceMicrosoftCalendarModule(folder => $"Event: {folder.calendar.source?.Id}"));

        private static IObservable<(IEvent target, Event source)> SynchronizeLocal(
            this (Frame frame, GraphServiceClient client, global::Microsoft.Graph.Calendar calendar,IModelCalendarItem calerdarItem) _, SynchronizationType synchronizationType){
            var newCloudEventType = _.frame.View.Model.Application.Calendar().NewCloudEvent.TypeInfo.Type;
            Func<IObjectSpace> objectSpaceFactory = _.frame.Application.CreateObjectSpace;
            return _.client.Me().CalendarView.ReturnObservable().SynchronizeLocalEvent(objectSpaceFactory,synchronizationType,
                Guid.Parse($"{_.frame.Application.Security.UserId}"), _.frame.View.ObjectTypeInfo.Type, newCloudEventType);
        }
        static IObservable<(Frame frame, GraphServiceClient client, global::Microsoft.Graph.Calendar calendar, IModelCalendarItem calerdarItem)> Authorize(this  IObservable<Frame> source) 
            => source
	            .AuthorizeMS()
                .EnsureCalendar()
                .Publish().RefCount()
                .WhenNotDefault(t => t.frame.Application)
                .Do(tuple => ClientSubject.OnNext((tuple.frame,tuple.client)))
                .SelectMany(t => t.frame.Application.Model.ToReactiveModule<IModelReactiveModuleOffice>().Office.Microsoft()
                    .Calendar().Items.Where(item => item.ObjectView == t.frame.View.Model)
                    .Select(item => (t.frame, t.client, t.calendar, item)))
                .TraceMicrosoftCalendarModule(_ => _.frame.View.Id);

        private static IObservable<(Event serviceObject, MapAction mapAction)> SynchronizeBoth(
            this IObservable<(Frame frame, GraphServiceClient client, global::Microsoft.Graph.Calendar calendar,
                IModelCalendarItem calerdarItem)> source) 
            => source.SynchronizeCloud()
                .Merge(source.SynchronizeLocal().Select(_ => default((Event serviceObject, MapAction mapAction))).IgnoreElements());

        private static IObservable<(Event serviceObject, MapAction mapAction)> SynchronizeCloud(
            this IObservable<(Frame frame, GraphServiceClient client, global::Microsoft.Graph.Calendar calendar,
                IModelCalendarItem calerdarItem)> source) 
            => source.Select(client => client.client.Me().Calendars[client.calendar.Id].ReturnObservable()
                    .SynchronizeCloud(client.calerdarItem.SynchronizationType,
                        client.frame.View.ObjectSpace, client.frame.View.AsObjectView().Application().CreateObjectSpace)
                )
                .Switch()
                .Do(tuple => UpdatedSubject.OnNext((tuple.serviceObject,null,tuple.mapAction,CallDirection.Out)))
                .TraceMicrosoftCalendarModule(_ => $"{_.mapAction} {_.serviceObject.Subject},  {_.serviceObject.Id}");

        private static IObservable<(Event serviceObject, MapAction mapAction)> SynchronizeCloud(this IObservable<ICalendarRequestBuilder> source, 
            SynchronizationType synchronizationType, IObjectSpace objectSpace, Func<IObjectSpace> objectSpaceFactory) 
            => source.SelectMany(builder => objectSpaceFactory.SynchronizeCloud<Event, IEvent>(synchronizationType, objectSpace,
                cloudId => ((IEventRequest) RequestCustomization.Default(builder.EventRequest(cloudId))).DeleteAsync().ToObservable(),
                @event => ((ICalendarEventsCollectionRequest) RequestCustomization.Default(builder.Events.Request())).AddAsync(@event).ToObservable(), 
                t => t.mapAction==MapAction.Delete?((IEventRequest) RequestCustomization.Default(builder.EventRequest(t.cloudId))).GetAsync().ToObservable():new Event().ReturnObservable(),
                t => ((IEventRequest) RequestCustomization.Default(builder.EventRequest(t.cloudId))).UpdateAsync(t.cloudEntity).ToObservable(), 
                e =>e.Handled=MapAction.Delete.CustomSynchronization(objectSpaceFactory, e.Instance.localEntinty, null, CallDirection.Out,synchronizationType,e.Instance.cloudOfficeObject).Handled , 
                t => MapAction.Insert.CustomSynchronization(objectSpaceFactory, t.source, t.target, CallDirection.Out,synchronizationType), 
                t => MapAction.Update.CustomSynchronization(objectSpaceFactory, t.source, t.target, CallDirection.Out,synchronizationType)));

        private static IEventRequest EventRequest(this ICalendarRequestBuilder builder, string cloudId) 
            => builder.Me().Events[cloudId].Request();

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
                    cloud.Location = new Location() { DisplayName = local.Location };
                    cloud.Start = new DateTimeTimeZone(){
                        DateTime = local.StartOn.ToString(CultureInfo.InvariantCulture),
                        TimeZone = TimeZoneInfo.Local.Id 
                    };
                    cloud.End = new DateTimeTimeZone{
                        DateTime = local.EndOn.ToString(CultureInfo.InvariantCulture),
                        TimeZone = TimeZoneInfo.Local.Id 
                    };
                    cloud.Body = new ItemBody() { Content = local.Description };
                    cloud.Subject = local.Subject;
                }
                else{
                    using var objectSpace = objectSpaceFactory();
                    objectSpace.Delete(objectSpace.GetObject(cloudOfficeObject));
                    objectSpace.CommitChanges();
                }
            }

            return e;
        }

        private static IObservable<(IEvent target, Event source)> SynchronizeLocalEvent(this IObservable<IUserCalendarViewCollectionRequestBuilder> source,
            Func<IObjectSpace> objectSpaceFactory, SynchronizationType synchronizationType, Guid currentUserId, Type eventType, Type newCloudEventType) 
            => source.SynchronizeLocalEvent(objectSpaceFactory, currentUserId, (service, deltaToken) 
                    => service.ListDelta(deltaToken, objectSpaceFactory),eventType ,"deltatoken")
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
                .TraceMicrosoftCalendarModule(tuple => $"cloudId: {tuple.cloudEvent.Id}, cloudSubject: {tuple.cloudEvent.Subject}, local:{tuple.localEvent}");

        private static void Update(this IEvent localEvent, GenericEventArgs<(Func<IObjectSpace> objectSpace, IEvent local, Event cloud, MapAction mapAction, CallDirection callDirection)> args){
            localEvent.Location = args.Instance.cloud.Location?.DisplayName;
            localEvent.StartOn = Convert.ToDateTime(args.Instance.cloud.Start?.DateTime).ToLocalTime();
            localEvent.EndOn = Convert.ToDateTime(args.Instance.cloud.End?.DateTime).ToLocalTime();
            localEvent.Subject = args.Instance.cloud.Subject;
            args.Instance.cloud.Body.ContentType=BodyType.Text;
            localEvent.Description = args.Instance.cloud.Body?.Content;
        }
        
        public static IObservable<(Event @event, MapAction mapAction)> ListDelta(this IUserCalendarViewCollectionRequestBuilder calendarViewCollectionRequestBuilder, ICloudOfficeToken cloudOfficeToken,
            Func<IObjectSpace> objectSpaceFactory){
            var queryOptions = new[]{
                new QueryOption("StartDateTime", DeltaSnapShotStartDateTime.ToString(CultureInfo.InvariantCulture)), 
                new QueryOption("EndDateTime", DeltaSnapShotEndDateTime.ToString(CultureInfo.InvariantCulture)),
            };
            
            if (cloudOfficeToken?.Token != null){
                queryOptions =new[] { new QueryOption($"${cloudOfficeToken.TokenType}", cloudOfficeToken.Token) } ;
            }
            return calendarViewCollectionRequestBuilder
                .Delta().Request(queryOptions)
                .Header("Prefer", "odata.track-changes")
                .ListAllItems<Event>(cloudOfficeToken, store => store?.SaveToken(objectSpaceFactory)).ToEnumerable().ToObservable()
                .PairMapAction(objectSpaceFactory)
                .TraceMicrosoftCalendarModule(tuple => $"{tuple.e.Subject}, {tuple.mapAction}");
        }

        public static IObservable<Unit> DeleteAllEvents(this ICalendarRequestBuilder builder) 
            => builder.Events.ListAllItems().DeleteAll(evt => builder.Me().Events[evt.Id].Request().DeleteAsync().ToObservable());

        public static IObservable<Unit> DeleteAll(this IObservable<IEnumerable<Entity>> source, Func<Entity, IObservable<Unit>> delete) 
            => source.Aggregate((acc, curr) => acc.Concat(curr)).SelectMany(entities => entities)
                .SelectMany(delete).LastOrDefaultAsync();

        public static IObservable<(Event e, MapAction mapAction)> PairMapAction(this IObservable<Event[]> source, Func<IObjectSpace> objectSpaceFactory) 
            => source.SelectMany(events => {
                using var objectSpace = objectSpaceFactory();
                return events.Select(e => e.AdditionalData.TryGetValue("@removed", out _)
                    ? (e, MapAction.Delete) : (e, objectSpace.QueryCloudOfficeObject(e.Id, e.GetType().ToCloudObjectType()).Any() ? MapAction.Update : MapAction.Insert)).ToArray();
            });

        public static IObservable<global::Microsoft.Graph.Calendar> GetCalendar(this IUserCalendarsCollectionRequestBuilder builder, string name, bool createNew = false){
            var addNew = createNew.ReturnObservable().WhenNotDefault().SelectMany(_ => builder.Request().AddAsync(new global::Microsoft.Graph.Calendar { Name = name }));
            return builder.Request().Filter($"{nameof(global::Microsoft.Graph.Calendar.Name)} eq '{name}'").GetAsync().ToObservable()
                .SelectMany(page => page).FirstOrDefaultAsync()
                .SwitchIfDefault(addNew)
                .Publish().RefCount();
        }

    }
}