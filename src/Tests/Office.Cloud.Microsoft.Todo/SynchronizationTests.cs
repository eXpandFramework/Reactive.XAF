using System;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using Microsoft.Graph;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Office.Cloud.Microsoft;
using Xpand.Extensions.Reactive.Transform;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.Office.Cloud.Tests;
using Platform = Xpand.Extensions.XAF.XafApplication.Platform;
using TaskStatus = DevExpress.Persistent.Base.General.TaskStatus;


namespace Xpand.XAF.Modules.Office.Cloud.Microsoft.Todo.Tests{
    [NonParallelizable]
    public class SynchronizationTests : BaseTaskTests{
        [TestCase(TaskStatus.NotStarted,nameof(TaskStatus.NotStarted))]
        // [TestCase(TaskStatus.Completed,nameof(TaskStatus.Completed))]
        // [XpandTest()]
        public override async Task Map_Two_New_Tasks(TaskStatus projectTaskStatus, string taskStatus){
            await MapTwoNewTasks(projectTaskStatus, taskStatus,TodoTestExtensions.TasksFolderName);
        }

        private async Task MapTwoNewTasks(TaskStatus projectTaskStatus, string taskStatus, string tasksFolderName,Action<IOutlookTaskFolderRequestBuilder> afterAssert=null){
            using (var application = Platform.Win.TodoModule(nameof(Map_Two_New_Tasks)).Application){
                (IOutlookTaskFolderRequestBuilder requestBuilder, Frame frame) builder = await application.InitializeService(tasksFolderName);
                await builder.frame.View.ObjectSpace.Map_Two_New_Entity(space => space.NewTask(projectTaskStatus), Timeout,
                    space => TodoService.Updated,
                    (pmeTask, outlookTask) => {
                        application.ObjectSpaceProvider.AssertTask(typeof(OutlookTask), pmeTask, outlookTask.Subject,
                            outlookTask.Body.Content,
                            DateTime.Parse(outlookTask.DueDateTime.DateTime, CultureInfo.InvariantCulture), taskStatus,
                            $"{outlookTask.Status}", outlookTask.Id);
                    });
                afterAssert?.Invoke(builder.requestBuilder);
            }
        }

        // [Test][XpandTest()]
        public override async Task Customize_Two_New_Tasks(){
            using (var application = Platform.Win.TodoModule(nameof(Customize_Two_New_Tasks)).Application){
                var builder = await application.InitializeService();
                await builder.frame.View.ObjectSpace.Map_Two_New_Entity(space =>space.NewTask(TaskStatus.Completed), Timeout,
                    space => TodoService.Updated.Merge(TodoService.CustomizeInsert.Take(2)
                        .Do(_ => _.outlookTask.Subject = nameof(Customize_Two_New_Tasks)).To(default(OutlookTask)).IgnoreElements()), 
                    (pmeTask, taskListEntry) => {
                        pmeTask.Subject = nameof(Customize_Two_New_Tasks);
                        application.ObjectSpaceProvider.AssertTask(typeof(OutlookTask),pmeTask, taskListEntry.Subject, taskListEntry.Body.Content, DateTime.Parse(taskListEntry.DueDateTime.DateTime),
                            "Completed", taskListEntry.Status.ToString(), taskListEntry.Id);
                    });
            
            }
        }

        // [TestCase(TaskStatus.NotStarted,nameof(TaskStatus.NotStarted))]
        // [TestCase(TaskStatus.Completed,nameof(TaskStatus.Completed))]
        // [XpandTest()]
        public override async Task Map_Existing_Two_Times(TaskStatus projectTaskStatus, string taskStatus){
            using (var application = Platform.Win.TodoModule(nameof(Map_Existing_Two_Times)).Application){
                var builderData = await application.InitializeService();
                var existingObjects = (await application.CreateExistingObjects(nameof(Map_Existing_Two_Times), projectTaskStatus)).First();

                await builderData.frame.View.ObjectSpace.Map_Existing_Entity_Two_Times(existingObjects.task,
                    (pmeTask,i) => pmeTask.Modify_Task( projectTaskStatus,i), existingObjects.outlookTask
                    , space => TodoService.Updated,
                    (task, outlookTask) => {
                        outlookTask.Subject.ShouldBe(task.Subject);
                        outlookTask.Status.ToString().ShouldBe(taskStatus);
                        return Task.CompletedTask;
                    },Timeout);
            }
        }
        
