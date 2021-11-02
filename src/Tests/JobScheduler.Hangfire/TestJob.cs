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
    public class ChainJob:TestJob {
        
    }

    [JobProvider]
    public class TestJob {
        public static readonly Subject<TestJob> Jobs=new();

        public PerformContext Context { get; private set; }

        public TestJob() { }
        public BlazorApplication Application { get; }

        protected TestJob(BlazorApplication application) {
            Application = application;
        }

        [AutomaticRetry(Attempts = 0)][JobProvider]
        public void FailMethodNoRetry() {
            throw new NotImplementedException();
        }
        
        [AutomaticRetry(Attempts = 1,DelaysInSeconds = new[] {1,1})][JobProvider]
        public void FailMethodRetry() {
            throw new NotImplementedException();
        }

        [JobProvider]
        public void Test() {
            Jobs.OnNext(this);
        }
        
        [JobProvider]
        public void TestJobId(PerformContext context) {
            Context = context;
            Jobs.OnNext(this);
        }
    }
}