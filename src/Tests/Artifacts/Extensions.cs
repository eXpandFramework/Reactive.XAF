using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using Fasterflect;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Agnostic.Tests.Artifacts {
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
