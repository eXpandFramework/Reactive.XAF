using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.FileAttachments.Win;
using DevExpress.ExpressApp.Updating;

namespace Xpand.XAF.SpeechManager.Win {
    [ToolboxItemFilter("Xaf.Platform.Win")]
    public sealed class SpeechManagerWinModule : ModuleBase {
        public SpeechManagerWinModule() {
            FormattingProvider.UseMaskSettings = true;
            RequiredModuleTypes.Add(typeof(FileAttachmentsWindowsFormsModule));
        }
        public override IEnumerable<ModuleUpdater> GetModuleUpdaters(IObjectSpace objectSpace, Version versionFromDB) {
            return ModuleUpdater.EmptyModuleUpdaters;
        }
    }
}
