using System;
using System.Collections.Generic;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using JetBrains.Annotations;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;


namespace Xpand.XAF.Modules.ClientLookupCascade {
    [UsedImplicitly]
    public sealed class ClientLookupCascadeModule : ReactiveModuleBase{
        static ClientLookupCascadeModule(){
            TraceSource=new ReactiveTraceSource(nameof(ClientLookupCascadeModule));
        }
        public ClientLookupCascadeModule() {
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Web.SystemModule.SystemAspNetModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));
        }

        public static ReactiveTraceSource TraceSource{ get; set; }
        protected override IEnumerable<Type> GetDeclaredControllerTypes(){
            yield return typeof(ClientDatasourceStorageController);
            yield return typeof(ClientLookupModelExtender);
            yield return typeof(ParentViewLookupItemsController);
        }

        public override void Setup(XafApplication application){
            base.Setup(application);
            
            // this.Connect()
            //     .TakeUntilDisposed(this)
            //     .Subscribe();
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            
            // extenders.Add<IModelReactiveModules,IModelReactiveModuleLogger>();
            
        }
    }

}
