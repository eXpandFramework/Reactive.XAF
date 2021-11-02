using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using Hangfire;
using Hangfire.MemoryStorage;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.TestsLib.Blazor;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.Email.Tests.BO;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.Email.Tests.Common {
    public abstract class CommonTest : BlazorCommonTest {

        protected override void ResetXAF() {
            
        }

        public override void Init() {
            base.Init();
            GlobalConfiguration.Configuration.UseMemoryStorage();
        }

        public EmailNotificationModule EmailNotificationModule(params ModuleBase[] modules) {
            var newBlazorApplication = NewBlazorApplication();
            return EmailNotificationModule(newBlazorApplication);
        }

        protected virtual BlazorApplication NewBlazorApplication() 
            => NewBlazorApplication(typeof(Startup));

        protected EmailNotificationModule EmailNotificationModule(BlazorApplication newBlazorApplication) {
            var module = newBlazorApplication.AddModule<EmailNotificationModule>(typeof(JSNEE).CollectExportedTypesFromAssembly().ToArray());
            // newBlazorApplication.ConfigureModel();
            newBlazorApplication.Logon();
            using var objectSpace = newBlazorApplication.CreateObjectSpace();
            return module;
        }

    }
}