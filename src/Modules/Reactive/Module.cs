using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive {
    public sealed class ReactiveModule : ModuleBase {
        readonly Subject<ITypesInfo> _typesInfoSubject=new Subject<ITypesInfo>();
        public IObservable<ITypesInfo> TypesInfo;

        public ReactiveModule() {
            
//            AppDomain.CurrentDomain.AssemblyResolve+=CurrentDomainOnAssemblyResolve;
            TypesInfo = _typesInfoSubject;
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
        }

        private Assembly CurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args){
            var setupInformation = AppDomain.CurrentDomain.SetupInformation;
            var binPath = setupInformation.PrivateBinPath??setupInformation.ApplicationBase;
            var indexOf = args.Name.IndexOf(",", StringComparison.Ordinal);
            if (indexOf > -1){
                var assemblyName = args.Name.Substring(0, indexOf);
                var path = $"{Path.Combine(binPath, assemblyName)}.dll";
                if (File.Exists(path)){
                    return Assembly.LoadFile(path);
                }
            }
            return null;
        }

        protected override void Dispose(bool disposing){
            base.Dispose(disposing);
            AppDomain.CurrentDomain.AssemblyResolve-=CurrentDomainOnAssemblyResolve;
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            Application?.Connect()
                .TakeUntil(this.WhenDisposed())
                .Subscribe();
        }

        public override void CustomizeTypesInfo(ITypesInfo typesInfo) {
            base.CustomizeTypesInfo(typesInfo);
            _typesInfoSubject.OnNext(typesInfo);
            _typesInfoSubject.OnCompleted();
        }


    }
}
