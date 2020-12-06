using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.TaskExtensions;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Office.Cloud.Tests;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Logger;
using Xpand.XAF.Modules.Reactive.Services;
using Platform = Xpand.Extensions.XAF.XafApplicationExtensions.Platform;
using TaskStatus = DevExpress.Persistent.Base.General.TaskStatus;


namespace Xpand.XAF.Modules.Office.Cloud.Google.Tasks.Tests{
    [NonParallelizable]
    public class GoogleTasksServiceTests : CommonTaskTests{
        [TestCase(TaskStatus.NotStarted,"needsAction")]
        [TestCase(TaskStatus.Completed,"completed")]
        [XpandTest()]
        public override async Task Map_Two_New_Tasks(TaskStatus projectTaskStatus, string taskStatus){
            await MapTwoNewTasks(projectTaskStatus, taskStatus,TasksTestExtensions.TasksFolderName);
        }

        private async Task MapTwoNewTasks(TaskStatus projectTaskStatus, string expectedStatus, string tasksFolderName,Func<global::Google.Apis.Tasks.v1.TasksService,IObservable<Unit>> afterAssert=null,bool keepTaskFolder=false){
            using var application = Platform.Win.TasksModule().Application;
            var builder = await application.InitializeService(tasksFolderName,keepTaskFolder:keepTaskFolder);
            await builder.frame.View.ObjectSpace.Map_Two_New_Entity(
                (space, i) => space.NewTask(projectTaskStatus, i), Timeout,
                space => GoogleTasksService.Updated.TakeUntilDisposed(application), async (task, _, i) => {
                    if (afterAssert != null){
                        await afterAssert(builder.service);
                    }
                    else{
                        application.ObjectSpaceProvider.AssertTask(typeof(global::Google.Apis.Tasks.v1.Data.Task), task, _.cloud.Title,
                            _.cloud.Notes, DateTime.Parse(_.cloud.Due), expectedStatus, $"{_.cloud.Status}", _.cloud.Id, task.Subject);    
                    }
                            
                });
        }

        [Test]
        [XpandTest()]
        public override async Task Customize_Two_New_Tasks(){
            using var application = Platform.Win.TasksModule().Application;
            var builder = await application.InitializeService();
            await builder.frame.View.ObjectSpace.Map_Two_New_Entity((space,i)=>space.NewTask(TaskStatus.NotStarted,i), Timeout,
                space => GoogleTasksService.Updated.Select(_ => _.cloud).Merge(GoogleTasksService.CustomizeSynchronization.When(MapAction.Insert)
                    .Select((tuple, i) => {
                        tuple.Instance.cloud.Title = $"{nameof(Customize_Two_New_Tasks)}{i}";
                        tuple.Handled=true;
                        return default(global::Google.Apis.Tasks.v1.Data.Task);
                    }).IgnoreElements()).TakeUntilDisposed(application), 
                (local, cloud,i) => {
                    cloud.Title.ShouldBe($"{nameof(Customize_Two_New_Tasks)}{i}");
                });
        }

        [TestCase(TaskStatus.NotStarted,"needsAction")]
        [TestCase(TaskStatus.Completed,"completed")]
        [XpandTest()]
        public override async Task Map_Existing_Two_Times(TaskStatus projectTaskStatus, string taskStatus){
            using var application = Platform.Win.TasksModule().Application;
            var builderData = await application.InitializeService();
            var existingObjects = (await application.CreateExistingObjects(nameof(Map_Existing_Two_Times), projectTaskStatus)).First();

            await builderData.frame.View.ObjectSpace.Map_Existing_Entity_Two_Times(existingObjects.task,
                (pmeTask,i) => pmeTask.Modify_Task( projectTaskStatus,i), existingObjects.cloudTask
                , space => GoogleTasksService.Updated.When(MapAction.Update).Select(_ => _.cloud).TakeUntilDisposed(application),
                (localTask, cloudTask) => {
                    cloudTask.Title.ShouldBe(localTask.Subject);
                    cloudTask.Status.ToString().ShouldBe(taskStatus);
                    return Task.CompletedTask;
                },Timeout);
        }
        
        [Test][XpandTest()]
        public override async Task Customize_Existing_Two_Times(){
            using var application = Platform.Win.TasksModule().Application;
            var builder = await application.InitializeService();
            var existingObjects = (await application.CreateExistingObjects(nameof(Customize_Existing_Two_Times))).First();
            await builder.frame.View.ObjectSpace.Map_Existing_Entity_Two_Times(existingObjects.task,
                (pmeTask,i) => pmeTask.Modify_Task( TaskStatus.Completed, i),existingObjects.cloudTask, space
                    => GoogleTasksService.Updated.When(MapAction.Update).Select(_ => _.cloud)
                        .Merge(GoogleTasksService.CustomizeSynchronization.When(MapAction.Update).Take(2)
                            .Do(_ => {
                                _.Instance.cloud.Title = nameof(Customize_Existing_Two_Times);
                                _.Handled = true;
                            }).To(default(global::Google.Apis.Tasks.v1.Data.Task)).IgnoreElements())
                        .TakeUntilDisposed(application),
                (pmeTask, cloudTask) => {
                    cloudTask.Title.ShouldBe(nameof(Customize_Existing_Two_Times));
                    return Task.CompletedTask;
                },Timeout);
        }

