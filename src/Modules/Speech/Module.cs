using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Notifications;
using DevExpress.ExpressApp.Validation;
using DevExpress.ExpressApp.ViewVariantsModule;
using DevExpress.XtraSpellChecker;
using Xpand.XAF.Modules.CloneModelView;
using Xpand.XAF.Modules.HideToolBar;
using Xpand.XAF.Modules.ModelViewInheritance;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Speech.Services;
using Xpand.XAF.Modules.SpellChecker;
using Xpand.XAF.Modules.ViewItemValue;
using Xpand.XAF.Modules.Windows;

namespace Xpand.XAF.Modules.Speech {
    
    public sealed class SpeechModule : ReactiveModuleBase{
        static SpeechModule(){
            TraceSource=new ReactiveTraceSource(nameof(SpeechModule));
        }
        public static ReactiveTraceSource TraceSource{ get; set; }
        public SpeechModule() {
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
            RequiredModuleTypes.Add(typeof(ConditionalAppearanceModule));
            RequiredModuleTypes.Add(typeof(NotificationsModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));
            RequiredModuleTypes.Add(typeof(ValidationModule));
            RequiredModuleTypes.Add(typeof(CloneModelViewModule));
            RequiredModuleTypes.Add(typeof(HideToolBarModule));
            RequiredModuleTypes.Add(typeof(WindowsModule));
            RequiredModuleTypes.Add(typeof(SpellCheckerModule));
            RequiredModuleTypes.Add(typeof(ViewVariantsModule));
            RequiredModuleTypes.Add(typeof(ModelViewInheritanceModule));
            RequiredModuleTypes.Add(typeof(ViewItemValueModule));
            
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            
            moduleManager.WhenApplication(xafApplication => xafApplication
                    .WhenObjectSpaceCreated()
                    .SelectMany(space => space.WhenModifyChanged())
                .Select(o => o))
                .Subscribe(this);
            moduleManager.ConnectSpeechToText()
                .Merge(moduleManager.ConnectAccount())
                .Merge(moduleManager.ConnectSpeech())
                .Merge(moduleManager.ConnectTextToSpeech())
                .Merge(moduleManager.ConnectSpeechTextInfo())
                .Subscribe(this);
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelReactiveModules,IModelReactiveModuleSpeech>();
        }
    }
}
