using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Xpand.XAF.Modules.ModelMapper.Services;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.ModelMapper {
    public sealed class ModelMapperModule : ReactiveModuleBase {
        private readonly IConnectableObservable<Unit> _modelExtended;


        public ModelMapperModule(){
            RequiredModuleTypes.Add(typeof(ReactiveModule));
            _modelExtended = ModelExtendingService.Connected.FirstAsync().Replay(1);
            _modelExtended.Connect();
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            _modelExtended.FirstAsync().Wait();
            extenders.Add<IModelApplication,IModelApplicationModelMapper>();
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            moduleManager.ConnectExtendingService()
                .Merge(Application.BindConnect())
                .TakeUntilDisposed(this)
                .Subscribe();
        }

        
    }

    
}
