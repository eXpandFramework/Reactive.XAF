using System;
using System.Linq;
using DevExpress.ExpressApp.Blazor;
using Fasterflect;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Xpand.Extensions.Blazor;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire {
    public class ServiceJobActivatorScope : JobActivatorScope {
        readonly IServiceScope _serviceScope;
        public ServiceJobActivatorScope(IServiceScope serviceScope) => _serviceScope = serviceScope ;

        public override object Resolve(Type type) {
            var constructors = type.Constructors(Flags.InstancePublic);
            if (constructors.Any(info => info.Parameters().Count == 1 && info.Parameters()
                .Any(parameterInfo => parameterInfo.ParameterType == typeof(BlazorApplication)))) {
                var sharedApplication = _serviceScope.ServiceProvider.GetService<ISharedXafApplicationProvider>()?.Application;
                return type.CreateInstance(sharedApplication);
            }
            return ActivatorUtilities.GetServiceOrCreateInstance(_serviceScope.ServiceProvider, type);
            
        }
    }

    public class ServiceJobActivator : JobActivator {
        readonly IServiceScopeFactory _serviceScopeFactory;
        public ServiceJobActivator(IServiceScopeFactory serviceScopeFactory) => _serviceScopeFactory = serviceScopeFactory ;

        public override JobActivatorScope BeginScope(JobActivatorContext context) => new ServiceJobActivatorScope(_serviceScopeFactory.CreateScope());
    }

}