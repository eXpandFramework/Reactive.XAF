using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using JetBrains.Annotations;
using MessagePack;
using MessagePack.Resolvers;
using Xpand.Extensions.Reactive.Utility;
using Xpand.XAF.Modules.Reactive.Extensions;
using YamlDotNet.Serialization;


namespace Xpand.XAF.Modules.Reactive.Logger.Hub {
    [UsedImplicitly]
    public sealed class ReactiveLoggerHubModule : ReactiveModuleBase{
        [PublicAPI]
        public const string CategoryName = "Xpand.XAF.Modules.Reactive.Logger.Hub";

        static ReactiveLoggerHubModule(){
            var serializer = new SerializerBuilder().Build();
            Utility.Serializer = o => $"---------{o.GetType().FullName}--------{Environment.NewLine}{serializer.Serialize(o)}";
            MessagePackSerializer.SetDefaultResolver(ContractlessStandardResolver.Instance);
            TraceSource=new ReactiveTraceSource(nameof(ReactiveLoggerHubModule));
            
        }
        public ReactiveLoggerHubModule() {
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
            RequiredModuleTypes.Add(typeof(ReactiveLoggerModule));
        }

        public static ReactiveTraceSource TraceSource{ get; [PublicAPI]set; }

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            moduleManager.Connect()
	            .Subscribe(this);
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelReactiveLogger,IModelReactiveLoggerHub>();
            
        }
    }

}
