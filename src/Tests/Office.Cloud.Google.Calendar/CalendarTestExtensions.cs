using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base.General;
using DevExpress.Persistent.BaseImpl;
using Google.Apis.Calendar.v3.Data;
using Shouldly;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Office.Cloud.Google.Tests;
using Xpand.XAF.Modules.Reactive;
using Event = DevExpress.Persistent.BaseImpl.Event;
using Platform = Xpand.Extensions.XAF.XafApplicationExtensions.Platform;

namespace Xpand.XAF.Modules.Office.Cloud.Google.Calendar.Tests{
	static class CalendarTestExtensions{
        public const string CalendarName = "Xpand DevOps Events";
        public const string CalendarPagingName = "Xpand Paging Events";
        public const int PagingItemsCount = 251;
        public static async Task<global::Google.Apis.Calendar.v3.CalendarService> CalendarService(this XafApplication application,bool deleteAll=false){
            application.ObjectSpaceProvider.NewAuthentication();
            var calendarService = await application.AuthorizeGoogle().NewService<global::Google.Apis.Calendar.v3.CalendarService>();
            if (deleteAll){
                var calendar = await calendarService.GetCalendar(CalendarName);
                await calendarService.DeleteAllEvents(calendar.Id);
            }
            return calendarService;
        }

        public static async Task<(global::Google.Apis.Calendar.v3.CalendarService service, Frame frame)>
            InitializeService(this XafApplication application, string defaultCalendarName = CalendarName,
                bool keepEvents = false, bool keepCalendar = false,bool newAuthentication=true){
            var modelTodo = application.Model.ToReactiveModule<IModelReactiveModuleOffice>().Office.Google().Calendar();
            modelTodo.DefaultCalendarName = defaultCalendarName;
            var t = await application.InitService(newAuthentication);
            var calendarListResource = t.service.CalendarList;
            
            var calendar = await t.service.GetCalendar(defaultCalendarName, !keepCalendar && defaultCalendarName!=CalendarPagingName);
            if (defaultCalendarName==CalendarPagingName){
                calendar ??= await calendarListResource.Insert(new CalendarListEntry(){Summary = CalendarPagingName}).ExecuteAsync();
                var count = (await t.service.ListEvents(calendar.Id).SelectMany(tasks => tasks).Select(tasks => tasks.Items.ToArray()).Sum(entities => entities.Length));
                var itemsCount = PagingItemsCount-count;
                if (itemsCount>0){
                    await t.service.NewCalendarEvents(itemsCount,calendar.Id, nameof(GoogleCalendarModule));
                }
            }
            
            if (defaultCalendarName != CalendarPagingName&&!keepEvents&&!keepCalendar){
                await t.service.DeleteAllEvents(calendar.Id);
                var listTasks = (await t.service.ListEvents(calendar.Id).SelectMany(tasks => tasks));
                listTasks.Items.Count.ShouldBe(0);
            }
            
            return (t.service,t.frame);
        }
        
        public static void Modify_Event<TEvent>(this TEvent @event,  int i) where TEvent:IEvent{
            @event.Subject = $"{nameof(Modify_Event)}{i}";
        }

        public static async Task<(Frame frame, global::Google.Apis.Calendar.v3.CalendarService service)> InitService(this XafApplication application,bool newAuthentication=true){
	        if (newAuthentication){
		        application.ObjectSpaceProvider.NewAuthentication();
	        }
            var todoModel = await application.ReactiveModulesModel().Office().Google().Calendar();
            var window = application.CreateViewWindow();
            var service = Calendar.CalendarService.Credentials.FirstAsync().SubscribeReplay();
            window.SetView(application.NewView(todoModel.Items.Select(item => item.ObjectView).First()));
            return (await service.Select(t => (t.frame,t.credential.NewService<global::Google.Apis.Calendar.v3.CalendarService>())).ToTaskWithoutConfigureAwait());
        }

        public static GoogleCalendarModule CalendarModule(this Platform platform,params ModuleBase[] modules){
            var application = NewApplication(platform,  modules);
            application.SetupGoogleSecurity();
            var module = application.AddModule<GoogleCalendarModule>(typeof(Event));
            application.Model.ConfigureGoogle(platform);
            var todoModel = application.Model.ToReactiveModule<IModelReactiveModuleOffice>().Office.Google().Calendar();
            var dependency = todoModel.Items.AddNode<IModelCalendarItem>();
            dependency.ObjectView = application.Model.BOModel.GetClass(typeof(Event)).DefaultDetailView;
            application.Logon();
            application.CreateObjectSpace();
            return module.Application.Modules.OfType<GoogleCalendarModule>().First();  
        }

