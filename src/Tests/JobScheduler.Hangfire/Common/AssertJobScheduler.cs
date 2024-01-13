using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.Common {
    public static class AssertJobScheduler {
        
        public static IObservable<Window> AssertJobListViewNavigation(this BlazorApplication application) 
            => application.AssertNavigation(() => application.FindListViewId(typeof(Job)));

        public static IObservable<Frame> CreateJob(this Window window,Type jobType,string methodName,bool closeView=true)
            => window.NewObject(typeof(Job),closeView,detailview: frame => {
                var job = ((Job)frame.View.CurrentObject);
                job.JobType = job.JobTypes.First(type => type.Type == jobType);
                job.JobMethod = job.JobMethods.First(s => s.Name == methodName);
                job.Id = Guid.NewGuid().ToString();
                return Observable.Empty<Unit>();
            }).To(window);

        public static IObservable<T> AssertTriggerJob<T>(this Frame frame, IObservable<T> afterExecuted)
            => frame.AssertSimpleAction(nameof(JobSchedulerService.TriggerJob))
                .SelectMany(action => action.Trigger(afterExecuted).ReplayFirstTake());

        public static IObservable<Unit> AssertTriggerJob(this BlazorApplication application,Type jobType,string methodName, bool saveAndClose)
            => application.AssertTriggerJob(jobType, methodName, _ => Observable.Empty<Unit>(),saveAndClose);
        
        public static IObservable<Unit> AssertTriggerJob(this BlazorApplication application,Type jobType,string methodName, Func<Frame,IObservable<Unit>> afterExecuted,bool saveAndClose=true)
            => application.AssertJobListViewNavigation()
                .SelectMany(window => window.CreateJob(jobType,methodName,saveAndClose))
                .If(_ =>saveAndClose,frame => application.AssertListViewHasObject<Job>().SelectMany(_ => frame.AssertTriggerJob(afterExecuted(frame))),
                    frame => frame.SaveAction().Trigger(frame.AssertTriggerJob(afterExecuted(frame)))).ToUnit()
                .ReplayFirstTake();
    }
}