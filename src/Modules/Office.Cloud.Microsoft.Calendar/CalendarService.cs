using System;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base.General;
using JetBrains.Annotations;
using Microsoft.Graph;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Office.Cloud.BusinessObjects;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.ViewExtenions;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft.Calendar{
    public static class CalendarService{
        private static readonly ISubject<(Frame frame, GraphServiceClient client)> ClientSubject=new Subject<(Frame frame, GraphServiceClient client)>();

        public static IObservable<(Frame frame, GraphServiceClient client)> Client => ClientSubject.AsObservable();

        private static readonly ISubject<(MapAction mapAction,Event cloud, IEvent local)> CustomizeCloudSynchronizationSubject;
        static CalendarService() => CustomizeCloudSynchronizationSubject = Subject.Synchronize(new Subject<(MapAction mapAction,Event cloud, IEvent local)>());
        internal const string DefaultTodoListId = "Tasks";
        private static IUserRequestBuilder Me(this IBaseRequestBuilder builder) => builder.Client.Me();
        private static IUserRequestBuilder Me(this IBaseClient client) => ((GraphServiceClient)client).Me;
        static readonly ISubject<GenericEventArgs<(CloudOfficeObject officeObject,CallDirection callDirection)>> CustomizeDeleteSubject=Subject.Synchronize(new Subject<GenericEventArgs<(CloudOfficeObject officeObject,CallDirection callDirection)>>());
        static readonly ISubject<(Event cloudEvent,IEvent localEvent,CallDirection callDirection)> CustomizeInsertSubject=Subject.Synchronize(new Subject<(Event cloudEvent,IEvent localEvent,CallDirection callDirection)>());
        static readonly ISubject<(Event cloudEvent,IEvent localEvent,CallDirection callDirection)> CustomizeUpdateSubject=Subject.Synchronize(new Subject<(Event cloudEvent,IEvent localEvent,CallDirection callDirection)>());

        [PublicAPI]
        public static IObservable<GenericEventArgs<(CloudOfficeObject officeObject,CallDirection callDirection)>> CustomizeDelete => CustomizeDeleteSubject.AsObservable();
        [PublicAPI]
        public static IObservable<(Event cloudEvent,IEvent localEvent,CallDirection callDirection)> CustomizeUpdate => CustomizeUpdateSubject.AsObservable();
        [PublicAPI]
        public static IObservable<(Event cloudEvent,IEvent localEvent,CallDirection callDirection)> CustomizeInsert => CustomizeInsertSubject.AsObservable();
        
        static readonly Subject<(Event serviceObject, MapAction mapAction,CallDirection callDirection)> UpdatedSubject=new Subject<(Event serviceObject, MapAction mapAction,CallDirection callDirection)>();

        public static IObservable<(Event serviceObject, MapAction mapAction, CallDirection callDirection)> When(this IObservable<(Event serviceObject, MapAction mapAction,CallDirection callDirection)> source, MapAction mapAction)
            => source.Where(_ => _.mapAction == mapAction);

        public static IObservable<(Event serviceObject, MapAction mapAction,CallDirection callDirection)> Updated{ get; }=UpdatedSubject.AsObservable();

        internal static IObservable<TSource> TraceMicrosoftCalendarModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, MicrosoftCalendarModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);
        
        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) =>
            manager.WhenApplication(application => application.WhenViewOnFrame()
                .When(frame => application.Model.ToReactiveModule<IModelReactiveModuleOffice>().Office
                    .Microsoft().Calendar().ObjectViews())
                .Authorize()
                .SynchronizeBoth()
                .ToUnit());

        private static IObservable<(Frame frame, GraphServiceClient client, global::Microsoft.Graph.Calendar calendar)> EnsureCalendar(this IObservable<(Frame frame, GraphServiceClient client)> source) 
            => source.Select(_ => _.frame.Application.SelectMany(() => _.client.Me.Calendars
                        .GetCalendar(_.frame.View.AsObjectView().Application().Model.ToReactiveModule<IModelReactiveModuleOffice>().Office.Microsoft().Calendar().DefaultCaledarName, true))
                    .Select(calendar => (_.frame,_.client,calendar))
                ).Merge()
                .TraceMicrosoftCalendarModule(tuple => tuple.calendar.Name)
                .Select(tuple => (tuple.frame,tuple.client,tuple.calendar));
        
        private static IObservable<(Frame frame, GraphServiceClient client, (IEvent target, Event source) calendar)> SynchronizeLocal(this IObservable<(Frame frame, GraphServiceClient client, global::Microsoft.Graph.Calendar calendar)> source)  
            => source.Select(_ => _.frame.Application
                    .SelectMany(() => {
                        var newCloudEventType = _.frame.View.Model.Application.CalendarModel().NewCloudEvent.TypeInfo.Type;
                        Func<IObjectSpace> objectSpaceFactory = _.frame.Application.CreateObjectSpace;
                        return _.client.Me.CalendarView.ReturnObservable().SynchronizeLocalEvent(objectSpaceFactory,
                                Guid.Parse($"{_.frame.Application.Security.UserId}"), _.frame.View.ObjectTypeInfo.Type,newCloudEventType);
                    })
                    .Select(calendar => (_.frame,_.client,calendar))
                ).Merge()
                .TraceMicrosoftCalendarModule(folder => $"{folder.calendar.source?.Id}")
        ;

        static IObservable<(Frame frame, GraphServiceClient client, global::Microsoft.Graph.Calendar calendar)> Authorize(this  IObservable<Frame> whenViewOnFrame) 
            => whenViewOnFrame.AuthorizeMS()
                .EnsureCalendar()
                .Do(tuple => ClientSubject.OnNext((tuple.frame,tuple.client)))
                .Publish().RefCount()
                .TraceMicrosoftCalendarModule(_ => _.frame.View.Id);

        private static IObservable<(Event serviceObject, MapAction mapAction)> SynchronizeBoth(this IObservable<(Frame frame, GraphServiceClient client, global::Microsoft.Graph.Calendar calendar)> source) 
            => source.SynchronizeCloud()
                .Merge(source.LocalSynchronizationSignal().SynchronizeLocal().Select(_ => default((Event serviceObject, MapAction mapAction))).IgnoreElements());

        private static IObservable<(Frame frame, GraphServiceClient client, global::Microsoft.Graph.Calendar calendar)> LocalSynchronizationSignal(this IObservable<(Frame frame, GraphServiceClient client, global::Microsoft.Graph.Calendar calendar)> source) 
            => source;

        private static IObservable<(Event serviceObject, MapAction mapAction)> SynchronizeCloud(this IObservable<(Frame frame, GraphServiceClient client, global::Microsoft.Graph.Calendar calendar)> source) 
            => source.SelectMany(client => client.client.Me.Calendars[client.calendar.Id].ReturnObservable().SynchronizeCloud(
                        client.frame.View.ObjectSpace, client.frame.View.AsObjectView().Application().CreateObjectSpace,
                        args => CustomizeDeleteSubject.OnNext(new GenericEventArgs<(CloudOfficeObject officeObject, CallDirection callDirection)>((args.Instance,CallDirection.Outgoing))),
                        tuple => CustomizeInsertSubject.OnNext((tuple.target,tuple.source,CallDirection.Outgoing)), tuple => CustomizeUpdateSubject.OnNext((tuple.target,tuple.source,CallDirection.Outgoing)))
                    .TakeUntil(client.frame.View.WhenClosing())
                )
                .Do(tuple => UpdatedSubject.OnNext((tuple.serviceObject,tuple.mapAction,CallDirection.Outgoing)))
                .TraceMicrosoftCalendarModule(_ => $"{_.mapAction} {_.serviceObject.Subject},  {_.serviceObject.Id}");

        private static IObservable<(Event serviceObject, MapAction mapAction)> SynchronizeCloud(this IObservable<ICalendarRequestBuilder> source, IObjectSpace objectSpace, Func<IObjectSpace> objectSpaceFactory,
            Action<GenericEventArgs<CloudOfficeObject>> delete = null, Action<(Event target, IEvent source)> insert = null, Action<(Event target, IEvent source)> update = null) 
            => source.SelectMany(builder => objectSpaceFactory.Synchronize(objectSpace, cloudId => (builder.Me().Events[cloudId].Request()).DeleteAsync().ToObservable(),
                @event => builder.Events.Request().AddAsync(@event).ToObservable(), cloudId => new Event().ReturnObservable(),
                _ => (builder.Me().Events[_.cloudId].Request()).UpdateAsync(_.cloudEntity).ToObservable(),
                (mapAction,target, sourceEntity) => CustomizeCloudSynchronizationSubject.SynchronizeCloud(target, sourceEntity,mapAction), delete, insert, update));

        static readonly Subject<GenericEventArgs<(IObjectSpace objectSpace,IEvent target, Event source,MapAction mapAction)>> CustomizeLocalSynchronizationSubject=new Subject<GenericEventArgs<(IObjectSpace objectSpace,IEvent target, Event source, MapAction mapAction)>>();

        public static IObservable<GenericEventArgs<(IObjectSpace objectSpace,IEvent target, Event source, MapAction mapAction)>> CustomizeLocalSynchronization => CustomizeLocalSynchronizationSubject.AsObservable();
        
        private static IObservable<(IEvent target, Event source)> SynchronizeLocalEvent(this IObservable<IUserCalendarViewCollectionRequestBuilder> source,
            Func<IObjectSpace> objectSpaceFactory, Guid currentUserId,Type eventType,Type newCloudEventType) 
            => source.SynchronizeLocalEvent(objectSpaceFactory, currentUserId, (service, tokenStorage) 
                    => service.ListDelta(tokenStorage, objectSpaceFactory),eventType )
                .Select(tuple => {
                    var args = new GenericEventArgs<(IObjectSpace objectSpace,IEvent target, Event source, MapAction mapAction)>((tuple.objectSpace,tuple.local,tuple.cloud,tuple.mapAction));
                    CustomizeLocalSynchronizationSubject.OnNext(args);
                    var localEvent=args.Instance.target;
                    if (!args.Handled){
                        if (args.Instance.mapAction == MapAction.Delete){
                            args.Instance.objectSpace.Delete(tuple.local);
                        }
                        else {
                            if (args.Instance.mapAction == MapAction.Insert){
                                localEvent = (IEvent)args.Instance.objectSpace.CreateObject(newCloudEventType);
                                args.Instance.objectSpace.CommitChanges();
                                args.Instance.objectSpace.NewCloudObject(localEvent, args.Instance.source);
                            }
                            
                            localEvent.Location = args.Instance.source.Location?.DisplayName;
                            localEvent.StartOn = Convert.ToDateTime(args.Instance.source.Start?.DateTime);
                            localEvent.EndOn = Convert.ToDateTime(args.Instance.source.End?.DateTime);
                            localEvent.Subject = args.Instance.source.Subject;
                            localEvent.Description = args.Instance.source.Body?.Content;
                        }
                    }
                    return (localEvent, cloudEvent: tuple.cloud);
                })
                .TraceMicrosoftCalendarModule(tuple => $"source: {tuple.cloudEvent.Id}, target:{tuple.localEvent.Subject}");
        
        [PublicAPI]
        public static DateTime DeltaSnapShotStartDateTime { get; set; } = DateTime.Now.AddMonths(-12);
        [PublicAPI]
        public static DateTime DeltaSnapShotEndDateTime { get; set; } = DateTime.Now.AddMonths(12 * 5);
        public static IObservable<(Event @event, MapAction mapAction)> ListDelta(this IUserCalendarViewCollectionRequestBuilder calendarViewCollectionRequestBuilder, ITokenStore storage,
            Func<IObjectSpace> objectSpaceFactory){
            var queryOptions = new[]{
                new QueryOption("StartDateTime", DeltaSnapShotStartDateTime.ToString(CultureInfo.InvariantCulture)), 
                new QueryOption("EndDateTime", DeltaSnapShotEndDateTime.ToString(CultureInfo.InvariantCulture)),
            };
            
            if (storage.Token != null){
                queryOptions =new[] { new QueryOption($"${storage.TokenType}", storage.Token) } ;
            }
            return calendarViewCollectionRequestBuilder
                .Delta().Request(queryOptions)
                .Header("Prefer", "odata.track-changes")
                .ListAllItems<Event>(storage, store => store.SaveToken(objectSpaceFactory)).ToEnumerable().ToObservable()
                .CalculateMap(objectSpaceFactory)
                .TraceMicrosoftCalendarModule(tuple => $"{tuple.e.Subject}, {tuple.mapAction}");
        }


        public static IObservable<(Event e, MapAction mapAction)> CalculateMap(this IObservable<Event[]> source, Func<IObjectSpace> objectSpaceFactory) 
            => source.SelectMany(events => {
                using (var objectSpace = objectSpaceFactory()){
                    return events.Select(e => e.AdditionalData.TryGetValue("@removed", out _)
                        ? (e, MapAction.Delete) : (e, objectSpace.QueryCloudOfficeObject(e.Id, e.GetType().ToCloudObjectType()).Any() ? MapAction.Update : MapAction.Insert)).ToArray();
                }
            });

        public static IObservable<global::Microsoft.Graph.Calendar> GetCalendar(this IUserCalendarsCollectionRequestBuilder builder, string name, bool createNew = false){
            var addNew = createNew.ReturnObservable().WhenNotDefault().SelectMany(_ => builder.Request().AddAsync(new global::Microsoft.Graph.Calendar { Name = name }));
            return builder.Request().Filter($"{nameof(global::Microsoft.Graph.Calendar.Name)} eq '{name}'").GetAsync().ToObservable()
                .SelectMany(page => page).FirstOrDefaultAsync()
                .SwitchIfDefault(addNew);
        }

        private static Event SynchronizeCloud(this ISubject<(MapAction mapAction,Event cloud, IEvent local)> subject, Event target, IEvent source,MapAction mapAction){
            target.Location = new Location() { DisplayName = source.Location };
            target.Start = new DateTimeTimeZone(){
                DateTime = source.StartOn.ToString(CultureInfo.InvariantCulture),
                TimeZone = TimeZoneInfo.Local.Id 
            };
            target.End = new DateTimeTimeZone{
                DateTime = source.EndOn.ToString(CultureInfo.InvariantCulture),
                TimeZone = TimeZoneInfo.Local.Id 
            };
            target.Body = new ItemBody() { Content = source.Description };
            target.Subject = source.Subject;
            subject.OnNext((mapAction, target, source));
            return target;
        }
    }
}