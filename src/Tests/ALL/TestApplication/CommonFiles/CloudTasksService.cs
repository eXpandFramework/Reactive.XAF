using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using Fasterflect;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Reactive.Services;
using Task = DevExpress.Persistent.BaseImpl.Task;

namespace ALL.Tests{
    public static class CloudTasksService{
        public static IObservable<Unit> ConnectCloudTasksService<TCloud>(this ApplicationModulesManager manager,
            Func<(IObservable<(TCloud cloud, MapAction mapAction)> updated, IObservable<IObservable<Unit>> deleteAll,IObservable<Unit> initializeModule)> config){
            var action = config();
            return action.initializeModule.Merge(manager.WhenApplication(application
                => application.UpdateTaskDescription(action.updated)
                    .Merge(application.DeleteAllTasks(action.deleteAll))).ToUnit());
        }

        private static IObservable<Unit> UpdateTaskDescription<TCloud>(this XafApplication application,IObservable<(TCloud cloud,MapAction mapAction)> updated) 
            => updated
                .Do(tuple => {
                    using (var objectSpace = application.CreateObjectSpace()){
                        var cloudOfficeObject = objectSpace.QueryCloudOfficeObject(tuple.cloud.GetPropertyValue("Id").ToString(), CloudObjectType.Task).First();
                    
                        var task = objectSpace.GetObjectByKey<Task>(Guid.Parse(cloudOfficeObject.LocalId));
                        task.Description = tuple.mapAction.ToString();
                        objectSpace.CommitChanges();
                    }
                })
                .ToUnit();
        private static IObservable<Unit> DeleteAllTasks(this XafApplication application, IObservable<IObservable<Unit>> deleteAll)
            => deleteAll.Switch().ToUnit()
                .Merge(application.WhenWindowCreated().When(TemplateContext.ApplicationWindow).FirstAsync()
                    .Do(window => {
                        var objectSpace = window.Application.CreateObjectSpace();
                        objectSpace.Delete(objectSpace.GetObjects<Task>());
                        objectSpace.CommitChanges();
                    }).ToUnit());

        

    }
}