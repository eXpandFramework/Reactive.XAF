using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Subjects;
using DevExpress.ExpressApp.Blazor;
using Hangfire;
using Hangfire.Server;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests {
    [JobProvider]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class TestJobDI:TestJob {
        public TestJobDI(BlazorApplication provider):base(provider) {
        }

        public TestJobDI() { }
    }

    [JobProvider]
    public class TestJob {
        public static Subject<TestJob> Jobs=new();

        public PerformContext Context { get; private set; }

        public TestJob() { }
        public BlazorApplication Application { get; }

        protected TestJob(BlazorApplication application) {
            Application = application;
        }

        [AutomaticRetry(Attempts = 0)]
        public void FailMethodNoRetry() {
            throw new NotImplementedException();
        }
        
        [AutomaticRetry(Attempts = 1,DelaysInSeconds = new[] {1,1})]
        public void FailMethodRetry() {
            throw new NotImplementedException();
        }

        public void Test() {
            Jobs.OnNext(this);
        }
        public void TestJobId(PerformContext context) {
            Context = context;
            Jobs.OnNext(this);
        }
    }
}