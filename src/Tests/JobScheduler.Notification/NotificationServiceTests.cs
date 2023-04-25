using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp.Blazor;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Blazor;
using Xpand.Extensions.XAF.NonPersistentObjects;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.BusinessObjects;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.Tests.BO;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.Tests.Common;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.Tests {
	
	public class NotificationServiceTests:CommonTest {
		
		protected override BlazorApplication NewBlazorApplication() {
			var application = base.NewBlazorApplication();
			application.CreateExistingObjects<JSNE>().Test(); 
			return application;
		}

		[Test]
		[XpandTest()][Order(0)]
		public async Task Persist_Last_Index_For_All_Notification_Jobs_At_Startup() {
			using var application = JobSchedulerNotificationModule().Application;

			var objectSpace = application.CreateObjectSpace();

			var notificationJobIndex = objectSpace.GetObjectsQuery<NotificationJobIndex>()
				.FirstOrDefault(index => index.ObjectType==typeof(JSNE));

			notificationJobIndex.ShouldNotBeNull();
			notificationJobIndex.Index.ShouldBe(1);

			await WebHost.StopAsync();
		}
		[Test]
		[XpandTest()][Order(1000)]
		public async Task WhenNotification_Emits_Non_Indexed_Objects_For_Existing_Jobs() {
			await using var application = JobSchedulerNotificationModule().Application.ToBlazor();
			await WhenNotification_Emits_Non_Indexed_Objects(application);
			await WebHost.StopAsync();
		}

		private async Task WhenNotification_Emits_Non_Indexed_Objects(BlazorApplication application,Type objectType=null) {
			objectType ??= typeof(JSNE);
			var objectSpace = application.CreateObjectSpace();
			var jsne =(IJSNE) objectSpace.CreateObject(objectType);
			objectSpace.CommitChanges();
			var jsneTestObserver = application.WhenNotification(objectType).SelectMany(t => t.objects).FirstAsync().Cast<IJSNE>().Test();
			var notificationJobIndexTestObserver = application.WhenCommitted<NotificationJobIndex>().SelectMany(t => t.objects).FirstAsync().Test();
			var notificationJob = objectSpace.GetObjectsQuery<ObjectStateNotification>().First();
			var jobWorker = notificationJob.ObjectSpace.CreateObject<JobWorker>();
			jobWorker.Job=notificationJob;
			jobWorker.Id = notificationJob.Id;
			await jobWorker.Commit();

			await application.NotificationJob(jobWorker).FirstOrDefaultAsync().Timeout(Timeout);

			jsneTestObserver.AwaitDone(Timeout).ItemCount.ShouldBe(1);
			jsneTestObserver.Items.First().Index.ShouldBe(jsne.Index);
			notificationJobIndexTestObserver.AwaitDone(Timeout);
			application.CreateObjectSpace().GetObjectsQuery<NotificationJobIndex>()
				.First(index => index.ObjectType == objectType).Index.ShouldBe(jsne.Index);
		}
		
		[Test()]
		[XpandTest()][Order(3000)][Ignore("")]
		public async Task WhenNotification_For_Job_Created_After_Startup() {
			await using var application = JobSchedulerNotificationModule().Application.ToBlazor();
			var objectSpace = application.CreateObjectSpace();
			objectSpace.Delete(objectSpace.GetObjectsQuery<ObjectStateNotification>().ToArray());
			objectSpace.Delete(objectSpace.GetObjectsQuery<NotificationJobIndex>().ToArray());
			objectSpace.CommitChanges();
			objectSpace.CreateObject<NotificationJobIndex>();
			objectSpace.ModifiedObjects.Count.ShouldBe(1);
			objectSpace.GetObjectsQuery<NotificationJobIndex>().ToArray().Length.ShouldBe(0);
			var testObserver = application.WhenCommitted<NotificationJobIndex>()
				.SelectMany(t => t.objects)
				.FirstAsync().Finally(() => {}).Timeout(Timeout).SubscribeOn(Scheduler.Default).Test();
			objectSpace.NewNotificationJob();
			objectSpace.CommitChanges();
			
			testObserver.AwaitDone(Timeout);

			await WhenNotification_Emits_Non_Indexed_Objects(application);
			await WebHost.StopAsync();
			
		}
		[Test]
		[XpandTest()][Order(3000)]
		public async Task WhenNotification_For_Modified_Job_Type() {
			await using var application = NewBlazorApplication().ToBlazor();
			application.CreateExistingObjects<JSNE2>().Test();
			application.JobSchedulerNotificationModule();
			
			var testObserver = application.WhenCommitted<NotificationJobIndex>().SelectMany(t => t.objects).FirstAsync(index => index.ObjectType==typeof(JSNE2)).Timeout(Timeout).Test();
			var objectSpace = application.CreateObjectSpace();
			var notificationJob = objectSpace.GetObjectsQuery<ObjectStateNotification>().First();
			notificationJob.Object = new ObjectType(typeof(JSNE2));
			notificationJob.ObjectSpace.CommitChanges();
			
			testObserver.AwaitDone(Timeout);
			
			await WhenNotification_Emits_Non_Indexed_Objects(application,typeof(JSNE2));
			await WebHost.StopAsync();
		}
		
	
			
	}
}