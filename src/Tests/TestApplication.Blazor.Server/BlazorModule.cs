using System.ComponentModel;
using DevExpress.Blazor.Internal;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.BaseImpl;

namespace TestApplication.Blazor.Server;

[ToolboxItemFilter("Xaf.Platform.Blazor")]
public sealed class TestApplicationBlazorModule : ModuleBase {
    public TestApplicationBlazorModule() {
        AdditionalExportedTypes.Add(typeof(ModelDifference));
    }

    private void Application_CreateCustomUserModelDifferenceStore(object sender, CreateCustomModelDifferenceStoreEventArgs e) {
        e.Store = new ModelDifferenceDbStore((XafApplication)sender, typeof(ModelDifference), false, "Blazor");
        e.Handled = true;
    }

    public override IEnumerable<ModuleUpdater> GetModuleUpdaters(IObjectSpace objectSpace, Version versionFromDB) => new Updater(objectSpace, versionFromDB).Yield();

    public override void Setup(XafApplication application) {
        base.Setup(application);
        application.CreateCustomUserModelDifferenceStore += Application_CreateCustomUserModelDifferenceStore;
    }
}
