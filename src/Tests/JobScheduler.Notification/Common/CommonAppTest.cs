using DevExpress.ExpressApp.Blazor;
using NUnit.Framework;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.Tests.Common {
    public abstract class CommonAppTest:CommonTest{
        protected BlazorApplication Application;

        protected void AwaitInit(){ }

        public override void Dispose(){ }

        protected override void ResetXAF(){ }


        [OneTimeTearDown]
        public override void Cleanup() {
            base.Cleanup();
            Application?.Dispose();
            base.Dispose();
        }

        [OneTimeSetUp]
        public override void Init() {
            base.Init();
            Application = NewBlazorApplication();
            JobSchedulerNotificationModule(Application);
        }
    }
}