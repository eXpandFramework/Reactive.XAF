using System;
using DevExpress.XtraPrinting.Native;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Xpand.Extensions.Blazor;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire {
    internal class JobFilterAttribute:global::Hangfire.Common.JobFilterAttribute, IApplyStateFilter {
        private readonly IServiceProvider _provider;

        public JobFilterAttribute(IServiceProvider provider) => _provider = provider;

        public virtual void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction) {
            var recurringJobId = $"{context.Connection.GetJobParameter(context.BackgroundJob.Id, "RecurringJobId")}"
                .Replace(@"\", "").Replace(@"""", "");
            if (!string.IsNullOrEmpty(recurringJobId)) {
                var sharedXafApplicationProvider = _provider.GetService<ISharedXafApplicationProvider>();
                var blazorApplication = sharedXafApplicationProvider.Application;
                blazorApplication.ApplyJobState(context, recurringJobId);
            }
        }

        public virtual void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction) {
            
        }
    }

    
}