using System;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Google.Apis.Tasks.v1;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Office.Cloud.Google;
using Xpand.XAF.Modules.Office.Cloud.Google.Tasks;

namespace ALL.Tests{
    public static class GoogleTasksService{
        public static IObservable<Unit> ConnectGoogleTasksService(this ApplicationModulesManager manager)
            => manager.ConnectCloudTasksService(() => (
                Xpand.XAF.Modules.Office.Cloud.Google.Tasks.GoogleTasksService.Updated, DeleteAllEntities(),
                manager.InitializeGoogleTasksModule()));

        private static IObservable<Unit> InitializeGoogleTasksModule(this ApplicationModulesManager manager) 
            => manager.InitializeCloudTasksModule(office => {
                var modelTasks = office.Google().Tasks();
                var tasksItem = modelTasks.Items.AddNode<IModelTasksItem>();
                tasksItem.ObjectView=(IModelObjectView) office.Application.Views["TaskGoogle_DetailView"];
            });

        private static IObservable<IObservable<Unit>> DeleteAllEntities() 
            => Xpand.XAF.Modules.Office.Cloud.Google.Tasks.GoogleTasksService.Credentials.FirstAsync()
                .Select(client => {
                    var tasksService = client.credential.NewService<TasksService>();
                    return tasksService.GetTaskList(returnDefault:true).SelectMany(list => tasksService.DeleteAllTasks(list.Id)).ToUnit();
                });
    }
}