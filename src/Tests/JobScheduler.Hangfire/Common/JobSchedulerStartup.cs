using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.AmbientContext;
using DevExpress.ExpressApp.Blazor.Services;
using Fasterflect;
using Hangfire.Server;
using Hangfire.States;
using HarmonyLib;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xpand.Extensions.Blazor;
using Xpand.Extensions.Harmony;
using Xpand.TestsLib.Blazor;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Hangfire;


namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.Common {
    public class JobSchedulerStartup : XafHostingStartup<JobSchedulerModule> {
        static JobSchedulerStartup() {
            // new HarmonyMethod(typeof(JobSchedulerStartup),nameof(SetStorage))
                // .PreFix(typeof(ValueManagerContext).Method(nameof(ValueManagerContext.SetStorage),Flags.StaticAnyVisibility));
        }
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static bool SetStorage(IValueManagerStorage storage,ref IDisposable __result) {
            if (ValueManagerContext.Storage != null) {
                __result = (IDisposable)AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes()).First(type => type.Name.StartsWith("StorageScope"))
                    .CreateInstance(typeof(ValueManagerContext).GetFieldValue("storageHolder").GetPropertyValue("Value"),storage);
                return false;
            }
            return true;
        }

        public JobSchedulerStartup(IConfiguration configuration) : base(configuration) { }

        public override void ConfigureServices(IServiceCollection services) {
            base.ConfigureServices(services);
            // services.AddSingleton<IHangfireJobFilter,HangfireJobFilter>();
            services.AddSingleton<IBackgroundProcessingServer, BackgroundProcessingServer>();
        }
    }

    class HangfireJobFilter:Hangfire.HangfireJobFilter {
	    public HangfireJobFilter(IServiceProvider provider) : base(provider) { }

	    protected override void ApplyJobState(ApplyStateContext context, IServiceProvider serviceProvider) 
            => ValueManagerContext.RunIsolated(() => context.ApplyJobState(GetApplication(serviceProvider)));

        private static BlazorApplication GetApplication(IServiceProvider serviceProvider) 
            => serviceProvider.GetRequiredService<IXafApplicationProvider>().GetApplication();

        protected override void ApplyPaused(PerformingContext context, IServiceProvider serviceProvider) 
            => ValueManagerContext.RunIsolated(() => context.ApplyPaused(GetApplication(serviceProvider)));
    }

}