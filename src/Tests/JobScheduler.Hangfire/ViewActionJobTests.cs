using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Blazor;
using Xpand.Extensions.XAF.FrameExtensions;
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
		[TestCase("JS_ListView",typeof(SimpleAction))]
        [TestCase("JS_ListView",typeof(SingleChoiceAction))]
        [TestCase("JS_DetailView",typeof(SingleChoiceAction))]
        [TestCase("JS_DetailView",typeof(SimpleAction))]
        [XpandTest()]
		public async Task Execute_Action(string viewId,Type actionType) {
			await using var application = NewBlazorApplication().ToBlazor();
			var actionId = "test";
			var whenApplicationModulesManager = application.WhenApplicationModulesManager();
			whenApplicationModulesManager.SelectMany(manager => manager.RegisterViewAction(actionId,
					t => CreateAction(t, actionId,actionType))).Test();
			JobSchedulerModule(application);
			using var testObserver = application.WhenViewOnFrame(typeof(JS))
				.SelectMany(frame => frame.Action(actionId).WhenExecuted()).FirstAsync().Test();
			var objectSpace = application.CreateObjectSpace();
			objectSpace.CreateObject<JS>();
			var executeActionJob = objectSpace.CreateObject<ExecuteActionJob>();
			executeActionJob.Action = new ObjectString(actionId);
			executeActionJob.Object = new ObjectType(typeof(JS));
			executeActionJob.View = new ObjectString(viewId);
			executeActionJob.CronExpression = objectSpace.GetObjectsQuery<CronExpression>().First();
			executeActionJob.Id = nameof(Execute_Action);
			objectSpace.CommitChanges();

			await application.ExecuteAction(executeActionJob).FirstAsync();

			testObserver.AwaitDone(Timeout).ItemCount.ShouldBe(1);
		}

        private static ActionBase CreateAction((ViewController controller, string id) t, string actionId, Type actionType) 
	        => actionType == typeof(SimpleAction) ? new SimpleAction(t.controller, actionId, PredefinedCategory.View)
		        : new SingleChoiceAction(t.controller, actionId, PredefinedCategory.View) { Items = { new ChoiceActionItem("test", "test") } };

	}
}