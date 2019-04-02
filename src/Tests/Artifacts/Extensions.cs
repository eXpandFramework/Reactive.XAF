using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Templates;
using DevExpress.ExpressApp.Xpo;
using Fasterflect;
using Moq;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Agnostic.Tests.Artifacts {
    static class Extensions {

        public static Mock<T> GetMock<T>(this T t) where T:class{
            return Mock.Get(t);
        }

        public static void MockCreateControls(this DashboardView view) {
            foreach (var dashboardViewItem in view.GetItems<DashboardViewItem>()){
                dashboardViewItem.CreateControl();
            }
            view.CreateControls();
        }

        public static void SetupDefaults(this XafApplication application, params ModuleBase[] modules) {
            application.RegisterDefaults(modules);
            application.Setup();
        }

        public static void RegisterDefaults(this XafApplication application, params ModuleBase[] modules){
            application.AlwaysUpdateOnDatabaseVersionMismatch().Subscribe();
            application.Modules.Add((ReactiveModule)typeof(ReactiveModule).CreateInstance());
            application.Modules.AddRange(modules);
            application.RegisterInMemoryObjectSpaceProvider();
            MockFrameTemplate(application);
        }

        private static void MockFrameTemplate(XafApplication application){
            var frameTemplateMock = new Mock<IFrameTemplate>();
            frameTemplateMock.Setup(template => template.GetContainers()).Returns(() => new IActionContainer[0]);
            application.WhenCreateCustomTemplate()
                .Do(_ => _.e.Template = frameTemplateMock.Object)
                .Subscribe();
        }

        public static void RegisterInMemoryObjectSpaceProvider(this XafApplication application) {
            application.AddObjectSpaceProvider(new XPObjectSpaceProvider(new MemoryDataStoreProvider()));
        }


    }
}
