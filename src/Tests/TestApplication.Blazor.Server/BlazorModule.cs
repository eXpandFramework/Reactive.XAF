using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.BaseImpl;
using System.ComponentModel;
using Updater = TestApplication.Blazor.Server.DatabaseUpdate.Updater;

namespace TestApplication.Blazor.Server
{
    [ToolboxItemFilter("Xaf.Platform.Blazor")]
    // For more typical usage scenarios, be sure to check out https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.ModuleBase.
    public sealed class TestApplicationBlazorModule : ModuleBase
    {
        public TestApplicationBlazorModule(){
            AdditionalExportedTypes.Add(typeof(ModelDifference));
        }

        //void Application_CreateCustomModelDifferenceStore(object sender, CreateCustomModelDifferenceStoreEventArgs e) {
        //    e.Store = new ModelDifferenceDbStore((XafApplication)sender, typeof(ModelDifference), true, "Blazor");
        //    e.Handled = true;
        //}
        void Application_CreateCustomUserModelDifferenceStore(object sender, CreateCustomModelDifferenceStoreEventArgs e)
        {
            e.Store = new ModelDifferenceDbStore((XafApplication)sender, typeof(ModelDifference), false, "Blazor");
            e.Handled = true;
        }

        public override IEnumerable<ModuleUpdater> GetModuleUpdaters(IObjectSpace objectSpace, Version versionFromDB) 
            => [new Updater(objectSpace, versionFromDB)];

        public override void Setup(XafApplication application)
        {
            base.Setup(application);
            // Uncomment this code to store the shared model differences (administrator settings in Model.XAFML) in the database.
            // For more information, refer to the following topic: https://docs.devexpress.com/eXpressAppFramework/113698/
            //application.CreateCustomModelDifferenceStore += Application_CreateCustomModelDifferenceStore;
            application.CreateCustomUserModelDifferenceStore += Application_CreateCustomUserModelDifferenceStore;
        }
    }
}
