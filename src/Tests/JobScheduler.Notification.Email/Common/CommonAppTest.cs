using System;
using System.Linq;
using DevExpress.ExpressApp.Blazor;
using Hangfire;
using Hangfire.MemoryStorage;
using NUnit.Framework;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.TestsLib.Blazor;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.Email.Tests.BO;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.Email.Tests.Common {
    public abstract class CommonAppTest:BlazorCommonAppTest{
        protected override Type StartupType => throw new NotImplementedException();
        
        protected EmailNotificationModule EmailNotificationModule(BlazorApplication newBlazorApplication) {
            var module = newBlazorApplication.AddModule<EmailNotificationModule>(typeof(JSNEE).CollectExportedTypesFromAssembly().ToArray());
            newBlazorApplication.Logon();
            using var objectSpace = newBlazorApplication.CreateObjectSpace();
            return module;
        }

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
            GlobalConfiguration.Configuration.UseMemoryStorage();
            Application = NewBlazorApplication(StartupType);
            EmailNotificationModule(Application);
        }
    }
}