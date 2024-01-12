using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;

namespace TestApplication.Blazor.Server;

public class TestApplicationBlazorApplication : BlazorApplication {
    public TestApplicationBlazorApplication() {
        ApplicationName = "TestApplication";
        CheckCompatibilityType = CheckCompatibilityType.DatabaseSchema;
        DatabaseVersionMismatch += TestApplicationBlazorApplication_DatabaseVersionMismatch;
    }
    protected override void OnSetupStarted() {
        base.OnSetupStarted();
        DatabaseUpdateMode = DatabaseUpdateMode.UpdateDatabaseAlways;
    }
    
    private void TestApplicationBlazorApplication_DatabaseVersionMismatch(object sender, DatabaseVersionMismatchEventArgs e) {
        e.Updater.Update();
        e.Handled = true;
    }
}
