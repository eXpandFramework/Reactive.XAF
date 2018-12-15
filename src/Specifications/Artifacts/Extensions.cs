using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using DevExpress.XAF.Modules.Reactive;
using DevExpress.XAF.Modules.Reactive.Services;
using Fasterflect;

namespace DevExpress.XAF.Agnostic.Specifications.Artifacts {
    static class Extensions {

        public static void SetupDefaults(this XafApplication application, params ModuleBase[] modules) {
            application.RegisterDefaults(modules);
            application.Setup();
        }

        public static void RegisterDefaults(this XafApplication application, params ModuleBase[] modules){
            application.AlwaysUpdateOnDatabaseVersionMismatch().Subscribe();
            application.Modules.Add((ReactiveModule)typeof(ReactiveModule).CreateInstance());
            application.Modules.AddRange(modules);
            application.RegisterInMemoryObjectSpaceProvider();

        }

        public static void RegisterInMemoryObjectSpaceProvider(this XafApplication application) {
            application.AddObjectSpaceProvider(new XPObjectSpaceProvider(new MemoryDataStoreProvider()));
        }


    }
}
