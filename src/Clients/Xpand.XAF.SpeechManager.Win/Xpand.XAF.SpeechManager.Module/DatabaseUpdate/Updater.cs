using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using Xpand.XAF.Modules.Speech.BusinessObjects;

namespace Xpand.XAF.SpeechManager.Module.DatabaseUpdate {
    public class Updater : ModuleUpdater {
        public Updater(IObjectSpace objectSpace, Version currentDBVersion) :
            base(objectSpace, currentDBVersion) {
        }

        public override void UpdateDatabaseAfterUpdateSchema() {
            base.UpdateDatabaseAfterUpdateSchema();
            if(CurrentDBVersion == new Version("0.0.0.0") ) {
                // foreach (var lang in new []{"en-US","el-EL"}) {
                //     var speechLanguage = ObjectSpace.CreateObject<SpeechLanguage>();
                //     speechLanguage.Name = lang;
                // }
                //
                // var speechVoice = ObjectSpace.CreateObject<SpeechVoice>();
                // speechVoice.Name = "en-US-JennyNeural";
                //
                // ObjectSpace.CommitChanges();
            }
        }
    }
}
