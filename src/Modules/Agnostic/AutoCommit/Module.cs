using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.AutoCommit{
    public sealed class AutoCommitModule : ModuleBase{
        public const string CategoryName = "Xpand.XAF.Modules.AutoCommit";

        public AutoCommitModule(){
            RequiredModuleTypes.Add(typeof(SystemModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));
            AutoCommitService.Connect().Subscribe();
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelClass, IModelClassAutoCommit>();
            extenders.Add<IModelObjectView, IModelObjectViewAutoCommit>();
        }
    }
}