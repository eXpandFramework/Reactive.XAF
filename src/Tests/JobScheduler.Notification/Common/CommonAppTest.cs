using System;
using NUnit.Framework;
using Xpand.TestsLib.Blazor;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.Tests.Common {
    public abstract class CommonAppTest:BlazorCommonAppTest{
        protected override Type StartupType => typeof(Startup);


        [OneTimeSetUp]
        public override void Init() {
            base.Init();
            Application.JobSchedulerNotificationModule();
        }
    }
}