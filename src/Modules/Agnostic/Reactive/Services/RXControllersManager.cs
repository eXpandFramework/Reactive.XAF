using System;
using System.Reactive.Subjects;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using DevExpress.ExpressApp.Model;

namespace DevExpress.XAF.Modules.Reactive.Services{
    public class RXControllersManager : ControllersManager{
        static readonly Subject<Controller> ControllersSubject=new Subject<Controller>();
        public static IObservable<Controller> ControllersCreated=ControllersSubject;

        protected override Controller CreateController(Controller sourceController, IModelApplication modelApplication){
            var controller = base.CreateController(sourceController, modelApplication);
//            if (typeof(WindowTemplateController).IsAssignableFrom(controller.GetType()))
//                Debug.WriteLine("");
            ControllersSubject.OnNext(controller);
            return controller;
        }
    }
}