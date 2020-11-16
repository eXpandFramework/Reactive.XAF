using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Fasterflect;
using TestApplication;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Task = DevExpress.Persistent.BaseImpl.Task;

namespace ALL.Tests{
    public static class CloudTasksService{
        public static IObservable<Unit> ConnectCloudTasksService<TCloud>(this ApplicationModulesManager manager,
            Func<(IObservable<(TCloud cloud, MapAction mapAction)> updated, IObservable<IObservable<Unit>> deleteAll,IObservable<Unit> initializeModule)> config){
            var action = config();
            return action.initializeModule.Merge(manager.WhenApplication(application
                => application.WhenViewOnFrame(typeof(Task),ViewType.DetailView)
                    .SelectMany(frame => frame.NotifyTaskOperation(Observable.Defer(() => action.updated).ObserveOn(SynchronizationContext.Current!)))
                    .ToUnit()
                    .Merge(application.DeleteAllEntities<Task>(action.deleteAll))).ToUnit());
        }

        private static IObservable<Unit> NotifyTaskOperation<TCloud>(this Frame frame,IObservable<(TCloud cloud,MapAction mapAction)> updated) 
            => updated.Do(tuple => {
                    using (var objectSpace = frame.Application.CreateObjectSpace()){
                        var cloudOfficeObject = objectSpace.QueryCloudOfficeObject(tuple.cloud.GetPropertyValue("Id").ToString(), CloudObjectType.Task).First();
                        if (cloudOfficeObject != null){
                            frame.GetController<ModificationsController>().SaveAction.ToolTip = tuple.mapAction.ToString();
                        }
                    }
                })
                .ToUnit();

        public static IObservable<Unit> InitializeCloudTasksModule(this ApplicationModulesManager manager,Action<IModelOffice> action){
            manager.Modules.OfType<AgnosticModule>().First().AdditionalExportedTypes.Add(typeof(Task));
            return manager.WhenGeneratingModelNodes(application => application.Views)
                .Do(views => action(views.Application.ToReactiveModule<IModelReactiveModuleOffice>().Office))
                .ToUnit();

        }
    }

}