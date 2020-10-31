using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.Xpo;
using Microsoft.Graph;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.TaskExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.Office.Cloud.Tests;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Logger;
using Xpand.XAF.Modules.Reactive.Services;
using Platform = Xpand.Extensions.XAF.XafApplicationExtensions.Platform;


namespace Xpand.XAF.Modules.Office.Cloud.Microsoft.Calendar.Tests{
    [NonParallelizable]
    public class CalendarServiceTests : BaseCalendarTests{
        
        [Test]
        [XpandTest()]
        public override async Task Map_Two_New_Events(){
            await MapTwoNewEvents(CalendarTestExtensions.CalendarName);
        }
        
        private async Task MapTwoNewEvents(string tasksFolderName,Func<ICalendarRequestBuilder, IObservable<Unit>> afterAssert=null,bool keepTaskFolder=false){
            using var application = Platform.Win.CalendarModule().Application;
            var builder = await application.InitializeService(tasksFolderName,keepEvents:keepTaskFolder);
            await builder.frame.View.ObjectSpace.Map_Two_New_Entity(
                (space, i) => space.NewEvent(i), Timeout,
                space => CalendarService.Updated.When(MapAction.Insert).TakeUntilDisposed(application), async (task, _, i) => {
                    if (afterAssert != null){
                        await afterAssert(builder.requestBuilder);
                    }
                    else{
                        application.ObjectSpaceProvider.AssertEvent(typeof(OutlookTask), task, _.cloud.Subject,
                            DateTime.Parse(_.cloud.End.DateTime, CultureInfo.InvariantCulture),
                            _.cloud.Id, task.Subject);    
                    }
                            
                });
        }

        [Test]
        [XpandTest()]
        public override async Task Customize_Two_New_Event(){
            using var application = Platform.Win.CalendarModule().Application;
            var builder = await application.InitializeService();
            await builder.frame.View.ObjectSpace.Map_Two_New_Entity((space,i)=>space.NewEvent(i), Timeout,
                space => CalendarService.Updated.When(MapAction.Insert).Select(_ => _.cloud)
                    .Merge(CalendarService.CustomizeSynchronization.Where(e => e.Instance.mapAction==MapAction.Insert)
                        .Select((tuple, i) => {
                            tuple.Instance.cloud.Subject = $"{nameof(Customize_Two_New_Event)}{i}";
                            tuple.Handled = true;
                            return tuple.Instance.cloud;
                        }).IgnoreElements())
                    .TakeUntilDisposed(application), 
                (cloudEvent, taskListEntry,i) => {
                    application.ObjectSpaceProvider.AssertEvent(typeof(Event),cloudEvent, taskListEntry.Subject, DateTime.Parse(taskListEntry.End.DateTime),  taskListEntry.Id,$"{nameof(Customize_Two_New_Event)}{i}");
                });
        }

        [Test][XpandTest()]
        public override async Task Map_Existing_Event_Two_Times(){
            using var application = Platform.Win.CalendarModule().Application;
            var builderData = await application.InitializeService();
            var existingObjects = (await application.CreateExistingObjects(nameof(Map_Existing_Event_Two_Times))).First();

            await builderData.frame.View.ObjectSpace.Map_Existing_Entity_Two_Times(existingObjects.local,
                (pmeTask,i) => pmeTask.Modify_Event( i), existingObjects.cloud
                , space => CalendarService.Updated.When(MapAction.Update).Select(_ => _.cloud).TakeUntilDisposed(application),
                (local, cloud) => {
                    cloud.Subject.ShouldBe(local.Subject);
                    return Task.CompletedTask;
                },Timeout);
        }

