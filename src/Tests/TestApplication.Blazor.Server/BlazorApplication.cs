using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ApplicationBuilder;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Security.ClientServer;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Updating;
using DevExpress.ExpressApp.Xpo;
using TestApplication.Blazor.Server.Services;

namespace TestApplication.Blazor.Server
{
    public class TestApplicationBlazorApplication : BlazorApplication
    {
        public TestApplicationBlazorApplication()
        {
            ApplicationName = "TestApplication";
            CheckCompatibilityType = DevExpress.ExpressApp.CheckCompatibilityType.DatabaseSchema;
            DatabaseVersionMismatch += TestApplicationBlazorApplication_DatabaseVersionMismatch;
        }
        protected override void OnSetupStarted()
        {
            base.OnSetupStarted();

#if DEBUG
            if( CheckCompatibilityType == CheckCompatibilityType.DatabaseSchema) {
                DatabaseUpdateMode = DatabaseUpdateMode.UpdateDatabaseAlways;
            }
#endif
        }
        void TestApplicationBlazorApplication_DatabaseVersionMismatch(object sender, DatabaseVersionMismatchEventArgs e)
        {
            e.Updater.Update();
            e.Handled = true;        }
    }
}
