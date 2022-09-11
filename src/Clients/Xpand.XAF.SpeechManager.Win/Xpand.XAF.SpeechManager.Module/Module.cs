using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Updating;
using Xpand.XAF.Modules.ModelMapper;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Speech;

namespace Xpand.XAF.SpeechManager.Module {
    
    public sealed class SpeechManagerModule : ModuleBase {
        public SpeechManagerModule() {
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Objects.BusinessClassLibraryCustomizationModule));
            RequiredModuleTypes.Add(typeof(SpeechModule));
            RequiredModuleTypes.Add(typeof(ModelMapperModule));
        }

        public override void Setup(ApplicationModulesManager moduleManager) {
            base.Setup(moduleManager);
            moduleManager
                .WhenApplication(xafApplication => xafApplication.WhenDetailViewCreated()
                    .SelectMany(t => t.e.View.GetItems<PropertyEditor>().Where(editor => editor.MemberInfo.Name=="File")))
                .Select(editor => editor)
                .Subscribe();
        }

        public override IEnumerable<ModuleUpdater> GetModuleUpdaters(IObjectSpace objectSpace, Version versionFromDB) {
            ModuleUpdater updater = new DatabaseUpdate.Updater(objectSpace, versionFromDB);
            return new[] { updater };
        }


    }
}