        [Test][XpandTest()]
        public override async Task Customize_Map_Existing_Event_Two_Times(){
            using var application = Platform.Win.CalendarModule().Application;
            var builder = await application.InitializeService();
            var existingObjects = (await application.CreateExistingObjects(nameof(Customize_Map_Existing_Event_Two_Times))).First();
            await builder.frame.View.ObjectSpace.Map_Existing_Entity_Two_Times(existingObjects.local,
                (pmeTask,i) => pmeTask.Modify_Event( i),existingObjects.cloud, space
                    => CalendarService.Updated.When(MapAction.Update).Select(_ => _.cloud)
                        .Merge(CalendarService.CustomizeSynchronization.Where(e => e.Instance.mapAction==MapAction.Update).Take(2)
                            .Do(_ => {
                                _.Instance.cloud.Subject = nameof(Customize_Map_Existing_Event_Two_Times);
                                _.Handled = true;
                            }).To(default(Event)).IgnoreElements())
                        .TakeUntilDisposed(application),
                (pmeTask, cloudTask) => {
                    cloudTask.Subject.ShouldBe(nameof(Customize_Map_Existing_Event_Two_Times));
                    return Task.CompletedTask;
                },Timeout);
        }

        [Test][XpandTest()]
        public override async Task Delete_Two_Events(){
            using var application = Platform.Win.CalendarModule().Application;
            var builder = await application.InitializeService();
            var existingObjects = await application.CreateExistingObjects(nameof(Delete_Two_Events),count:2);

            await builder.frame.View.ObjectSpace.Delete_Two_Entities(existingObjects.Select(tuple => tuple.local).ToArray(),
                space => CalendarService.Updated.When(MapAction.Delete).Select(_ => _.cloud).TakeUntilDisposed(application), async () => {
                    var allTasks = await builder.requestBuilder.Events.ListAllItems();
                    allTasks.Length.ShouldBe(0);
                }, Timeout,existingObjects.Select(_ => _.cloud).ToArray());
        }

        [TestCase(false)]
        [TestCase(true)]
        [XpandTest()]
        public override async Task Customize_Delete_Two_Events(bool handleDeletion){
            using var application = Platform.Win.CalendarModule().Application;
            var builder = await application.InitializeService();
            var existingObjects = await application.CreateExistingObjects(nameof(Customize_Delete_Two_Events),count:2);
            var deleteTwoEntities = builder.frame.View.ObjectSpace.Delete_Two_Entities(existingObjects.Select(_ => _.local).ToArray(),
                objectSpace => CalendarService.CustomizeSynchronization.When(MapAction.Delete).Take(2)
                    .Do(_ => _.Handled=handleDeletion).To(default(Event))
                    .TakeUntilDisposed(application)
                    .IgnoreElements()
                    .Merge(CalendarService.Updated.When(MapAction.Delete).To(default(Event)).Take(2).TakeUntilDisposed(application))
                ,
                async () => {
                    var allTasks = await builder.requestBuilder.Events.ListAllItems();
                    allTasks.Length.ShouldBe(handleDeletion?2:0);
                }, Timeout,existingObjects.Select(_ => _.cloud).ToArray());
            await deleteTwoEntities;
        }

        [Test]
        [XpandTest()]
        public override async Task Delete_Local_Event_Resource(){
            using var application = Platform.Win.CalendarModule().Application;
            var builder = await application.InitializeService();

            await builder.frame.View.ObjectSpace.Delete_Event_Resource(pmeEvent =>
                CalendarService.Updated.Select(_ => _.local).TakeUntilDisposed(application), async () => {
                var events = await builder.requestBuilder.Events.Request().GetAsync();
                events.Count.ShouldBe(1);
                events.First().Attendees.Count().ShouldBe(0);
            }, Timeout);
        }

        [XpandTest()]
        [Test]
        public override async Task Create_Entity_Container_When_Not_Exist(){
            using var application = Platform.Win.CalendarModule().Application;
            var modelTodo = application.Model.ToReactiveModule<IModelReactiveModuleOffice>().Office.Microsoft().Calendar();
            modelTodo.DefaultCalendarName = $"{nameof(Create_Entity_Container_When_Not_Exist)}{Guid.NewGuid()}";

            var serviceClient =  await application.InitGraphServiceClient();

            var outlookTaskFolders = serviceClient.client.Me().Calendars;
            var folder = await outlookTaskFolders.GetCalendar(modelTodo.DefaultCalendarName).WhenNotDefault().FirstAsync();
            await outlookTaskFolders[folder.Id].Request().DeleteAsync();
        }

