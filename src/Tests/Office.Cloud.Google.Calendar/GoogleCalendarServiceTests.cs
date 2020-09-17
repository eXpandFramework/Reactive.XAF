using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.Xpo;
using Google.Apis.Calendar.v3.Data;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Office.Cloud.BusinessObjects;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.Office.Cloud.Tests;
using Xpand.XAF.Modules.Reactive;
using Platform = Xpand.Extensions.XAF.XafApplicationExtensions.Platform;


namespace Xpand.XAF.Modules.Office.Cloud.Google.Calendar.Tests{
    [NonParallelizable]
    public class GoogleCalendarServiceTests : BaseCalendarTests{
        
        [Test]
        [XpandTest()]
        public override async Task Map_Two_New_Events(){
            await MapTwoNewEvents(CalendarTestExtensions.CalendarName);
        }
        
        private async Task MapTwoNewEvents(string tasksFolderName,Func<global::Google.Apis.Calendar.v3.CalendarService, IObservable<Unit>> afterAssert=null,bool keepTaskFolder=false){
            using (var application = Platform.Win.CalendarModule().Application){
                var t = await application.InitializeService(tasksFolderName,keepEvents:keepTaskFolder);
                await t.frame.View.ObjectSpace.Map_Two_New_Entity(
                    (space, i) => space.NewEvent(i), Timeout,
                    space => CalendarService.Updated.When(MapAction.Insert).TakeUntilDisposed(application), async (@event, _, i) => {
                        if (afterAssert != null){
                            await afterAssert(t.service);
                        }
                        else{
                            application.ObjectSpaceProvider.AssertEvent(typeof(Event), @event, _.cloud.Description,
                                _.cloud.End.DateTime, _.cloud.Id, @event.Subject);    
                        }
                    });
            }
        }

        [Test]
        [XpandTest()]
        public override async Task Customize_Two_New_Event(){
            using (var application = Platform.Win.CalendarModule().Application){
                var builder = await application.InitializeService();
                await builder.frame.View.ObjectSpace.Map_Two_New_Entity((space,i)=>space.NewEvent(i), Timeout,
                    space => CalendarService.Updated.When(MapAction.Insert).Select(_ => _.cloud)
                        .Merge(CalendarService.CustomizeSynchronization.Where(e => e.Instance.mapAction==MapAction.Insert)
                            .Select((tuple, i) => {
                                tuple.Instance.local.MapCloudEvent(tuple.Instance.cloud);
                                tuple.Instance.cloud.Summary = $"{nameof(Customize_Two_New_Event)}{i}";
                                tuple.Handled = true;
                                return tuple.Instance.cloud;
                            }).IgnoreElements())
                        .TakeUntilDisposed(application), 
                    (cloudEvent, listEntry,i) => {
                        application.ObjectSpaceProvider
                            .AssertEvent(typeof(Event),cloudEvent, listEntry.Summary, listEntry.End.DateTime,  listEntry.Id,$"{nameof(Customize_Two_New_Event)}{i}");
                    });
            
            }
        }

        [Test]
        [XpandTest()]
        public override async Task Map_Existing_Event_Two_Times(){
            using (var application = Platform.Win.CalendarModule().Application){
                var builderData = await application.InitializeService();
                var existingObjects = (await application.CreateExistingObjects(nameof(Map_Existing_Event_Two_Times))).First();
                
                await builderData.frame.View.ObjectSpace.Map_Existing_Entity_Two_Times(existingObjects.local,
                    (pmeTask,i) => pmeTask.Modify_Event( i), existingObjects.cloudEvent
                    , space => CalendarService.Updated.When(MapAction.Update).Select(_ => _.cloud).TakeUntilDisposed(application),
                    (local, cloud) => {
                        cloud.Summary.ShouldBe(local.Subject);
                        return Task.CompletedTask;
                    },Timeout);
            }

        }

