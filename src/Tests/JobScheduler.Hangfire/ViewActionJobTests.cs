using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using NUnit.Framework;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.BO;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.Common;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests {
	[NonParallelizable]
	public class ViewActionJobTests:JobSchedulerCommonTest {

        private static ActionBase CreateAction((ViewController controller, string id) t, string actionId, Type actionType) 
	        => actionType == typeof(SimpleAction) ? new SimpleAction(t.controller, actionId, PredefinedCategory.View)
		        : new SingleChoiceAction(t.controller, actionId, PredefinedCategory.View) { Items = { new ChoiceActionItem("test", "test") } };

        [TestCase("JS_ListView",typeof(SimpleAction))]
        [TestCase("JS_DetailView",typeof(SimpleAction))][XpandTest(state:ApartmentState.MTA)]
        public async Task Execute_Action(string viewId, Type actionType) {
	        await StartJobSchedulerTest(application
		        => application.WhenApplicationModulesManager()
			        .SelectMany(manager => manager.RegisterViewAction("test",
				        t => CreateAction(t, "test",actionType)).WhenExecuted().Select(e => e))
			        .MergeToUnit(application.WhenMainWindowCreated()
				        .SelectMany(_ => application.AssertJobListViewNavigation()
					        .SelectMany(frame => frame.NewObject(typeof(ExecuteActionJob),detailview: frame1 => {
						        var executeActionJob = ((ExecuteActionJob)frame1.View.CurrentObject);
						        executeActionJob.Action = executeActionJob.Actions.First(s => s.Name=="test");
						        executeActionJob.Object = executeActionJob.Objects.First(type => type.Type==typeof(JS));
						        executeActionJob.View = executeActionJob.Views.First(s => s.Name==viewId);
						        executeActionJob.CronExpression = frame1.View.ObjectSpace.GetObjectsQuery<CronExpression>().First();
						        executeActionJob.Id = nameof(Execute_Action);
						        executeActionJob.CommitChanges();
						        return Observable.Empty<Unit>();
					        }))
					        .SelectMany(frame => frame.SimpleAction("test").Trigger().IgnoreElements())))
			        .ToUnit().ReplayFirstTake());
        }
	}
}