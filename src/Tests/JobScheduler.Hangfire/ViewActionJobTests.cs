using System;
using System.Reactive.Linq;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Xpand.Extensions.Blazor;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.Common;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests {
	[NonParallelizable]
	public class ViewActionJobTests:JobSchedulerCommonTest {
		[TestCase(false)]
		[TestCase(true)]
		[XpandTest()]
		public void Customize_Job_Schedule(bool newObject) {
            GlobalConfiguration.Configuration.UseMemoryStorage();
			var application = NewBlazorApplication();
			using var _ = application.WhenApplicationModulesManager()
				.SelectMany(manager => manager.RegisterViewSimpleAction("test"))
				.Subscribe();
			JobSchedulerModule(application);
                
			application.ServiceProvider.GetService<ISharedXafApplicationProvider>()
				?.Application.WhenFrameViewChanged()
				.WhenFrame();
		}

	}
}