using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.XAF.Modules.Reactive.Services {
    public static class ServiceProviderExtensions {
        #region High-Level Logical Operations
        public static IObservable<Unit> WhenApplicationStarted(this IServiceProvider serviceProvider) 
            => serviceProvider.WhenLifeTimeEvent(lifetime => lifetime.ApplicationStarted).PushStackFrame();

        public static IObservable<Unit> WhenApplicationStopping(this IServiceProvider serviceProvider) 
            => serviceProvider.WhenLifeTimeEvent(lifetime => lifetime.ApplicationStopping).PushStackFrame();
        
        public static IObservable<Unit> WhenApplicationStopped(this IServiceProvider serviceProvider) 
            => serviceProvider.WhenLifeTimeEvent(lifetime => lifetime.ApplicationStopped).PushStackFrame();
        #endregion

        #region Low-Level Plumbing
        public static void StopApplication(this IServiceProvider serviceProvider) 
            => serviceProvider.GetRequiredService<IHostApplicationLifetime>().StopApplication();
    
        private static IObservable<Unit> WhenLifeTimeEvent(this IServiceProvider serviceProvider,Func<IHostApplicationLifetime,CancellationToken> theEvent){
            var subject = new Subject<Unit>();
            theEvent(serviceProvider.GetRequiredService<IHostApplicationLifetime>()).Register(_ => {
                if (subject.IsDisposed)return;
                subject.OnNext();
            }, null);
            return subject.AsObservable().Take(1).Finally(() => subject.Dispose());
        }
    
        public static void Decorate<TInterface, TDecorator>(this IServiceCollection services) where TInterface : class where TDecorator : class, TInterface {
            var wrappedDescriptor = services.FirstOrDefault((Func<ServiceDescriptor, bool>) (s => s.ServiceType == typeof (TInterface)));
            if (wrappedDescriptor == null)
                throw new InvalidOperationException("TypeIsNotRegistered");
            var objectFactory = ActivatorUtilities.CreateFactory(typeof (TDecorator), [typeof (TInterface)]);
            services.Replace(ServiceDescriptor.Describe(typeof (TInterface), (Func<IServiceProvider, object>) (s => (TInterface) objectFactory(s, [
                s.CreateInstance(wrappedDescriptor)
            ])), wrappedDescriptor.Lifetime));
        }

        public static object CreateInstance(this IServiceProvider services, ServiceDescriptor descriptor) 
            => descriptor.ImplementationInstance ?? (descriptor.ImplementationFactory != null ? descriptor.ImplementationFactory(services) :
                ActivatorUtilities.GetServiceOrCreateInstance(services, descriptor.ImplementationType!));
        #endregion
    }
}