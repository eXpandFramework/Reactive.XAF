using System;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Google.Apis.Tasks.v1;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Office.Cloud.Google;
using Xpand.XAF.Modules.Office.Cloud.Google.Tasks;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace ALL.Tests{
    public static class GoogleTasksService{
        public static IObservable<Unit> ConnectGoogleTasksService(this ApplicationModulesManager manager) 
            => manager.ConnectCloudTasksService(() => (Xpand.XAF.Modules.Office.Cloud.Google.Tasks.GoogleTasksService.Updated, DeleteAllEntities(), manager.InitializeModule()));

        private static IObservable<Unit> InitializeModule(this ApplicationModulesManager manager) 
            => manager.WhenGeneratingModelNodes(application => application.Views)
                .Do(views => {
                    var modelTasks = views.Application.ToReactiveModule<IModelReactiveModuleOffice>().Office.Google().Tasks();
                    var tasksItem = modelTasks.Items.AddNode<IModelTasksItem>();
                    tasksItem.ObjectView=(IModelObjectView) views.Application.Views["TaskGoogle_DetailView"];
                }).ToUnit();

        private static IObservable<IObservable<Unit>> DeleteAllEntities() 
            => Xpand.XAF.Modules.Office.Cloud.Google.Tasks.GoogleTasksService.Credentials.FirstAsync()
                .Select(client => {
                    var tasksService = client.credential.NewService<TasksService>();
                    return tasksService.GetTaskList("@default").SelectMany(list => tasksService.DeleteAllTasks(list.Id)).ToUnit();
                });    }
}