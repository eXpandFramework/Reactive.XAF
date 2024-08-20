using System;
using Hangfire;
using Hangfire.AspNetCore;
using Microsoft.Extensions.DependencyInjection;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Hangfire {
    public class ServiceJobActivatorScope(IServiceScope serviceScope) : JobActivatorScope {
        public override void DisposeScope() {
            base.DisposeScope();
            serviceScope.Dispose();
        }

        public override object Resolve(Type type) => ActivatorUtilities.GetServiceOrCreateInstance(serviceScope.ServiceProvider, type);
    }

    public class ServiceJobActivator(IServiceScopeFactory serviceScopeFactory)
        : AspNetCoreJobActivator(serviceScopeFactory) {
        readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

        public override JobActivatorScope BeginScope(JobActivatorContext context) => new ServiceJobActivatorScope(_serviceScopeFactory.CreateScope());
        
    }

}