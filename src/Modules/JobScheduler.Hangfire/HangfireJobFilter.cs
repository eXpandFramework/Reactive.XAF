using System;
using DevExpress.XtraPrinting.Native;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using Xpand.Extensions.Blazor;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire {
    public class HangfireJobFilter:global::Hangfire.Common.JobFilterAttribute, IApplyStateFilter,IServerFilter {
        private readonly IServiceProvider _provider;

        public HangfireJobFilter(IServiceProvider provider) => _provider = provider;

        public virtual void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction) {
            var recurringJobId = context.Connection.RecurringJobId(context.BackgroundJob.Id);
            if (!string.IsNullOrEmpty(recurringJobId)) {
                var sharedXafApplicationProvider = _provider.GetService<ISharedXafApplicationProvider>();
                var blazorApplication = sharedXafApplicationProvider.Application;
                blazorApplication.ApplyJobState(context, recurringJobId);
            }
        }

        public virtual void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction) { }

        public void OnPerforming(PerformingContext performingContext) 
            => performingContext.Canceled = performingContext.Connection.GetAllItemsFromSet(JobSchedulerService.PausedJobsSetName)
                .Contains(performingContext.Connection.RecurringJobId(performingContext.BackgroundJob.Id));

        public void OnPerformed(PerformedContext filterContext) { }
    }

    
}