        static XafApplication NewApplication(this Platform platform,  ModuleBase[] modules){
            var xafApplication = platform.NewApplication<GoogleCalendarModule>();
            xafApplication.Modules.AddRange(modules);
            return xafApplication;
        }

        public static void AssertEvent(this IObjectSpaceProvider objectSpaceProvider, Type cloudEntityType, Event @event,
            string title, DateTime? due, string taskId, string localEventSubject){
            title.ShouldBe(localEventSubject);
            
            due.ShouldNotBeNull();


            using var space = objectSpaceProvider.CreateObjectSpace();
            var cloudObjects = space.QueryCloudOfficeObject(cloudEntityType,@event).ToArray();
            cloudObjects.Length.ShouldBe(1);
            var cloudObject = cloudObjects.First();
            cloudObject.LocalId.ShouldBe(@event.Oid.ToString());
            cloudObject.CloudId.ShouldBe(taskId);
        }

        public static async Task<IList<(Event local, global::Google.Apis.Calendar.v3.Data.Event cloudEvent)>> CreateExistingObjects(
            this XafApplication application, string title,int count=1){
            var credential =await application.AuthorizeTestGoogle();
            var calendarService = credential.NewService<global::Google.Apis.Calendar.v3.CalendarService>();
            var calendar = await calendarService.GetCalendar(CalendarName,true);
            
            await calendarService.DeleteAllEvents(calendar.Id);
            return await Observable.Range(0, count)
                .SelectMany(_ => calendarService.NewCalendarEvents(1,calendar.Id, title)
                    .SelectMany(lst => lst).Select(outlookTask1 => (application.NewEvent(), outlookTask1)))
                .Buffer(count);
        }

        public static Event NewEvent(this IObjectSpace objectSpace,int index=0,string subject=null) {
            var @event = objectSpace.CreateObject<Event>();
            @event.Subject =subject?? $"Subject{index}";
            
            @event.StartOn=DateTime.Now.AddDays(1);
            @event.EndOn = @event.StartOn.AddMinutes(30);
            var resource = objectSpace.CreateObject<Resource>();
            resource.Caption = "organizer@mail.com";
            @event.Resources.Add(resource);
            return @event;
        }

        public static Event NewEvent(this XafApplication application){
            using var objectSpace = application.CreateObjectSpace();
            var newEvent = objectSpace.NewEvent();
            objectSpace.CommitChanges();
            return newEvent;
        }

        public static IObservable<IList<global::Google.Apis.Calendar.v3.Data.Event>> NewCalendarEvents(
            this global::Google.Apis.Calendar.v3.CalendarService calendarService, int count, string calendarId, string title){
            var dateTime = DateTime.Now;
            return Observable.Range(0, count).SelectMany(i => Observable.FromAsync(() => {
                var task = new global::Google.Apis.Calendar.v3.Data.Event(){
                    Summary = i>0?$"{title}{i}": title??i.ToString(),
                    End = new EventDateTime(){DateTime = dateTime.AddDays(i+1)},
                    Start = new EventDateTime(){DateTime = dateTime}
                };
                return calendarService.Events.Insert(task, calendarId).ExecuteAsync();
            })).Buffer(count);
        }

        public static async System.Threading.Tasks.Task Delete_Event_Resource<TEvent>(this IObjectSpace objectSpace, Func<Event, IObservable<TEvent>> synchronizeEvents,
            Func<System.Threading.Tasks.Task> assert,TimeSpan timeout){
            
            var localEvent = objectSpace.NewEvent();
            var calendarEvents = synchronizeEvents(localEvent)
                .FirstAsync().SubscribeReplay();
            objectSpace.CommitChanges();
            await calendarEvents.Timeout(timeout);

            calendarEvents = synchronizeEvents(localEvent)
                .FirstAsync().SubscribeReplay();
            localEvent.Resources.Remove(localEvent.Resources.First());
            objectSpace.CommitChanges();


            await calendarEvents.Timeout(timeout);

            await assert();
        }

    }
}