using System;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xpand.Extensions.Blazor;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Hangfire {
    public class HangfireJobFilter(IServiceProvider provider) : global::Hangfire.Common.JobFilterAttribute,
        IApplyStateFilter, IServerFilter, IElectStateFilter, IHangfireJobFilter {
        public virtual void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction) {
            var recurringJobId = context.Connection.RecurringJobId(context.BackgroundJob.Id);
            if (!string.IsNullOrEmpty(recurringJobId)) {
	            using var serviceScope = provider.CreateScope();
                ApplyJobState(context, serviceScope.ServiceProvider);
            }
        }

        protected virtual void ApplyJobState(ApplyStateContext context, IServiceProvider serviceProvider) 
	        => serviceProvider.RunWithStorage(context.ApplyJobState);
        
        protected virtual void ApplyPaused(PerformingContext context, IServiceProvider serviceProvider) 
	        => serviceProvider.RunWithStorage(context.ApplyPaused);

        public void OnStateElection(ElectStateContext context) {
            if (!context.GetJobParameter<bool>("Cancel")) return;
            context.CandidateState = new SkippedState("Paused");
        }
        
        public virtual void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction) { }

        public void OnPerforming(PerformingContext performingContext) {
            using var serviceScope = provider.CreateScope();
            ApplyPaused(performingContext, serviceScope.ServiceProvider);
            performingContext.Canceled = true;
            performingContext.SetJobParameter("Cancel",performingContext.Canceled);
        }

        public void OnPerformed(PerformedContext filterContext) {
            
        }
    }

    public interface IHangfireJobFilter;
}