using System;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xpand.Extensions.Blazor;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Hangfire {
    public class HangfireJobFilter:global::Hangfire.Common.JobFilterAttribute, IApplyStateFilter,IServerFilter,IElectStateFilter,IHangfireJobFilter {
        private readonly IServiceProvider _provider;
        public HangfireJobFilter(IServiceProvider provider) => _provider = provider;

        public virtual void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction) {
            var recurringJobId = context.Connection.RecurringJobId(context.BackgroundJob.Id);
            if (!string.IsNullOrEmpty(recurringJobId)) {
	            using var serviceScope = _provider.CreateScope();
                var serviceProvider = serviceScope.ServiceProvider;
                ApplyJobState(context, serviceProvider);
            }
        }

        protected virtual void ApplyJobState(ApplyStateContext context, IServiceProvider serviceProvider) 
	        => serviceProvider.RunWithStorage(context.ApplyJobState);

        public void OnStateElection(ElectStateContext context) {
        }
        
        public virtual void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction) { }

        public void OnPerforming(PerformingContext performingContext) 
            => performingContext.Canceled=performingContext.Connection
                .GetAllItemsFromSet(JobSchedulerService.PausedJobsSetName)
                .Contains(performingContext.Connection.RecurringJobId(performingContext.BackgroundJob.Id));

        public void OnPerformed(PerformedContext filterContext) {
            
        }
    }

    public interface IHangfireJobFilter { }
}