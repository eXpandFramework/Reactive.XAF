// using System.Linq;
// using NUnit.Framework;
// using Shouldly;
// using Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.Email.Tests.BO;
// using Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.Email.Tests.Common;
// using Xpand.XAF.Modules.Reactive;
//
// namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.Email.Tests {
// 	
// 	public class EmailNotificationServiceTests:CommonAppTest {
// 		public override void Init() {
// 			base.Init();
// 			var emailTypes = ((IModelNotificationEmail)((IModelJobSchedulerNotification)Application.Model
// 					.ToReactiveModule<IModelReactiveModulesJobScheduler>().JobScheduler).Notification).EmailTypes;
// 			var emailType = emailTypes.AddNode<IModelNotificationEmailType>();
// 			emailType.Type = emailType.Application.BOModel.GetClass(typeof(JSNEE));
// 		}
//
// 		[Test]
// 		public void Email_Channel_Lookup_Contains_Model_Types() {
// 			var objectSpace = Application.CreateObjectSpace();
// 			var email = objectSpace.CreateObject<BusinessObjects.Email>();
// 			
// 			email.Types.Count.ShouldBe(1);
// 			email.Types.First().Type.ShouldBe(typeof(JSNEE));
// 		}
//
// 	
// 			
// 	}
// }