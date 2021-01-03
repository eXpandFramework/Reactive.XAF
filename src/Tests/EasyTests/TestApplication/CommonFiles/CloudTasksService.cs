using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using Fasterflect;
using TestApplication;
using TestApplication.Module;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Task = DevExpress.Persistent.BaseImpl.Task;

namespace ALL.Tests{
    public static class CloudTasksService{
        public static IObservable<Unit> ConnectCloudTasksService<TCloud>(this ApplicationModulesManager manager,
            Func<(IObservable<(TCloud cloud, MapAction mapAction)> updated, IObservable<IObservable<Unit>> deleteAll,IObservable<Unit> initializeModule)> config) 
            => config().initializeModule
                .Merge(manager.WhenApplication(application
                    => application.WhenViewOnFrame(typeof(Task),ViewType.DetailView)
                        .SelectMany(frame => frame.View.ObjectSpace.WhenCommiting().SelectMany(t => config().updated)
                            .Do(tuple => {
                                using (var objectSpace = frame.Application.CreateObjectSpace()) {
                                    var cloudOfficeObject = objectSpace.QueryCloudOfficeObject(tuple.cloud.GetPropertyValue("Id").ToString(), CloudObjectType.Task).First();
                                    var task = objectSpace.GetObjectByKey<Task>(Guid.Parse(cloudOfficeObject.LocalId));
                                    task.Description = tuple.mapAction.ToString();
                                    objectSpace.CommitChanges();
                                }
                            }))
                        .ToUnit()
                        .Merge(application.DeleteAllEntities<Task>(config().deleteAll))).ToUnit());


        public static IObservable<Unit> InitializeCloudTasksModule(this ApplicationModulesManager manager,Action<IModelOffice> action){
            manager.Modules.OfType<TestApplicationModule>().First().AdditionalExportedTypes.Add(typeof(Task));
            return manager.WhenGeneratingModelNodes(application => application.Views)
                .Do(views => action(views.Application.ToReactiveModule<IModelReactiveModuleOffice>().Office))
                .ToUnit();

        }
    }

}