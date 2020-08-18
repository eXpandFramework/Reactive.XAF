using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using ALL.Tests;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Microsoft.Graph;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Office.Cloud.Microsoft;
using Xpand.XAF.Modules.Office.Cloud.Microsoft.Calendar;
using Xpand.XAF.Modules.Office.Cloud.Microsoft.Todo;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Task = DevExpress.Persistent.BaseImpl.Task;

// ReSharper disable once CheckNamespace
namespace TestApplication.MicrosoftTodoService{
    public static class MicrosoftTodoService{
        public static IObservable<Unit> ConnectMicrosoftTodoService(this ApplicationModulesManager manager) 
            => manager.ConnectCloudTasksService(() => (
                Xpand.XAF.Modules.Office.Cloud.Microsoft.Todo.MicrosoftTodoService.Updated,
                Xpand.XAF.Modules.Office.Cloud.Microsoft.MicrosoftService.Client.DeleteAllTasks(),manager.InitializeModule()));

        private static IObservable<IObservable<Unit>> DeleteAllTasks(this IObservable<GraphServiceClient> source)
            => source.FirstAsync().Select(client 
                => client.Me.Outlook.Tasks.ListAllItems().DeleteAll(task 
                    => client.Me().Outlook.Tasks[task.Id].Request().DeleteAsync().ToObservable()));

        private static IObservable<Unit> InitializeModule(this ApplicationModulesManager manager){
            manager.Modules.OfType<AgnosticModule>().First().AdditionalExportedTypes.Add(typeof(Task));
            return manager.WhenGeneratingModelNodes(application => application.Views)
                .Do(views => {
                    var modelTodo = views.Application.ToReactiveModule<IModelReactiveModuleOffice>().Office.Microsoft().Todo();
                    var modelTodoItem = modelTodo.Items.AddNode<IModelTodoItem>();
                    modelTodoItem.ObjectView=(IModelObjectView) views.Application.Views["TaskMicrosoft_DetailView"];
                }).ToUnit();
        }
    }
}
