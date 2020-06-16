using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using JetBrains.Annotations;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.XAF.Modules.ModelMapper.Services;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.ModelMapper{
    public sealed class ModelMapperModule : ReactiveModuleBase{
        public const string ModelCategory = "Xpand.ModelMapper";
        private readonly IConnectableObservable<Unit> _modelExtended;

        static ModelMapperModule(){
            TraceSource=new ReactiveTraceSource(nameof(ModelMapperModule));
        }

        public static ReactiveTraceSource TraceSource{ get; [PublicAPI]set; }

        public ModelMapperModule(){
            RequiredModuleTypes.Add(typeof(ReactiveModule));
            _modelExtended = ModelExtendingService.Connected.FirstAsync().Replay(1);
            _modelExtended.Connect();
        }

        public override void CustomizeLogics(CustomLogics customLogics){
            base.CustomizeLogics(customLogics);
            customLogics.RegisterLogic(typeof(IModelLayoutGroup),typeof(ModelLayoutGroupLogic));
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            _modelExtended.FirstAsync().Wait();
            extenders.Add<IModelApplication,IModelApplicationModelMapper>();
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            
            CheckXpandVSIXInstalled();
            moduleManager.ConnectExtendingService()
                .Merge(moduleManager.BindConnect())
                .TakeUntilDisposed(this)
                .Subscribe();
        }

        private static void CheckXpandVSIXInstalled(){
            if (DesignerOnlyCalculator.IsRunFromDesigner){
                var result = Observable.Range(15, 10)
                    .SelectMany(i => Observable
                        .Start(() => System.Runtime.InteropServices.Marshal.GetActiveObject($"VisualStudio.DTE.{i}.0"))
                        .OnErrorResumeNext(Observable.Never<object>())
                        .Select(o => i)).FirstAsync()
                    .SelectMany(i => {
                        return Observable.Start(() => {
                            var installed = Directory.GetDirectories($@"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\appdata\local\microsoft\visualstudio")
                                .Where(s => {
                                    var directoryName = $"{new DirectoryInfo(s).Name}";
                                    return !directoryName.EndsWith("Exp") &&directoryName.StartsWith(i.ToString());
                                })
                                .Any(s => Directory.GetFiles(s, "Xpand.VSIX.pkgdef", SearchOption.AllDirectories).Any());
                            return (vs: i, installed);
                        });
                    }).FirstAsync()
                    .ToTask().Result;
                if (!result.installed){
                    throw new NotSupportedException($"ModelMapper requires Xpand.VSIX which is not installed in VS {result.vs}");
                }
            }
        }
    }

    
}