        [TestCase("CPDx077Z9-sCEPDx077Z9-sCGAU=")]
        [XpandTest()]
        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        public override async Task Populate_All(string syncToken){
            using var application = Platform.Win.CalendarModule().Application;
            var builder = await application.InitializeService(CalendarTestExtensions.PagingCalendarName);
            await builder.frame.View.ObjectSpace.Populate_All(syncToken,
                storage => builder.requestBuilder.Events.ListAllItems(
                    store => store.SaveToken(application.ObjectSpaceProvider.CreateObjectSpace), storage), Timeout,
                tasks => {
                    tasks.Test().ItemCount.ShouldBe(2);
                    tasks.SelectMany(events1 => events1).Test().ItemCount.ShouldBe(CalendarTestExtensions.PagingCalendarItemsCount);
                });
        }

        [Test]
        [XpandTest()]
        public override async Task Populate_Modified(){
            using var application = Platform.Win.CalendarModule().Application;
            var builder = await application.InitializeService();
            await builder.requestBuilder.Me().Calendar.DeleteAllEvents();
            var calendarEvents = (await builder.requestBuilder.Me().Calendar.NewCalendarEvents(12, nameof(Populate_Modified)).FirstAsync()).First();
            await builder.frame.Application.ObjectSpaceProvider
                .Populate_Modified(storage => builder.requestBuilder.Me().CalendarView
                        .ListDelta(storage,application.CreateObjectSpace), Unit.Default.ReturnObservable()
                        .SelectMany(unit => builder.requestBuilder.Me().Events[calendarEvents.Id].Request()
                            .UpdateAsync(new Event(){Subject = "updated"}).ToObservable().Select(_ => _).ToUnit()),
                    Timeout, events => {
                        var testObserver = events.Test();
                        testObserver.Items.Count(_ => _.@event.Subject=="updated").ShouldBe(1);
                    });
        }


        [Test]
        [XpandTest()]
        public override async Task Update_Cloud_Event(){
            using var application = Platform.Win.CalendarModule().Application;
            await application.MSGraphClient(true);
            var updatedEvent = CalendarService.Updated.When(MapAction.Insert).FirstAsync().SubscribeReplay();
            var builder = await application.InitializeService("Calendar");
            application.CreateObjectSpace().GetObjects<DevExpress.Persistent.BaseImpl.Event>().Count.ShouldBe(0);
            var @event = builder.frame.View.ObjectSpace.CreateObject<DevExpress.Persistent.BaseImpl.Event>();
            @event.Subject = nameof(Update_Cloud_Event);
            builder.frame.View.ObjectSpace.CommitChanges();
                
            var serviceObject = (await updatedEvent).cloud;
            application.CreateObjectSpace().GetObjects<DevExpress.Persistent.BaseImpl.Event>().Count.ShouldBe(1);
            await builder.requestBuilder.Me().Events[serviceObject.Id].Request().UpdateAsync(new Event(){Subject = "updated"});
                
            var synchronization = CalendarService.CustomizeSynchronization.FirstAsync(args => args.Instance.mapAction==MapAction.Update).SubscribeReplay();

                
            builder.frame.SetView(application.NewView(ViewType.DetailView, typeof(DevExpress.Persistent.BaseImpl.Event)));

            var tuple = await synchronization;

            tuple.Instance.mapAction.ShouldBe(MapAction.Update);
            var objectSpace = application.CreateObjectSpace();
            objectSpace.GetObject(@event).Subject.ShouldBe("updated");
            objectSpace.GetObjects<DevExpress.Persistent.BaseImpl.Event>().Count.ShouldBe(1);
        }

