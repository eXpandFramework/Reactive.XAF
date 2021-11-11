using System;
using Hangfire;
using Hangfire.AspNetCore;
using Microsoft.Extensions.DependencyInjection;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Hangfire {
    public class ServiceJobActivatorScope : JobActivatorScope {
        readonly IServiceScope _serviceScope;
        public ServiceJobActivatorScope(IServiceScope serviceScope) => _serviceScope = serviceScope;

        public override void DisposeScope() {
            base.DisposeScope();
            _serviceScope.Dispose();
        }

        public override object Resolve(Type type) => ActivatorUtilities.GetServiceOrCreateInstance(_serviceScope.ServiceProvider, type);
    }

    public class ServiceJobActivator : AspNetCoreJobActivator {
        readonly IServiceScopeFactory _serviceScopeFactory;
        public ServiceJobActivator(IServiceScopeFactory serviceScopeFactory) : base(serviceScopeFactory) => _serviceScopeFactory = serviceScopeFactory ;

        public override JobActivatorScope BeginScope(JobActivatorContext context) => new ServiceJobActivatorScope(_serviceScopeFactory.CreateScope());
        
    }

}