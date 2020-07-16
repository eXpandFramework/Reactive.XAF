using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base.General;
using Microsoft.Graph;
using Shouldly;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.XAF.Modules.Office.Cloud.Microsoft.Tests;
using Xpand.XAF.Modules.Reactive;
using Platform = Xpand.Extensions.XAF.XafApplicationExtensions.Platform;
using Task = DevExpress.Persistent.BaseImpl.Task;
using TaskStatus = DevExpress.Persistent.Base.General.TaskStatus;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft.Todo.Tests{
	static class TodoTestExtensions{
        public const string TasksFolderName = "Xpnad Tasks";
        public const string TasksPagingFolderName = "Xpand Paging Tasks";

        public const int TasksFolderPagingItemsCount = 11;

        public static async Task<(IOutlookTaskFolderRequestBuilder requestBuilder, Frame frame)> InitializeService(this XafApplication application,string taskFolderName=TasksFolderName,bool keepTasks=false,bool keepTaskFolder=false){
            var modelTodo = application.Model.ToReactiveModule<IModelReactiveModuleOffice>().Office.Microsoft().Todo();
            modelTodo.DefaultTodoListName = taskFolderName;
            var client = await application.InitGraphServiceClient();
            var foldersRequestBuilder = client.client.Me.Outlook.TaskFolders;
            var taskFolder = await foldersRequestBuilder.GetFolder(taskFolderName, !keepTaskFolder && taskFolderName!=TasksPagingFolderName);
            if (taskFolder == null&&taskFolderName==TasksPagingFolderName){
                taskFolder = await foldersRequestBuilder.Request()
                    .AddAsync(new OutlookTaskFolder(){Name = TasksPagingFolderName});
                await foldersRequestBuilder[taskFolder.Id].NewFolderTasks(TasksFolderPagingItemsCount, nameof(MicrosoftTodoModule));
            }
            var requestBuilder = foldersRequestBuilder[taskFolder?.Id];
            if (taskFolderName != TasksPagingFolderName&&!keepTasks&&!keepTaskFolder){
                await requestBuilder.DeleteAllTasks();
                (await requestBuilder.Tasks.ListAllItems()).Length.ShouldBe(0);
            }
            
            return (requestBuilder,client.frame);
        }
        public static void Modify_Task<TTask>(this TTask task, TaskStatus projectTaskStatus, int i) where TTask:ITask{
            task.Subject = $"{nameof(Modify_Task)}{i}";
            task.Status=projectTaskStatus;
        }
        
        public static async Task<(Frame frame, GraphServiceClient client)> InitGraphServiceClient(this XafApplication application){
            application.ObjectSpaceProvider.NewMicrosoftAuthentication();
            var todoModel = await application.ReactiveModulesModel().OfficeModel().MicrosoftModel().TodoModel();
            var window = application.CreateViewWindow();
            window.SetView(application.NewView(todoModel.ObjectViews().First().ObjectView));
            var client = await TodoService.Client.FirstAsync().ToTaskWithoutConfigureAwait();
            return client;
        }

        public static MicrosoftTodoModule TodoModule(this Platform platform,params ModuleBase[] modules){
            var application = NewApplication(platform,  modules);
            application.SetupSecurity();
            var module = application.AddModule<MicrosoftTodoModule>(typeof(Task));
            application.Model.ConfigureMicrosoft();
            var todoModel = application.Model.ToReactiveModule<IModelReactiveModuleOffice>().Office.Microsoft().Todo();
            var dependency = ((IModelObjectViews) todoModel).ObjectViews.AddNode<IModelObjectViewDependency>();
            dependency.ObjectView = application.Model.BOModel.GetClass(typeof(Task)).DefaultDetailView;
            application.Logon();
            application.CreateObjectSpace();
            return module.Application.Modules.OfType<MicrosoftTodoModule>().First();  
        }

        static XafApplication NewApplication(this Platform platform,  ModuleBase[] modules){
            var xafApplication = platform.NewApplication<MicrosoftTodoModule>();
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

        public static async Task<IList<(Task task, OutlookTask outlookTask)>> CreateExistingObjects(
            this XafApplication application, string title, TaskStatus taskStatus = TaskStatus.InProgress,int count=1){
            var builder =await application.AuthorizeTestMS();
            var folder = await builder.Me.Outlook.TaskFolders.GetFolder(TasksFolderName, true);
            await builder.Me.Outlook.TaskFolders[folder.Id].DeleteAllTasks();
            return await Observable.Range(0, count).SelectMany(i => {
                return builder.Me.Outlook.TaskFolders[folder.Id].NewFolderTasks(1, title)
                    .SelectMany(lst => lst).Select(outlookTask1 => (application.NewTask(taskStatus), outlookTask1));
            }).Buffer(count);
        }

        public static Task NewTask(this XafApplication application, TaskStatus taskStatus) {
            using (var objectSpace = application.CreateObjectSpace()){
                var task = objectSpace.NewTask(taskStatus);
                objectSpace.CommitChanges();
                return task;
            }
        }
        public static Task NewTask(this IObjectSpace objectSpace, TaskStatus taskStatus,int index=0) {
            var task = objectSpace.CreateObject<Task>();
            task.Subject = $"Subject{index}";
            task.Description = "Description";
            task.StartDate=DateTime.Now.AddMinutes(1);
            task.DueDate=DateTime.Now.AddMinutes(2);
            task.Status=taskStatus;
            return task;
        }

        public static IObservable<Unit> DeleteAllTasks(this IOutlookTaskFolderRequestBuilder builder){
            return builder.Tasks.ListAllItems().DeleteAll(task => builder.Me().Outlook.Tasks[task.Id].Request().DeleteAsync().ToObservable());
        }

        public static IObservable<Unit> DeleteAll(this IObservable<IEnumerable<Entity>> source, Func<Entity, IObservable<Unit>> delete){
            return source.Aggregate((acc, curr) => acc.Concat(curr)).SelectMany(tasks => tasks)
                .SelectMany(delete).LastOrDefaultAsync();
        }


        public static IObservable<IList<OutlookTask>> NewFolderTasks(this IOutlookTaskFolderRequestBuilder builder,int count,string title){
            var dateTime = DateTime.Now;
            return Observable.Range(0, count).SelectMany(i => {
                var task = new OutlookTask(){
                    Subject = $"{i}{title}",
                    CompletedDateTime = new DateTimeTimeZone {DateTime = dateTime.AddHours(i).ToString(CultureInfo.InvariantCulture),
                        TimeZone = TimeZoneInfo.Local.Id
                    }
                };
                
                return builder.Tasks.Request().AddAsync(task);
            }).Buffer(count);
        }
        
    }
}