        [Test]
        [XpandTest()]
        public override async Task Delete_Cloud_Event(){
            using var application = Platform.Win.CalendarModule().Application;
            var client = await application.MSGraphClient(true);
            await client.Me().Calendar.Events.Request().AddAsync(new Event(){Subject = "test"});
            var officeTokenStorage = application.CreateObjectSpace().CloudOfficeToken((Guid) SecuritySystem.CurrentUserId,typeof(Event).FullName,"deltatoken");
            officeTokenStorage.Token.ShouldBeNull();

            var storage = officeTokenStorage;
            var observeInsert = Observable.Start(() => client.Me().CalendarView.ListDelta(storage,application.CreateObjectSpace ).FirstAsync().SubscribeReplay()).Merge();
            await observeInsert;
            var items = observeInsert.ToEnumerable().ToArray();
            items.Count().ShouldBe(1);
            items.Select(_ => _.mapAction).First().ShouldBe(MapAction.Insert);
            officeTokenStorage = application.CreateObjectSpace().CloudOfficeToken((Guid) SecuritySystem.CurrentUserId,typeof(Event).FullName,"deltatoken");
            officeTokenStorage.Token.ShouldNotBeNull();
            var objectSpace1 = application.CreateObjectSpace();
            var o = objectSpace1.CreateObject<DevExpress.Persistent.BaseImpl.Event>();
            objectSpace1.CommitChanges();
            await objectSpace1.NewCloudObject(o, items.First().@event);
            objectSpace1.CommitChanges();
            var synchronization = CalendarService.CustomizeSynchronization.FirstAsync(args => args.Instance.mapAction==MapAction.Delete).SubscribeReplay();
            var builder = await application.InitializeService("Calendar",true);
            await builder.client.Me.Events[items.First().@event.Id].Request().DeleteAsync();

            builder.frame.SetView(application.NewView(ViewType.DetailView, typeof(DevExpress.Persistent.BaseImpl.Event)));
                
            var tuple = await synchronization.Delay(TimeSpan.FromSeconds(1)).ToTaskWithoutConfigureAwait();
            tuple.Instance.mapAction.ShouldBe(MapAction.Delete);
            var objectSpace = application.CreateObjectSpace();
            objectSpace.GetObjectsQuery<DevExpress.Persistent.BaseImpl.Event>().Count().ShouldBe(0);
        }

        [Test]
        [XpandTest()]
        public override async Task Insert_Cloud_Event(){
            using var application = Platform.Win.CalendarModule().Application;
            await application.MSGraphClient(true);
            var builder = await application.InitializeService("Calendar");
            await builder.client.Me.Events.Request().AddAsync(new Event(){Subject = "New"});
            var synchronization = CalendarService.CustomizeSynchronization
                .FirstAsync(args => args.Instance.mapAction == MapAction.Insert).SubscribeReplay();
            builder.frame.SetView(application.NewView(ViewType.DetailView, typeof(DevExpress.Persistent.BaseImpl.Event)));

            var tuple = await synchronization;

            tuple.Instance.mapAction.ShouldBe(MapAction.Insert);
            var objectSpace = application.CreateObjectSpace();
            var objectsQuery = objectSpace.GetObjectsQuery<DevExpress.Persistent.BaseImpl.Event>().ToArray();
            objectsQuery.Length.ShouldBe(1);
            objectsQuery.FirstOrDefault(e => e.Subject == "New").ShouldNotBeNull();
            await objectSpace.QueryCloudOfficeObject(tuple.Instance.cloud.Id, CloudObjectType.Event).FirstAsync()
                .WithTimeOut();
        }

        [Test]
        [XpandTest()]
        public override Task Skip_Authorization_If_Authentication_Storage_Is_Empty(){
	        using var application = Platform.Win.CalendarModule().Application;
	        var observer = application.WhenObjectViewCreated().Test();
	        var exceptions = application.WhenTrace(rxAction: RXAction.OnError).Test();
	        Should.ThrowAsync<TimeoutException>(async () =>
		        await application.InitializeService(newAuthentication: false).Timeout(TimeSpan.FromSeconds(5)));

            observer.ItemCount.ShouldBe(1);
            exceptions.ItemCount.ShouldBe(0);

            return Task.CompletedTask;
        }

    }
}