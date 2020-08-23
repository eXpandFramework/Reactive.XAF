using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base.General;
using Google.Apis.Tasks.v1.Data;
using Shouldly;
using Xpand.Extensions.DateTimeExtensions;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.XAF.Modules.Office.Cloud.Google.Tests;
using Xpand.XAF.Modules.Reactive;
using Platform = Xpand.Extensions.XAF.XafApplicationExtensions.Platform;
using Task = DevExpress.Persistent.BaseImpl.Task;
using TaskStatus = DevExpress.Persistent.Base.General.TaskStatus;

namespace Xpand.XAF.Modules.Office.Cloud.Google.Tasks.Tests{
	static class TasksTestExtensions{
        public const string TasksFolderName = "Xpand Tasks";
        public const string TasksPagingFolderName = "Xpand Paging Tasks";

        public const int TasksFolderPagingItemsCount = 21;

        public static async Task<(global::Google.Apis.Tasks.v1.TasksService service, Frame frame)> InitializeService(this XafApplication application,string taskFolderName=TasksFolderName,bool keepTasks=false,bool keepTaskFolder=false){
            var modelTodo = application.Model.ToReactiveModule<IModelReactiveModuleOffice>().Office.Google().Tasks();
            modelTodo.DefaultTaskListName = taskFolderName;
            var t = await application.InitService();
            var tasklistsResource = t.service.Tasklists;
            
            var taskFolder = await t.service.GetTaskList(taskFolderName, !keepTaskFolder && taskFolderName!=TasksPagingFolderName);
            if (taskFolderName==TasksPagingFolderName){
                taskFolder ??= await tasklistsResource.Insert(new TaskList(){Title = TasksPagingFolderName}).ExecuteAsync();

                var count = (await t.service.ListTasks(taskFolder.Id).SelectMany(tasks => tasks).Select(tasks => tasks.Items.ToArray()).Sum(entities => entities.Length));
                var itemsCount = TasksFolderPagingItemsCount-count;
                if (itemsCount>0){
                    await t.service.NewTaskListTasks(itemsCount,taskFolder.Id, nameof(GoogleTasksModule));
                }
            }
            
            if (taskFolderName != TasksPagingFolderName&&!keepTasks&&!keepTaskFolder){
                await t.service.DeleteAllTasks(taskFolder.Id);
                var listTasks = (await t.service.ListTasks(taskFolder.Id).SelectMany(tasks => tasks));
                listTasks.Items.Count.ShouldBe(0);
            }
            
            return (t.service,t.frame);
        }
        public static void Modify_Task<TTask>(this TTask task, TaskStatus projectTaskStatus, int i) where TTask:ITask{
            task.Subject = $"{nameof(Modify_Task)}{i}";
            task.Status=projectTaskStatus;
        }
        
        public static async Task<(Frame frame, global::Google.Apis.Tasks.v1.TasksService service)> InitService(this XafApplication application){
            application.ObjectSpaceProvider.NewAuthentication();
            var todoModel = await application.ReactiveModulesModel().Office().Google().Tasks();
            var window = application.CreateViewWindow();
            var service = GoogleTasksService.Credentials.FirstAsync().SubscribeReplay();
            window.SetView(application.NewView(todoModel.Items.Select(item => item.ObjectView).First()));
            return (await service.Select(t => (t.frame,t.credential.NewService<global::Google.Apis.Tasks.v1.TasksService>())).ToTaskWithoutConfigureAwait());
        }

        public static GoogleTasksModule TasksModule(this Platform platform,params ModuleBase[] modules){
            var application = NewApplication(platform,  modules);
            application.SetupGoogleSecurity(platform);
            var module = application.AddModule<GoogleTasksModule>(typeof(Task));
            application.Model.ConfigureGoogle(platform);
            var todoModel = application.Model.ToReactiveModule<IModelReactiveModuleOffice>().Office.Google().Tasks();
            var dependency = todoModel.Items.AddNode<IModelTasksItem>();
            dependency.ObjectView = application.Model.BOModel.GetClass(typeof(Task)).DefaultDetailView;
            application.Logon();
            application.CreateObjectSpace();
            return module.Application.Modules.OfType<GoogleTasksModule>().First();  
        }

        static XafApplication NewApplication(this Platform platform,  ModuleBase[] modules){
            var xafApplication = platform.NewApplication<GoogleTasksModule>();
            xafApplication.Modules.AddRange(modules);
            return xafApplication;
        }

        public static void AssertTask(this IObjectSpaceProvider objectSpaceProvider, Type cloudEntityType, Task task,
            string title, string notes, DateTime? due, string expectedStatus, string actualStatus, string taskId,
            string localTaskSubject){
            title.ShouldBe(localTaskSubject);
            notes.ShouldBe(task.Description);
            due.ShouldNotBeNull();
            actualStatus.ShouldBe(expectedStatus);

            using (var space = objectSpaceProvider.CreateObjectSpace()){
                var cloudObjects = space.QueryCloudOfficeObject(cloudEntityType,task).ToArray();
                cloudObjects.Length.ShouldBe(1);
                var cloudObject = cloudObjects.First();
                cloudObject.LocalId.ShouldBe(task.Oid.ToString());
                cloudObject.CloudId.ShouldBe(taskId);
            }
        }

        public static async Task<IList<(Task task, global::Google.Apis.Tasks.v1.Data.Task cloudTask)>> CreateExistingObjects(
            this XafApplication application, string title, TaskStatus taskStatus = TaskStatus.InProgress,int count=1){
            var credential =await application.AuthorizeTestGoogle();
            var tasksService = credential.NewService<global::Google.Apis.Tasks.v1.TasksService>();
            var folder = await tasksService.GetTaskList(TasksFolderName,true);
            await tasksService.DeleteAllTasks(folder.Id);
            return await Observable.Range(0, count).SelectMany(i => tasksService.NewTaskListTasks(1,folder.Id,title)
                .Select((cloudTask,i1) => (application.NewTask(taskStatus,i1), cloudTask))).Buffer(count);
        }

        public static Task NewTask(this XafApplication application, TaskStatus taskStatus,int index=0) {
            using (var objectSpace = application.CreateObjectSpace()){
                var task = objectSpace.NewTask(taskStatus,index);
                objectSpace.CommitChanges();
                return task;
            }
        }
        public static Task NewTask(this IObjectSpace objectSpace, TaskStatus taskStatus,int index=0) {
            var task = objectSpace.CreateObject<Task>();
            task.Subject = $"Subject{index}";
            task.Description = "Description";
            task.StartDate=DateTime.Now.AddMinutes(1);
            // task.DueDate=DateTime.Now.AddDays(2);
            task.Status=taskStatus;
            return task;
        }
        
        public static IObservable<global::Google.Apis.Tasks.v1.Data.Task> NewTaskListTasks(this global::Google.Apis.Tasks.v1.TasksService tasksService,int count,string tasksListId,string title){
            var dateTime = DateTime.Now;
            return Observable.Range(0, count).SelectMany(i => Observable.FromAsync(() => {
                var task = new global::Google.Apis.Tasks.v1.Data.Task(){
                    Title =i>0?$"{title}{i}": title??i.ToString(),
                    Completed = dateTime.AddHours(i).ToRfc3339String()
                };
                return tasksService.Tasks.Insert(task, tasksListId).ExecuteAsync();
            }));
        }
        
    }
}