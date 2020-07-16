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

        private static readonly ISubject<(Event, IEvent)> CustomizeCloudSynchronizationSubject;
        static CalendarService() => CustomizeCloudSynchronizationSubject = Subject.Synchronize(new Subject<(Event, IEvent)>());
        internal const string DefaultTodoListId = "Tasks";
        private static IUserRequestBuilder Me(this IBaseRequestBuilder builder) => builder.Client.Me();
        private static IUserRequestBuilder Me(this IBaseClient client) => ((GraphServiceClient)client).Me;
        static readonly ISubject<GenericEventArgs<CloudOfficeObject>> CustomizeDeleteSubject=Subject.Synchronize(new Subject<GenericEventArgs<CloudOfficeObject>>());
        static readonly ISubject<(Event cloudEvent,IEvent localEvent)> CustomizeInsertSubject=Subject.Synchronize(new Subject<(Event cloudEvent,IEvent localEvent)>());
        static readonly ISubject<(Event cloudEvent,IEvent localEvent)> CustomizeUpdateSubject=Subject.Synchronize(new Subject<(Event cloudEvent,IEvent localEvent)>());

        [PublicAPI]
        public static IObservable<GenericEventArgs<CloudOfficeObject>> CustomizeDelete => CustomizeDeleteSubject.AsObservable();
        [PublicAPI]
        public static IObservable<(Event cloudEvent,IEvent localEvent)> CustomizeUpdate => CustomizeUpdateSubject.AsObservable();
        [PublicAPI]
        public static IObservable<(Event cloudEvent,IEvent localEvent)> CustomizeInsert => CustomizeInsertSubject.AsObservable();
        
        static readonly Subject<(Event serviceObject, MapAction mapAction)> UpdatedSubject=new Subject<(Event serviceObject, MapAction mapAction)>();

        public static IObservable<(Event serviceObject, MapAction mapAction)> Updated{ get; }=UpdatedSubject.AsObservable();


        internal static IObservable<TSource> TraceMicrosoftCalendarModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
            source.Trace(name, MicrosoftCalendarModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);
        
        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) =>
            manager.WhenApplication(application => application.WhenViewOnFrame()
                .When(frame => application.Model.ToReactiveModule<IModelReactiveModuleOffice>().Office
                    .Microsoft().Calendar().ObjectViews())
                .Authorize()
                .Synchronize()
                .ToUnit());

        private static IObservable<(Frame frame, GraphServiceClient client, global::Microsoft.Graph.Calendar calendar)> EnsureCalendar(this IObservable<(Frame frame, GraphServiceClient client)> source) =>
            source.Select(_ => Observable.Start(() => _.client.Me.Calendars
                        .GetCalendar(_.frame.View.AsObjectView().Application().Model.ToReactiveModule<IModelReactiveModuleOffice>().Office.Microsoft()
                            .Calendar().DefaultCaledarName, true)).Merge().Wait().ReturnObservable()
                    .Select(folder => (_.frame,_.client,folder))
                ).Merge()
                .TraceMicrosoftCalendarModule(folder => folder.folder.Name);

        static IObservable<(Frame frame, GraphServiceClient client, global::Microsoft.Graph.Calendar calendar)> Authorize(this  IObservable<Frame> whenViewOnFrame) => whenViewOnFrame
            .AuthorizeMS().EnsureCalendar()
            .Do(tuple => ClientSubject.OnNext((tuple.frame,tuple.client)))
            .TraceMicrosoftCalendarModule(_ => _.frame.View.Id);
        
        
        private static readonly ISubject<(IEventAttendee target, Attendee source)> CustomizeResponseStatusSynchronizationSubject = Subject.Synchronize(new Subject<(IEventAttendee target, Attendee source)>());

        [PublicAPI]
        public static IObservable<(IEventAttendee target, Attendee source)> CustomizeResponseStatusSynchronization => CustomizeResponseStatusSynchronizationSubject.AsObservable();

        private static IObservable<(Event serviceObject, MapAction mapAction)> Synchronize(this IObservable<(Frame frame, GraphServiceClient client, global::Microsoft.Graph.Calendar calendar)> source) =>
            source.Select(client => client.client.Me.Calendars[client.calendar.Id].ReturnObservable().Synchronize(
                        client.frame.View.ObjectSpace, client.frame.View.AsObjectView().Application().CreateObjectSpace,
                        CustomizeDeleteSubject.OnNext, CustomizeInsertSubject.OnNext,
                        CustomizeUpdateSubject.OnNext)
                    .TakeUntil(client.frame.View.WhenClosing())
                ).Switch()
                .Do(UpdatedSubject)
                .TraceMicrosoftCalendarModule(_ => $"{_.mapAction} {_.serviceObject.Subject},  {_.serviceObject.Id}");

        public static IObservable<(Event serviceObject, MapAction mapAction)> Synchronize(this IObservable<ICalendarRequestBuilder> source, IObjectSpace objectSpace, Func<IObjectSpace> objectSpaceFactory,
            Action<GenericEventArgs<CloudOfficeObject>> delete = null, Action<(Event target, IEvent source)> insert = null, Action<(Event target, IEvent source)> update = null) => source
            .SelectMany(builder => objectSpaceFactory.Synchronize(objectSpace, cloudId => (builder.Me().Events[cloudId].Request()).DeleteAsync().ToObservable(),
                @event => builder.Events.Request().AddAsync(@event).ToObservable(), cloudId => new Event().ReturnObservable(),
                _ => (builder.Me().Events[_.cloudId].Request()).UpdateAsync(_.cloudEntity).ToObservable(),
                (target, sourceEntity) => CustomizeCloudSynchronizationSubject.SynchronizeCloud(target, sourceEntity), delete, insert, update));

        public static IObservable<Event> SynchronizeEventLocal<TEvent>(this IObservable<IUserCalendarViewCollectionRequestBuilder> source,
            Func<IObjectSpace> objectSpaceFactory, Guid currentUserId) where TEvent : IEvent, IEventAttendees => source.SynchronizeEventResources<TEvent, Event, Attendee, IUserCalendarViewCollectionRequestBuilder>(objectSpaceFactory, currentUserId
                , attendee => attendee.EmailAddress.Address, ResponseStatusNotificationCustomization, (service, tokenStorage) => service.ListDelta(tokenStorage, objectSpaceFactory).SelectMany(events => events));

        private static void ResponseStatusNotificationCustomization(IEventAttendee target, Attendee source){
            CustomizeResponseStatusSynchronizationSubject.OnNext((target, source));
        }

        [PublicAPI]
        public static DateTime DeltaSnapShotStartDateTime { get; set; } = DateTime.Now.AddMonths(-12);
        [PublicAPI]
        public static DateTime DeltaSnapShotEndDateTime { get; set; } = DateTime.Now.AddMonths(12 * 5);
        public static IObservable<Event[]> ListDelta(this IUserCalendarViewCollectionRequestBuilder calendarViewCollectionRequestBuilder, ITokenStore storage, Func<IObjectSpace> objectSpaceFactory){
            var queryOptions = new[]{
                new QueryOption("StartDateTime", DeltaSnapShotStartDateTime.ToString(CultureInfo.InvariantCulture)),
                new QueryOption("EndDateTime", DeltaSnapShotEndDateTime.ToString(CultureInfo.InvariantCulture)),
            };
            if (storage.Token != null){
                queryOptions = new[] { new QueryOption($"${storage.TokenType}", storage.Token) };
            }
            return calendarViewCollectionRequestBuilder
                .Delta().Request(queryOptions)
                .Header("Prefer", "odata.track-changes")
                .ListAllItems<Event>(storage, store => store.SaveToken(objectSpaceFactory));
        }

        public static IObservable<global::Microsoft.Graph.Calendar> GetCalendar(this IUserCalendarsCollectionRequestBuilder builder, string name, bool createNew = false){
            var addNew = createNew.ReturnObservable().WhenNotDefault().SelectMany(_ => builder.Request().AddAsync(new global::Microsoft.Graph.Calendar { Name = name }));
            return builder.Request().Filter($"{nameof(global::Microsoft.Graph.Calendar.Name)} eq '{name}'").GetAsync().ToObservable()
                .SelectMany(page => page).FirstOrDefaultAsync()
                .SwitchIfDefault(addNew);
        }

        private static Event SynchronizeCloud(this ISubject<(Event target, IEvent source)> subject, Event target, IEvent source){
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
            if (source is IEventAttendees eventAttendees){
                target.Attendees = eventAttendees.Attendees.Select(attendee => new Attendee()
                { EmailAddress = new EmailAddress() { Address = attendee.UserEmail } }).ToArray();
            }
            subject.OnNext((target, source));
            return target;
        }
    }
}