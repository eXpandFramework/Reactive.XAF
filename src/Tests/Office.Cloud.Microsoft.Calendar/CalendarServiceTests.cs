using System;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using Microsoft.Graph;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.Office.Cloud.Tests;
using Xpand.XAF.Modules.Reactive;
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
            using (var application = Platform.Win.CalendarModule().Application){
                var builder = await application.InitializeService(tasksFolderName,keepTaskFolder:keepTaskFolder);
                await builder.frame.View.ObjectSpace.Map_Two_New_Entity(
                    (space, i) => space.NewEvent(i), Timeout,
                    space => CalendarService.Updated.TakeUntilDisposed(application), async (task, _, i) => {
                        if (afterAssert != null){
                            await afterAssert(builder.requestBuilder);
                        }
                        else{
                            application.ObjectSpaceProvider.AssertEvent(typeof(OutlookTask), task, _.serviceObject.Subject,
                                DateTime.Parse(_.serviceObject.End.DateTime, CultureInfo.InvariantCulture),
                                 _.serviceObject.Id, task.Subject);    
                        }
                            
                    });
            }
        }

        [Test][XpandTest()]
        public override async Task Customize_Two_New_Event(){
            using (var application = Platform.Win.CalendarModule().Application){
                var builder = await application.InitializeService();
                await builder.frame.View.ObjectSpace.Map_Two_New_Entity((space,i)=>space.NewEvent(i), Timeout,
                    space => CalendarService.Updated.Select(_ => _.serviceObject).Merge(CalendarService.CustomizeInsert
                        .Select((tuple, i) => {
                            tuple.cloudEvent.Subject = $"{nameof(Customize_Two_New_Event)}{i}";
                            return default(Event);
                        }).IgnoreElements()).TakeUntilDisposed(application).Select(task => task), 
                    (cloudEvent, taskListEntry,i) => {
                        application.ObjectSpaceProvider.AssertEvent(typeof(Event),cloudEvent, taskListEntry.Subject, DateTime.Parse(taskListEntry.End.DateTime),  taskListEntry.Id,$"{nameof(Customize_Two_New_Event)}{i}");
                        
                    });
            
            }
        }

        [Test][XpandTest()]
        public override async Task Map_Existing_Event_Two_Times(){
            using (var application = Platform.Win.CalendarModule().Application){
                var builderData = await application.InitializeService();
                var existingObjects = (await application.CreateExistingObjects(nameof(Map_Existing_Event_Two_Times))).First();

                await builderData.frame.View.ObjectSpace.Map_Existing_Entity_Two_Times(existingObjects.local,
                    (pmeTask,i) => pmeTask.Modify_Event( i), existingObjects.cloud
                    , space => CalendarService.Updated.Select(_ => _.serviceObject).TakeUntilDisposed(application),
                    (task, outlookTask) => {
                        outlookTask.Subject.ShouldBe(task.Subject);
                        return Task.CompletedTask;
                    },Timeout);
            }

        }

        [Test][XpandTest()]
        public override async Task Customize_Map_Existing_Event_Two_Times(){
            using (var application = Platform.Win.CalendarModule().Application){
                var builder = await application.InitializeService();
                var existingObjects = (await application.CreateExistingObjects(nameof(Customize_Map_Existing_Event_Two_Times))).First();
                await builder.frame.View.ObjectSpace.Map_Existing_Entity_Two_Times(existingObjects.local,
                    (pmeTask,i) => pmeTask.Modify_Event( i),existingObjects.cloud, space
                        => CalendarService.Updated.Select(_ => _.serviceObject).Merge(CalendarService.CustomizeUpdate.Take(2)
                                .Do(_ => _.cloudEvent.Subject = nameof(Customize_Map_Existing_Event_Two_Times)).To(default(Event)).IgnoreElements())
                            .TakeUntilDisposed(application),
                    (pmeTask, cloudTask) => {
                        cloudTask.Subject.ShouldBe(nameof(Customize_Map_Existing_Event_Two_Times));
                        return Task.CompletedTask;
                    },Timeout);
            }

        }

        [Test][XpandTest()]
        public override async Task Delete_Two_Events(){
            using (var application = Platform.Win.CalendarModule().Application){
                var builder = await application.InitializeService();
                var existingObjects = await application.CreateExistingObjects(nameof(Delete_Two_Events),count:2);

                await builder.frame.View.ObjectSpace.Delete_Two_Entities(existingObjects.Select(tuple => tuple.local).ToArray(),
                    space => CalendarService.Updated.Select(_ => _.serviceObject).TakeUntilDisposed(application), async () => {
                        var allTasks = await builder.requestBuilder.Events.ListAllItems();
                        allTasks.Length.ShouldBe(0);
                    }, Timeout,existingObjects.Select(_ => _.cloud).ToArray());
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
                    objectSpace => CalendarService.CustomizeDelete.Take(2)
                        .Do(_ => _.Handled=handleDeletion).To(default(Event))
                        .TakeUntilDisposed(application),
                    async () => {
                        var allTasks = await builder.requestBuilder.Events.ListAllItems();
                        allTasks.Length.ShouldBe(handleDeletion?2:0);
                    }, Timeout,existingObjects.Select(_ => _.cloud).ToArray());
                await deleteTwoEntities;
            }

        }

        [Test][XpandTest()]
        public override async Task Delete_Event_resource(){
            using (var application = Platform.Win.CalendarModule().Application){
                var builder = await application.InitializeService();

                await builder.frame.View.ObjectSpace.Delete_Event_Resource(pmeEvent =>
                    CalendarService.Updated.Select(_ => _.serviceObject).TakeUntilDisposed(application), async () => {
                    var events = await builder.requestBuilder.Events.Request().GetAsync();
                    events.Count.ShouldBe(1);
                    events.First().Attendees.Count().ShouldBe(0);
                }, Timeout);
            }
        }

        [TestCase(null)][XpandTest()]
        public override async Task Populate_All(string syncToken){
            using (var application = Platform.Win.CalendarModule().Application){
                var builder = await application.InitializeService(CalendarTestExtensions.PagingCalendarName);
                await builder.frame.View.ObjectSpace.Populate_All(syncToken,
                    storage => builder.requestBuilder.Events.ListAllItems(
                        store => store.SaveToken(application.ObjectSpaceProvider.CreateObjectSpace), storage), Timeout,
                    tasks => {
                        tasks.Test().ItemCount.ShouldBe(2);
                        tasks.SelectMany(events1 => events1).Test().ItemCount.ShouldBe(CalendarTestExtensions.PagingCalendarItemsCount);
                    });
            }
        }

        [Test][XpandTest()]
        public override async Task Populate_Modified(){
            using (var application = Platform.Win.CalendarModule().Application){
                var builder = await application.InitializeService();
                await builder.requestBuilder.Me().Calendar.DeleteAllEvents();
                var calendarEvents = (await builder.requestBuilder.Me().Calendar.NewCalendarEvents(12, nameof(Populate_Modified)).FirstAsync()).First();
                await builder.frame.Application.ObjectSpaceProvider.Populate_Modified(storage => builder.requestBuilder.Me().CalendarView.ListDelta(storage,application.CreateObjectSpace),
                    Unit.Default.ReturnObservable().Select(unit => unit)
                        .SelectMany(unit => builder.requestBuilder.Me().Events[calendarEvents.Id].Request().UpdateAsync(new Event(){Subject = "updated"}).ToObservable().Select(_ => _).ToUnit()),
                    Timeout,
                    events => {
                        var testObserver = events.SelectMany(events1 => events1).Test();
                        testObserver.ItemCount.ShouldBeLessThan(12);
                        testObserver.Items.Any(_ => _.Subject=="updated").ShouldBeTrue();
                    });
            }
        }

        [XpandTest()]
        [Test]
        public override async Task Create_Entity_Container_When_Not_Exist(){
            using (var application = Platform.Win.CalendarModule().Application){
                var modelTodo = application.Model.ToReactiveModule<IModelReactiveModuleOffice>().Office.Microsoft().Calendar();
                modelTodo.DefaultCaledarName = $"{nameof(Create_Entity_Container_When_Not_Exist)}{Guid.NewGuid()}";

                var serviceClient = await application.InitGraphServiceClient();

                var outlookTaskFolders = serviceClient.client.Me().Calendars;
                var folder = await outlookTaskFolders.GetCalendar(modelTodo.DefaultCaledarName).WhenNotDefault().FirstAsync();
                await outlookTaskFolders[folder.Id].Request().DeleteAsync();
            }

            
            
        }


    }
}