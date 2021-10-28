using DevExpress.ExpressApp.Blazor;
using NUnit.Framework;

namespace Xpand.XAF.Modules.RazorView.Tests.Common {
    public abstract class CommonAppTest:CommonTest{
        protected BlazorApplication Application;

        protected void AwaitInit(){ }

        public override void Dispose(){ }

        protected override void ResetXAF(){ }

        protected void NewRule() {
            var space = Application.CreateObjectSpace();
            // var notificationRule = space.CreateObject<NotificationRule>();
            // notificationRule.Object = new ObjectType(typeof(NObject));
            space.CommitChanges();
        }

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
            RazorViewModule(Application);
        }
    }
}