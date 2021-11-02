using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.SystemModule;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Blazor;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.NonPersistentObjects;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.BO;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.Common;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests {
	[NonParallelizable]
	public class ViewActionJobTests:JobSchedulerCommonTest {
        [TestCase("JS_DetailView","New")]
        [TestCase("JS_ListView","New")]
        [TestCase("JS_ListView","Delete")]
        [TestCase("JS_DetailView","Delete")]
		[XpandTest()]
		public async Task Execute_Action(string viewId,string actionId) {
            GlobalConfiguration.Configuration.UseMemoryStorage();
			using var application = NewBlazorApplication().ToBlazor();
			JobSchedulerModule(application);
			var blazorApplication = application.ServiceProvider.GetRequiredService<ISharedXafApplicationProvider>().Application;
			using var testObserver = ActionExecuted(blazorApplication,actionId).FirstAsync().Test();
			var objectSpace = application.CreateObjectSpace();
			objectSpace.CreateObject<JS>();
			var executeActionJob = objectSpace.CreateObject<ExecuteActionJob>();
			executeActionJob.Action = new ObjectString(actionId);
			executeActionJob.Object = new ObjectType(typeof(JS));
			executeActionJob.View = new ObjectString(viewId);
			executeActionJob.CronExpression = objectSpace.GetObjectsQuery<CronExpression>().First();
			executeActionJob.Id = nameof(Execute_Action);
			objectSpace.CommitChanges();

			await blazorApplication.ExecuteAction(executeActionJob);

			testObserver.AwaitDone(Timeout).ItemCount.ShouldBe(1);
		}

        private static IObservable<Unit> ActionExecuted(BlazorApplication blazorApplication,string actionId) 
	        => actionId == "Delete" ? blazorApplication.WhenViewOnFrame(typeof(JS)).ToController<DeleteObjectsViewController>()
			        .SelectMany(controller => controller.DeleteAction.WhenExecuted()).ToUnit()
		        : blazorApplication.WhenViewOnFrame(typeof(JS)).ToController<NewObjectViewController>()
			        .SelectMany(controller => controller.NewObjectAction.WhenExecuting().Do(t => t.e.Cancel=true)).ToUnit();
	}
}