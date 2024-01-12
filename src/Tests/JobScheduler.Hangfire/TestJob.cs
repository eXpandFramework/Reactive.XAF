using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using DevExpress.ExpressApp.Security;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.DependencyInjection;
using Xpand.Extensions.Blazor;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.BO;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests {
    [JobProvider]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class TestJobDI:TestJob {
        [ActivatorUtilitiesConstructor]
        public TestJobDI(IServiceProvider provider):base(provider) {
        }

        public TestJobDI() { }
        
        [JobProvider]
        public async Task<bool> CreateObject(PerformContext context) 
            => await Provider.RunWithStorageAsync(application => application.UseObjectSpace(space => Observable.Range(0, 10)
                .Select(_ => {
                    var js = space.CreateObject<JS>();
                    js.CommitChanges();
                    return js;
                })))
                .ToObservable().To(true);

        [JobProvider]
        public async Task<bool> CreateObjectAnonymous(PerformContext context) 
            => await Provider.RunWithStorageAsync(application => {
                    application.Security.Cast<SecurityStrategyComplex>().AnonymousAllowedTypes.Add(typeof(JS));
                    return application.UseObjectSpace(space => Observable.Range(0, 10)
                        .Select(_ => space.CreateObject<JS>()).Commit());
                })
                .ToObservable().To(true);
        
        [JobProvider]
        public async Task<bool> CreateObjectNonSecured(PerformContext context) 
            => await Provider.RunWithStorageAsync(application => application.UseNonSecuredObjectSpace(space => Observable.Range(0, 10)
                    .Select(_ => space.CreateObject<JS>()).Commit()))
                .ToObservable().To(true);
        [JobProvider]
        public async Task<bool> CreateObjectNonAuthenticated(PerformContext context) 
            => await Provider.RunWithStorageAsync(application => application.UseNonSecuredObjectSpace(space => Observable.Range(0, 10)
                    .Select(_ => space.CreateObject<JS>()).Commit()))
                .ToObservable().To(true);

    }

    [JobProvider]
    public class ChainJob:TestJob {
        [JobProvider]
        public async Task<bool> TestChainJob(PerformContext context) {
            Context = context;
            JobsSubject.OnNext(this);
            var returnObservable = await Result.Observe();
            return returnObservable;
        }
        [JobProvider]
        public void TestVoidChainJob(PerformContext context) {
            Context = context;
            JobsSubject.OnNext(this);
        }

        public static bool Result;

    }

    [JobProvider]
    public class TestJob {
        protected static readonly ISubject<TestJob> JobsSubject=Subject.Synchronize(new Subject<TestJob>());

        public static IObservable<TestJob> Jobs => JobsSubject.AsObservable().Delay(1.Seconds());

        public PerformContext Context { get; protected set; }

        public TestJob() { }
        public IServiceProvider Provider { get; }

        [ActivatorUtilitiesConstructor]
        protected TestJob(IServiceProvider provider) => Provider = provider;

        [AutomaticRetry(Attempts = 0)][JobProvider]
        public void FailMethodNoRetry() => throw new NotImplementedException();

        [AutomaticRetry(Attempts = 1,DelaysInSeconds = new[] {1})][JobProvider]
        public void FailMethodRetry() => throw new NotImplementedException();

        [JobProvider]
        public void Test() => JobsSubject.OnNext(this);

        [JobProvider]
        public void TestJobId(PerformContext context) {
            Context = context;
            
            JobsSubject.OnNext(this);
        }
    }
}