        [Test]
        [XpandTest()]
        public override async Task Customize_Map_Existing_Event_Two_Times(){

            using (var application = Platform.Win.CalendarModule().Application){
                var builder = await application.InitializeService();
                var existingObjects = (await application.CreateExistingObjects(nameof(Customize_Map_Existing_Event_Two_Times))).First();
                await builder.frame.View.ObjectSpace.Map_Existing_Entity_Two_Times(existingObjects.local,
                    (pmeTask,i) => pmeTask.Modify_Event( i),existingObjects.cloudEvent, space
                        => CalendarService.Updated.When(MapAction.Update).Select(_ => _.cloud)
                            .Merge(CalendarService.CustomizeSynchronization.Where(e => e.Instance.mapAction==MapAction.Update).Take(2)
                                .Do(_ => {
                                    _.Instance.local.MapCloudEvent(_.Instance.cloud);
                                    _.Instance.cloud.Summary = nameof(Customize_Map_Existing_Event_Two_Times);
                                    _.Handled = true;
                                }).To(default(Event)).IgnoreElements())
                            .TakeUntilDisposed(application),
                    (pmeTask, cloudTask) => {
                        cloudTask.Summary.ShouldBe(nameof(Customize_Map_Existing_Event_Two_Times));
                        return Task.CompletedTask;
                    },Timeout);
            }

        }

        [Test]
        [XpandTest()]
        public override async Task Delete_Two_Events(){
            using (var application = Platform.Win.CalendarModule().Application){
                var t = await application.InitializeService();
                var existingObjects = await application.CreateExistingObjects(nameof(Delete_Two_Events),count:2);
            
                await t.frame.View.ObjectSpace.Delete_Two_Entities(existingObjects.Select(tuple => tuple.local).ToArray(),
                    space => CalendarService.Updated.When(MapAction.Delete).Select(_ => _.cloud).TakeUntilDisposed(application), async () => {
                        var allEvents = await t.service.GetCalendar(CalendarTestExtensions.CalendarName).SelectMany(list => t.service.Events.List(list.Id).ToObservable());
                        allEvents.Items.Count.ShouldBe(0);
                    }, Timeout,existingObjects.Select(_ => _.cloudEvent).ToArray());
            }

        }

