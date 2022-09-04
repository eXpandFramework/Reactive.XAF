using System.Diagnostics.CodeAnalysis;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ApplicationBuilder;
using DevExpress.ExpressApp.Win.ApplicationBuilder;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Design;

namespace Xpand.XAF.SpeechManager.Win {
    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    public class ApplicationBuilder : IDesignTimeApplicationFactory {
        public static WinApplication BuildApplication(string connectionString) {
            var builder = WinApplication.CreateBuilder();
            builder.UseApplication<SpeechManagerWindowsFormsApplication>();
            builder.Modules
                .Add<Xpand.XAF.SpeechManager.Module.SpeechManagerModule>()
                .Add<SpeechManagerWinModule>();
            builder.ObjectSpaceProviders
                .AddXpo((application, options) => {
                    options.ConnectionString = connectionString;
                })
                .AddNonPersistent();
            builder.AddBuildStep(application => {
                application.ConnectionString = connectionString;
#if DEBUG
                if(System.Diagnostics.Debugger.IsAttached && application.CheckCompatibilityType == CheckCompatibilityType.DatabaseSchema) {
                    application.DatabaseUpdateMode = DatabaseUpdateMode.UpdateDatabaseAlways;
                }
#endif
            });
            var winApplication = builder.Build();
            return winApplication;
        }

        XafApplication IDesignTimeApplicationFactory.Create()
            => BuildApplication(XafApplication.DesignTimeConnectionString);
    }
}
