using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.AmbientContext;
using DevExpress.ExpressApp.AspNetCore.Model;
using DevExpress.ExpressApp.AspNetCore.WebApi.Core;
using DevExpress.ExpressApp.Core.Internal;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.MultiTenancy.Internal;
using DevExpress.ExpressApp.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Xpand.TestsLib.Blazor {
    internal class SharedApplicationProvider(
        SharedApplicationCreator applicationCreator,
        IOptions<MultiTenancyOptions> multiTenancyOptions,
        IOptions<ApplicationWarmUpOptions> applicationWarmUpOptions)
        : ISharedApplicationProvider {
        private readonly ConcurrentDictionary<string, Lazy<SharedApplicationContainer>> _dictionary = new();

        ISharedApplicationContainer ISharedApplicationProvider.GetContainer() {
            var options = new SharedApplicationCreationParameters {
                UseInMemoryDatabaseForSharedApplication =
                    multiTenancyOptions.Value.UseInMemoryDatabaseForSharedApplication
            };
            if (applicationWarmUpOptions.Value.WarmUpSharedApplicationModel) {
                options.ApplicationKey = CultureInfo.InvariantCulture.Name;
                options.UseSharedModelForSharedApplication = true;
            }
            else {
                options.ApplicationKey = Thread.CurrentThread.CurrentUICulture.Name;
            }

            return GetSharedApplicationContainer(options);
        }

        ISharedApplicationContainer ISharedApplicationProvider.GetContainer(
            SharedApplicationCreationParameters options)
            => GetSharedApplicationContainer(options);

        private SharedApplicationContainer GetSharedApplicationContainer(
            SharedApplicationCreationParameters options) {
            return _dictionary.GetOrAdd(options.ApplicationKey ?? Thread.CurrentThread.CurrentUICulture.Name,
                    (Func<string, Lazy<SharedApplicationContainer>>)(_
                        => new Lazy<SharedApplicationContainer>(
                            (Func<SharedApplicationContainer>)(()
                                => applicationCreator.CreateSharedApplication(options)))))
                .Value;
        }
    }
}

internal class SharedApplicationCreator(
    IServiceProvider serviceProvider) {
    public SharedApplicationContainer CreateSharedApplication(
        SharedApplicationCreationParameters options) {
        var scope = serviceProvider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IValueManagerStorageContext>().RunWithStorage(
            (Func<SharedApplicationContainer>)(() => {
                _ = scope.ServiceProvider.GetRequiredService<IOptions<MultiTenancyOptions>>().Value;
                var requiredService1 = scope.ServiceProvider.GetRequiredService<ITypesInfo>();
                var requiredService2 = scope.ServiceProvider.GetRequiredService<IApplicationModelManagerContainer>();
                var application = scope.ServiceProvider.GetRequiredService<IWebApiApplicationFactory>()
                    .CreateApplication(requiredService1);
                requiredService2.UseSharedModel = options.UseSharedModelForSharedApplication;
                if (options.UseInMemoryDatabaseForSharedApplication)
                    SharedApplicationHelper.SetSharedApplicationTenant(application.ServiceProvider);
                application.Setup();
                application.LoggingOn += (_, _)
                    => throw new InvalidOperationException("Shared application should never be logon");
                if (application.Security.IsAuthenticated &&
                    scope.ServiceProvider.GetRequiredService<IPrincipalProvider>().User.Identity?.AuthenticationType !=
                    "SecurityDummy")
                    throw new InvalidOperationException(
                        "Shared application shouldn't have credentials for automatic logon");
                var afterSetup = options.AfterSetup;
                afterSetup?.Invoke(application);
                return new SharedApplicationContainer(application, scope);
            }));
    }
}

internal class SharedApplicationContainer(XafApplication xafApplication, IServiceScope serviceScope)
    : ISharedApplicationContainer {
    XafApplication ISharedApplicationContainer.Application => xafApplication;

    IServiceScope ISharedApplicationContainer.ServiceScope => serviceScope;
}