        [TestCase(false)]
        [TestCase(true)]
        [XpandTest()]
        public override async Task Customize_Delete_Two_Events(bool handleDeletion){
            using (var application = Platform.Win.CalendarModule().Application){
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
                        var allTasks =await builder.service.GetCalendar(CalendarTestExtensions.CalendarName)
                            .SelectMany(list => builder.service.Events.List(list.Id).ToObservable());
                        allTasks.Items.Count.ShouldBe(handleDeletion ? 2 : 0);
                    }, Timeout,existingObjects.Select(_ => _.cloudEvent).ToArray());
                await deleteTwoEntities;
            }

        }

        [Test]
        [XpandTest()]
        public override async Task Delete_Local_Event_Resource(){
            using (var application = Platform.Win.CalendarModule().Application){
                var t = await application.InitializeService();
                var calendar = await t.service.GetCalendar(CalendarTestExtensions.CalendarName);
                await t.frame.View.ObjectSpace.Delete_Event_Resource(pmeEvent =>
                    CalendarService.Updated.Select(_ => _.local).TakeUntilDisposed(application), async () => {
                    var events = await t.service.ListEvents(calendar.Id);
                    events.First().Items.Count.ShouldBe(1);
                    events.First().Items.First().Attendees.ShouldBeNull();
                }, Timeout);
            }
        }

        [XpandTest()]
        [Test]
        public override async Task Create_Entity_Container_When_Not_Exist(){
            using (var application = Platform.Win.CalendarModule().Application){
                var modelTodo = application.Model.ToReactiveModule<IModelReactiveModuleOffice>().Office.Google().Calendar();
                modelTodo.DefaultCalendarName = $"{nameof(Create_Entity_Container_When_Not_Exist)}{Guid.NewGuid()}";
            
                var t =  await application.InitService();
            
                var service = t.service;
                var calendarListEntry = await service.GetCalendar(modelTodo.DefaultCalendarName).WhenNotDefault().FirstAsync();
                await service.CalendarList.Delete(calendarListEntry.Id).ExecuteAsync();
            }
        }

        [TestCase(null)]
        [TestCase("invalidToken")]
        [XpandTest()]
        public override async Task Populate_All(string syncToken){
            using (var application = Platform.Win.CalendarModule().Application){
                var t = await application.InitializeService(CalendarTestExtensions.CalendarPagingName);
                var calendarId = (await t.service.GetCalendar(CalendarTestExtensions.CalendarPagingName)).Id;
                await t.frame.View.ObjectSpace.Populate_All(syncToken,
                    storage => t.service.ListEvents(calendarId,null,
                        store => store.SaveToken(application.ObjectSpaceProvider.CreateObjectSpace),250), Timeout,
                    events => {
                        var testObserver = events.Test();
                        testObserver.Items.First().Length.ShouldBe(2);
                        testObserver.Items.First().SelectMany(events1 => events1.Items).Count().ShouldBe(CalendarTestExtensions.PagingItemsCount);
                    });
            }
        }

        [Test]
        [XpandTest()]
        public override async Task Populate_Modified(){
            using (var application = Platform.Win.CalendarModule().Application){
                var t = await application.InitializeService();
                var calendarId = (await t.service.GetCalendar(CalendarTestExtensions.CalendarName)).Id;
                await t.service.DeleteAllEvents(calendarId);
                var calendarEvent = (await t.service.NewCalendarEvents(2,calendarId, nameof(Populate_Modified)).FirstAsync()).First();
                await t.frame.Application.ObjectSpaceProvider
                    .Populate_Modified(storage => t.service.ListEvents(calendarId,storage), calendarEvent.ReturnObservable()
                            .Do(cloudEvent => cloudEvent.Description="updated")
                            .SelectMany(unit => t.service.Events.Update(calendarEvent, calendarId,calendarEvent.Id ).ExecuteAsync()).ToUnit() ,
                    Timeout, events => {
                        var testObserver = events.Test();
                        testObserver.Items.First().SelectMany(_ => _.Items).First().Description.ShouldBe("updated");
                    });
            }
        }


        [Test]
        [XpandTest()]
        public override async Task Update_Cloud_Event(){
            using (var application = Platform.Win.CalendarModule().Application){
                await application.CalendarService(true);
                var updatedEvent = CalendarService.Updated.When(MapAction.Insert).FirstAsync().SubscribeReplay();
                var t = await application.InitializeService();
                application.CreateObjectSpace().GetObjects<DevExpress.Persistent.BaseImpl.Event>().Count.ShouldBe(0);
                var @event = t.frame.View.ObjectSpace.NewEvent(subject: nameof(Update_Cloud_Event));
                @event.Subject = nameof(Update_Cloud_Event);
                t.frame.View.ObjectSpace.CommitChanges();
                var serviceObject = (await updatedEvent).cloud;
                serviceObject.Summary = nameof(Update_Cloud_Event);
                application.CreateObjectSpace().GetObjects<DevExpress.Persistent.BaseImpl.Event>().Count.ShouldBe(1);
                var calendar = await t.service.GetCalendar(CalendarTestExtensions.CalendarName);
                await t.service.Events.Update(serviceObject,calendar.Id,serviceObject.Id).ToObservable();
                
                var synchronization = CalendarService.CustomizeSynchronization.FirstAsync(args => args.Instance.mapAction==MapAction.Update).SubscribeReplay();
            
                
                t.frame.SetView(application.NewView(ViewType.DetailView, typeof(DevExpress.Persistent.BaseImpl.Event)));
            
                var tuple = await synchronization.Timeout(Timeout);
            
                tuple.Instance.mapAction.ShouldBe(MapAction.Update);
                var objectSpace = application.CreateObjectSpace();
                objectSpace.GetObject(@event).Subject.ShouldBe(nameof(Update_Cloud_Event));
                objectSpace.GetObjects<DevExpress.Persistent.BaseImpl.Event>().Count.ShouldBe(1);
            }
        }

        [Test]
        [XpandTest()]
        public override async Task Delete_Cloud_Event(){
            using (var application = Platform.Win.CalendarModule().Application){
                var calendarService = await application.CalendarService(true);
                var calendar = await calendarService.GetCalendar(CalendarTestExtensions.CalendarName);
                await calendarService.NewCalendarEvents(1, calendar.Id, "test");
                
                var officeTokenStorage = application.CreateObjectSpace().CloudOfficeTokenStorage((Guid) SecuritySystem.CurrentUserId);
                officeTokenStorage.Token.ShouldBeNull();
                var observeInsert = calendarService.ListEvents(officeTokenStorage, application.CreateObjectSpace,calendar.Id).FirstAsync().Test();
                observeInsert.AwaitDone(Timeout);
                observeInsert.Items.Count.ShouldBe(1);
                observeInsert.Items.Select(_ => _.mapAction).First().ShouldBe(MapAction.Insert);
                application.CreateObjectSpace().GetObjectsQuery<CloudOfficeTokenStorage>();
                officeTokenStorage = application.CreateObjectSpace().CloudOfficeTokenStorage((Guid) SecuritySystem.CurrentUserId);
                officeTokenStorage.Token.ShouldNotBeNull();
                var objectSpace1 = application.CreateObjectSpace();
                var o = objectSpace1.CreateObject<DevExpress.Persistent.BaseImpl.Event>();
                objectSpace1.CommitChanges();
                await objectSpace1.NewCloudObject(o, observeInsert.Items.First().e);
                objectSpace1.CommitChanges();
                var synchronization = CalendarService.CustomizeSynchronization.FirstAsync(args => args.Instance.mapAction==MapAction.Delete).SubscribeReplay();
                var t = await application.InitializeService(keepEvents:true);
                await t.service.Events.Delete(calendar.Id, observeInsert.Items.First().e.Id).ExecuteAsync();

                t.frame.SetView(application.NewView(ViewType.DetailView, typeof(DevExpress.Persistent.BaseImpl.Event)));
                
                var tuple = await synchronization.ToTaskWithoutConfigureAwait();
                tuple.Instance.mapAction.ShouldBe(MapAction.Delete);
                var objectSpace = application.CreateObjectSpace();
                objectSpace.GetObjectsQuery<DevExpress.Persistent.BaseImpl.Event>().Count().ShouldBe(0);
            }
        }

        [Test]
        [XpandTest()]
        public override async Task Insert_Cloud_Event(){
            using (var application = Platform.Win.CalendarModule().Application){
                await application.CalendarService(true);
                var t = await application.InitializeService();
                var calendar = await t.service.GetCalendar(CalendarTestExtensions.CalendarName);
                await t.service.NewCalendarEvents(1, calendar.Id, "New");

                var synchronization = CalendarService.CustomizeSynchronization
                    .FirstAsync(args => args.Instance.mapAction == MapAction.Insert).SubscribeReplay();
                t.frame.SetView(application.NewView(ViewType.DetailView, typeof(DevExpress.Persistent.BaseImpl.Event)));
            
                var tuple = await synchronization;
            
                tuple.Instance.mapAction.ShouldBe(MapAction.Insert);
                var objectSpace = application.CreateObjectSpace();
                var objectsQuery = objectSpace.GetObjectsQuery<DevExpress.Persistent.BaseImpl.Event>().ToArray();
                objectsQuery.Length.ShouldBe(1);
                objectsQuery.FirstOrDefault(e => e.Subject == "New").ShouldNotBeNull();
                await objectSpace.QueryCloudOfficeObject(tuple.Instance.cloud.Id, CloudObjectType.Event).FirstAsync()
                    .WithTimeOut();

            }
        }

    }
}