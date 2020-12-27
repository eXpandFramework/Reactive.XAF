using System;
using DevExpress.XtraPrinting.Native;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using Xpand.Extensions.Blazor;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire {
    internal class HangfireJobFilterState:global::Hangfire.Common.JobFilterAttribute, IApplyStateFilter,IServerFilter {
        private readonly IServiceProvider _provider;

        public HangfireJobFilterState(IServiceProvider provider) => _provider = provider;

        public virtual void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction) {
            var recurringJobId = RecurringJobId(context.BackgroundJob.Id, context.Connection);
            if (!string.IsNullOrEmpty(recurringJobId)) {
                var sharedXafApplicationProvider = _provider.GetService<ISharedXafApplicationProvider>();
                var blazorApplication = sharedXafApplicationProvider.Application;
                blazorApplication.ApplyJobState(context, recurringJobId);
            }
        }

        private static string RecurringJobId(string backgroundJobId, IStorageConnection connection) 
            => $"{connection.GetJobParameter(backgroundJobId, "RecurringJobId")}".Replace(@"\", "").Replace(@"""", "");

        public virtual void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction) { }

        public void OnPerforming(PerformingContext filterContext) {
            var values = filterContext.Connection.GetAllItemsFromSet(JobService.PausedJobsSetName);
            var item = RecurringJobId(filterContext.BackgroundJob.Id,filterContext.Connection);
            if (values.Contains(item)) {
                filterContext.Canceled = true;
            }
        }

        public void OnPerformed(PerformedContext filterContext) { }
    }

    
}