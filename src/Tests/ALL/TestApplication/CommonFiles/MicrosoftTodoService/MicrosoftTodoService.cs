using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using ALL.Tests;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using Microsoft.Graph;
using Xpand.XAF.Modules.Office.Cloud.Microsoft;
using Xpand.XAF.Modules.Office.Cloud.Microsoft.Calendar;
using Xpand.XAF.Modules.Office.Cloud.Microsoft.Todo;
namespace TestApplication.MicrosoftTodoService{
    public static class MicrosoftTodoService{
        public static IObservable<Unit> ConnectMicrosoftTodoService(this ApplicationModulesManager manager) 
            => manager.ConnectCloudTasksService(() => (
                Xpand.XAF.Modules.Office.Cloud.Microsoft.Todo.MicrosoftTodoService.Updated,
                Xpand.XAF.Modules.Office.Cloud.Microsoft.MicrosoftService.Client.DeleteAllTasks(),manager.InitializeMSTodoModule())
            );

        private static IObservable<Unit> InitializeMSTodoModule(this ApplicationModulesManager manager) 
            => manager.InitializeCloudTasksModule(
                office => {
                    var modelTodo = office.Microsoft().Todo();
                    var modelTodoItem = modelTodo.Items.AddNode<IModelTodoItem>();
                    modelTodoItem.ObjectView=(IModelObjectView) office.Application.Views["TaskMicrosoft_DetailView"];
                    ((ModelNode) modelTodoItem).Id = $"{modelTodoItem.ObjectView.Id}-{modelTodoItem.SynchronizationType}";
                });

        private static IObservable<IObservable<Unit>> DeleteAllTasks(this IObservable<GraphServiceClient> source)
            => source.FirstAsync().Select(client => client.Me.Outlook.Tasks.ListAllItems().DeleteAll(task 
                    => client.Me().Outlook.Tasks[task.Id].Request().DeleteAsync().ToObservable()));
    }
}
