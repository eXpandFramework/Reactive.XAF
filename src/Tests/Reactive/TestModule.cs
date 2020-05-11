using System;
using System.Reactive.Subjects;
using DevExpress.ExpressApp;
using JetBrains.Annotations;

namespace Xpand.XAF.Modules.Reactive.Tests{
    [UsedImplicitly]
    public class TestModule:ModuleBase{
        public TestModule(){
            RequiredModuleTypes.Add(typeof(ReactiveModule));
        }

        readonly Subject<(ApplicationModulesManager manager, ModuleBase module)> _modulesManagerSubject=new Subject<(ApplicationModulesManager manager, ModuleBase module)>();
        public IObservable<(ApplicationModulesManager manager,ModuleBase module)> ApplicationModulesManager=>_modulesManagerSubject;
        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            _modulesManagerSubject.OnNext((moduleManager, this));
        }
    }
}