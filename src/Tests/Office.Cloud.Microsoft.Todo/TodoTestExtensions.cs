using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using Microsoft.Graph;
using Shouldly;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Office.Cloud.Microsoft;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.Model;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.TestsLib;
using Xpand.XAF.Modules.Reactive;
using Platform = Xpand.Extensions.XAF.XafApplication.Platform;
using Task = DevExpress.Persistent.BaseImpl.Task;
using TaskStatus = DevExpress.Persistent.Base.General.TaskStatus;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft.Todo.Tests{
    static class TodoTestExtensions{
        public const string TasksFolderName = "Brokero Tasks";
        public const string TasksPagingFolderName = "Brokero Paging Tasks";

        public const int TasksFolderPagingItemsCount = 11;

        public static async Task<(IOutlookTaskFolderRequestBuilder requestBuilder, Frame frame)> InitializeService(this XafApplication application,string taskFolderName=TasksFolderName,bool keepTasks=false){
            var modelTodo = application.Model.ToReactiveModule<IModelReactiveModuleOffice>().Office.Microsoft().Todo();
            modelTodo.DefaultTodoListName = taskFolderName;
            var client = await application.InitGraphServiceClient();
            var foldersRequestBuilder = client.client.Me.Outlook.TaskFolders;
            var taskFolder = await foldersRequestBuilder.GetFolder(taskFolderName, taskFolderName!=TasksPagingFolderName);
            if (taskFolder == null&&taskFolderName==TasksPagingFolderName){
                taskFolder = await foldersRequestBuilder.Request()
                    .AddAsync(new OutlookTaskFolder(){Name = TasksPagingFolderName});
                await foldersRequestBuilder[taskFolder.Id].NewFolderTasks(TasksFolderPagingItemsCount, nameof(MicrosoftTodoModule));
            }
            var requestBuilder = foldersRequestBuilder[taskFolder?.Id];
            if (taskFolderName != TasksPagingFolderName&&!keepTasks){
                await requestBuilder.DeleteAllTasks();
                (await requestBuilder.Tasks.ListAllItems()).Length.ShouldBe(0);
            }
            
            return (requestBuilder,client.frame);
        }

        public static async Task<(Frame frame, GraphServiceClient client)> InitGraphServiceClient(this XafApplication application){
            application.ObjectSpaceProvider.NewMicrosoftAuthentication();
            var todoModel = await application.ReactiveModulesModel().OfficeModel().MicrosoftModel().TodoModel();
            var window = application.CreateViewWindow();
            window.SetView(application.NewView(todoModel.ObjectViews().First().ObjectView));
            var client = await TodoService.Client.FirstAsync().ToTaskWithoutConfigureAwait();
            return client;
        }

        public static MicrosoftTodoModule TodoModule(this Platform platform,string title,params ModuleBase[] modules){
            var application = NewApplication(platform, title, modules);
            var securityStrategyComplex = new SecurityStrategyComplex(typeof(PermissionPolicyUser),
                typeof(PermissionPolicyRole), new AuthenticationStandard());
            application.Security = securityStrategyComplex;
            var module = application.AddModule<MicrosoftTodoModule>(typeof(Task));
            var todoModel = application.Model.ToReactiveModule<IModelReactiveModuleOffice>().Office.Microsoft().Todo();
            var dependency = ((IModelObjectViews) todoModel).ObjectViews.AddNode<IModelObjectViewDependency>();
            dependency.ObjectView = application.Model.BOModel.GetClass(typeof(Task)).DefaultDetailView;
            application.Logon();
            application.CreateObjectSpace();
            return module.Application.Modules.OfType<MicrosoftTodoModule>().First();
        }

        static XafApplication NewApplication(this Platform platform, string title, ModuleBase[] modules){
            var xafApplication = platform.NewApplication<MicrosoftTodoModule>();
            xafApplication.Title = title;
            xafApplication.Modules.AddRange(modules);
            return xafApplication;
        }

        public static void AssertTask(this IObjectSpaceProvider objectSpaceProvider, Type cloudEntityType, Task task,
            string title, string notes, DateTime? due, string expectedStatus, string actualStatus, string taskId){
            title.ShouldBe(task.Subject);
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
            var builder =await application.ObjectSpaceProvider.NewGraphServiceClient();
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
        public static Task NewTask(this IObjectSpace objectSpace, TaskStatus taskStatus) {
            var task = objectSpace.CreateObject<Task>();
            task.Subject = "Subject";
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
        public static Guid NewMicrosoftAuthentication(this IObjectSpaceProvider objectSpaceProvider){
            var type = typeof(TodoSynchronizationTests);
            using (var manifestResourceStream = type.Assembly.GetManifestResourceStream(type, "AuthenticationData.json")){
                var token = Encoding.UTF8.GetBytes(new StreamReader(manifestResourceStream ?? throw new InvalidOperationException()).ReadToEnd());
                using (var objectSpace = objectSpaceProvider.CreateObjectSpace()){
                    var authenticationOid = (Guid)objectSpace.GetKeyValue(SecuritySystem.CurrentUser);
                    if (objectSpace.GetObjectByKey<MSAuthentication>(authenticationOid)==null){
                        var authentication = objectSpace.CreateObject<MSAuthentication>();
                    
                        authentication.Oid=authenticationOid;
                        authentication.Token=token;
                        objectSpace.CommitChanges();
                    }
                    
                    return authenticationOid;
                }
            }
        }

        public static IObservable<GraphServiceClient> NewGraphServiceClient(this IObjectSpaceProvider objectSpaceProvider){
            return ServiceProvider.ClientAppBuilder.Authorize(cache => {
                var newMicrosoftAuthentication = objectSpaceProvider.NewMicrosoftAuthentication();
                return cache.SynchStorage(objectSpaceProvider.CreateObjectSpace, newMicrosoftAuthentication);
            });
        }
    }
}