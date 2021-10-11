using System;
using System.Diagnostics;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.Persistent.Base;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Blazor;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
[assembly: HostingStartup(typeof(HostingStartup))]
namespace Xpand.TestsLib.Blazor {
    public abstract class XafHostingStartup<TModule> where TModule : ModuleBase, new() {
        public IConfiguration Configuration { get; }

        protected XafHostingStartup(IConfiguration configuration) => Configuration = configuration;

        public void Configure(IApplicationBuilder app) => app.UseXaf();

        public virtual void ConfigureServices(IServiceCollection services) {
            Tracing.Initialize(AppDomain.CurrentDomain.ApplicationPath(),TraceLevel.Verbose.ToString());
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddHttpContextAccessor();
            services.AddXaf<TestBlazorApplication>(Configuration);
            BlazorApplication NewApplication() => Platform.Blazor.NewApplication<TModule>().ToBlazor();
            services.AddSingleton<IXafApplicationFactory>(provider => new XafApplicationFactory(() => {
                var blazorApplication = NewApplication();
                blazorApplication.ServiceProvider=provider;
                return blazorApplication;
            }));
            var appProviderMock = new Mock<IXafApplicationProvider>();
            appProviderMock.Setup(provider => provider.GetApplication()).Returns(NewApplication);
            services.AddSingleton(_ => appProviderMock.Object)
                .AddSingleton<ISharedXafApplicationProvider, TestXafApplicationProvider<TModule>>()
                .AddSingleton<XpoDataStoreProviderAccessor>()
                .AddScoped<IExceptionHandlerService,MyClass>();

        }
        class MyClass:IExceptionHandlerService {
            public void HandleException(Exception exception) {
                throw exception;
            }
        }


        internal sealed class XafApplicationFactory : IXafApplicationFactory {
            private readonly Func<BlazorApplication> _createApplication;
            public XafApplicationFactory(Func<BlazorApplication> createApplication) 
                => _createApplication = createApplication ?? throw new ArgumentNullException(nameof(createApplication));
            public BlazorApplication CreateApplication() => _createApplication();
        }

    }
}