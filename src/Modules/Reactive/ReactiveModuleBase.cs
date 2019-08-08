using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DevExpress.ExpressApp;

namespace Xpand.XAF.Modules.Reactive{
    public abstract partial class ReactiveModuleBase:ModuleBase{
        
        internal ReplaySubject<ReactiveModuleBase> SetupCompletedSubject=new ReplaySubject<ReactiveModuleBase>(1);
        public void Unload(){
            Application.Modules.Remove(this);
            Dispose();
        }

        public IObservable<ReactiveModuleBase> SetupCompleted => Observable.Defer(() => SetupCompletedSubject.Select(module => module));


    }
}