        // [Test][XpandTest()]
        public override async Task Customize_Existing_Two_Times(){
            using (var application = Platform.Win.TodoModule(nameof(Customize_Existing_Two_Times)).Application){
                var builder = await application.InitializeService();
                var existingObjects = (await application.CreateExistingObjects(nameof(Customize_Existing_Two_Times))).First();
                await builder.frame.View.ObjectSpace.Map_Existing_Entity_Two_Times(existingObjects.task,
                    (pmeTask,i) => pmeTask.Modify_Task( TaskStatus.Completed, i),existingObjects.outlookTask, space
                        => TodoService.Updated.Merge(TodoService.CustomizeUpdate.Take(2)
                            .Do(_ => _.outlookTask.Subject = nameof(Customize_Existing_Two_Times)).To(default(OutlookTask)).IgnoreElements()),
                    (pmeTask, cloudTask) => {
                        cloudTask.Subject.ShouldBe(nameof(Customize_Existing_Two_Times));
                        cloudTask.Status.ShouldBe(global::Microsoft.Graph.TaskStatus.Completed);
                        return Task.CompletedTask;
                    },Timeout);
            }
        }

        // [Test][XpandTest()]
        public override async Task Delete_Two_Tasks(){
            using (var application = Platform.Win.TodoModule(nameof(Delete_Two_Tasks)).Application){
                var builder = await application.InitializeService();
                var existingObjects = await application.CreateExistingObjects(nameof(Delete_Two_Tasks),count:2);

                await builder.frame.View.ObjectSpace.Delete_Two_Entities(existingObjects.Select(tuple => tuple.task).ToArray(),
                    space => TodoService.Updated, async () => {
                        var allTasks = await builder.requestBuilder.Tasks.ListAllItems();
                        allTasks.Length.ShouldBe(0);
                    }, Timeout,existingObjects.Select(_ => _.outlookTask).ToArray());
            }
        }
        
        // [TestCase(true)]
        // [TestCase(false)]
        // [XpandTest()]
        public override async Task Customize_Delete_Two_Tasks(bool handleDeletion){
            using (var application = Platform.Win.TodoModule(nameof(Customize_Delete_Two_Tasks)).Application){
                var builder = await application.InitializeService();
                var existingObjects = await application.CreateExistingObjects(nameof(Customize_Delete_Two_Tasks),count:2);
                var deleteTwoEntities = builder.frame.View.ObjectSpace.Delete_Two_Entities(existingObjects.Select(_ => _.task).ToArray(),
                    objectSpace => TodoService.Updated.Merge(TodoService.CustomizeDelete.Take(2)
                        .Do(_ => _.Handled=handleDeletion).To(default(OutlookTask)).IgnoreElements()),
                    async () => {
                        var allTasks = await builder.requestBuilder.Tasks.ListAllItems();
                        allTasks.Length.ShouldBe(0);
                    }, Timeout,existingObjects.Select(_ => _.outlookTask).ToArray());
                if (handleDeletion){
                    await deleteTwoEntities.ShouldThrowAsync<TimeoutException>();
                }
                else{
                    await deleteTwoEntities;
                }
            }
        }


        [TestCase(null)][XpandTest()]
        public override async Task Populate_All(string syncToken){
            using (var application = Platform.Win.TodoModule(nameof(Populate_All)).Application){
                var builder = await application.InitializeService(TodoTestExtensions.TasksPagingFolderName);
                await builder.frame.View.ObjectSpace.Populate_All(syncToken,
                    storage => builder.requestBuilder.Tasks.ListAllItems(
                        store => store.SaveToken(application.ObjectSpaceProvider.CreateObjectSpace), storage), Timeout,
                    tasks => {
                        tasks.Test().ItemCount.ShouldBe(2);
                        tasks.SelectMany(events1 => events1).Test().ItemCount.ShouldBe(TodoTestExtensions.TasksFolderPagingItemsCount);
                    });
            }
        }

        public override Task Populate_Modified(){
            throw new NotImplementedException();
        }

        // [XpandTest()]
        public override async Task Create_Entity_Container_When_Not_Exist(){
            var tasksFolderName = Guid.NewGuid().ToString();
            await MapTwoNewTasks(TaskStatus.InProgress,TaskStatus.InProgress.ToString() ,tasksFolderName);
        }

        // [XpandTest()]
        public override Task User_Container(){
            throw new NotImplementedException();
        }
    }
}