        [Test]
        [XpandTest()]
        public override async Task Delete_Two_Tasks(){
            using var application = Platform.Win.TasksModule().Application;
            var builder = await application.InitializeService();
            var existingObjects = await application.CreateExistingObjects(nameof(Delete_Two_Tasks),count:2);

            await builder.frame.View.ObjectSpace.Delete_Two_Entities(existingObjects.Select(tuple => tuple.task).ToArray(),
                space => GoogleTasksService.Updated.When(MapAction.Delete).Select(_ => _.cloud).TakeUntilDisposed(application), async () => {
                    global::Google.Apis.Tasks.v1.Data.Tasks allTasks =await builder.service.GetTaskList(TasksTestExtensions.TasksFolderName).SelectMany(list => builder.service.Tasks.List(list.Id).ToObservable());
                    allTasks.Items.ShouldBeNull();
                }, Timeout,existingObjects.Select(_ => _.cloudTask).ToArray());
        }
        
        [TestCase(true)]
        [TestCase(false)]
        [XpandTest()]
        public override async Task Customize_Delete_Two_Tasks(bool handleDeletion){
            using var application = Platform.Win.TasksModule().Application;
            var builder = await application.InitializeService();
            var existingObjects = await application.CreateExistingObjects(nameof(Customize_Delete_Two_Tasks),count:2);
            var deleteTwoEntities = builder.frame.View.ObjectSpace.Delete_Two_Entities(existingObjects.Select(_ => _.task).ToArray(),
                objectSpace => GoogleTasksService.CustomizeSynchronization.When(MapAction.Delete).Take(2)
                    .Do(_ => _.Handled=handleDeletion).To(default(global::Google.Apis.Tasks.v1.Data.Task))
                    .TakeUntilDisposed(application),
                async () => {
                    var allTasks =await builder.service.GetTaskList(TasksTestExtensions.TasksFolderName)
                        .SelectMany(list => builder.service.Tasks.List(list.Id).ToObservable());
                    if (handleDeletion){
                        allTasks.Items.Count.ShouldBe(2);
                    }
                    else{
                        allTasks.Items.ShouldBeNull();
                    }
                }, Timeout,existingObjects.Select(_ => _.cloudTask).ToArray());
            await deleteTwoEntities;
        }


        [TestCase(null)]
        [XpandTest()]
        public override async Task Populate_All(string syncToken){
            using var application = Platform.Win.TasksModule().Application;
            var builder = await application.InitializeService(TasksTestExtensions.TasksPagingFolderName);
            var taskListId = (await builder.service.GetTaskList(TasksTestExtensions.TasksPagingFolderName)).Id;
            await builder.frame.View.ObjectSpace.Populate_All(syncToken,
                storage => builder.service.ListTasks(taskListId,null,
                    store => store.SaveToken(application.ObjectSpaceProvider.CreateObjectSpace),20), Timeout,
                tasks => {
                    tasks.Test().Items.First().Length.ShouldBe(2);
                    tasks.Test().Items.First().SelectMany(events1 => events1.Items).Count().ShouldBe(TasksTestExtensions.TasksFolderPagingItemsCount);
                });
        }

        public override Task Populate_Modified(){
            throw new NotImplementedException();
        }

        [XpandTest()]
        [Test]
        public override async Task Create_Entity_Container_When_Not_Exist(){
            using var application = Platform.Win.TasksModule().Application;
            var modelTodo = application.Model.ToReactiveModule<IModelReactiveModuleOffice>().Office.Google().Tasks();
            modelTodo.DefaultTaskListName = $"{nameof(Create_Entity_Container_When_Not_Exist)}{Guid.NewGuid()}";

            var serviceClient = await application.InitService();

            var service = serviceClient.service;
            var folder = await service.GetTaskList(modelTodo.DefaultTaskListName).WhenNotDefault().FirstAsync();
            await service.Tasklists.Delete(folder.Id).ToObservable();
        }

        [Test]
        [XpandTest()]
        public override Task Skip_Authorization_If_Authentication_Storage_Is_Empty(){
	        using var application = Platform.Win.TasksModule().Application;
	        var observer = application.WhenObjectViewCreated().Test();

	        var exceptions = application.WhenTraceError().Test();
	        Should.ThrowAsync<TimeoutException>(async () =>
		        await application.InitializeService(newAuthentication: false).Timeout(TimeSpan.FromSeconds(5)));

	        observer.ItemCount.ShouldBe(1);
	        exceptions.ItemCount.ShouldBe(0);
	        return Task.CompletedTask;
        }

    }
}