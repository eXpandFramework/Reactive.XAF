using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Notifications;
using DevExpress.ExpressApp.Updating;
using Xpand.Extensions.XAF.Xpo.BaseObjects;
using Xpand.XAF.Modules.Reactive.Logger;
using Xpand.XAF.Modules.Reactive.Logger.Hub;
using Xpand.XAF.Modules.Speech;

namespace Xpand.XAF.SpeechManager.Module {
    
    public sealed class SpeechManagerModule : ModuleBase {
        public SpeechManagerModule() {
            AdditionalExportedTypes.Add(typeof(ErrorEvent));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Objects.BusinessClassLibraryCustomizationModule));
            RequiredModuleTypes.Add(typeof(SpeechModule));
            RequiredModuleTypes.Add(typeof(ReactiveLoggerModule));
            RequiredModuleTypes.Add(typeof(ReactiveLoggerHubModule));
        }

        public override void Setup(ApplicationModulesManager moduleManager) {
            base.Setup(moduleManager);
            moduleManager.Modules.FindModule<NotificationsModule>().ShowNotificationsWindow = false;
        }

        public override IEnumerable<ModuleUpdater> GetModuleUpdaters(IObjectSpace objectSpace, Version versionFromDB) 
            => new[] { new DatabaseUpdate.Updater(objectSpace, versionFromDB) };
    }
}
