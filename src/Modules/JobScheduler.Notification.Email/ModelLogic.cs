using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.Persistent.Base;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.Email{
	public interface IModelNotificationEmail : IModelNode {
		IModelNotificationEmailTypes EmailTypes { get; }
	}
	
	[ModelNodesGenerator(typeof(ModelNotificationEmailTypesModelGenerator))]
	public interface IModelNotificationEmailTypes :IModelList<IModelNotificationEmailType>, IModelNode {
		
	}
	public class ModelNotificationEmailTypesModelGenerator:ModelNodesGeneratorBase {
		protected override void GenerateNodesCore(ModelNode node) { }
	}

	[KeyProperty(nameof(TypeId))]
	public interface IModelNotificationEmailType : IModelNode {
		[DataSourceProperty(nameof(IModelNotificationType.Types))]
		[Required][RefreshProperties(RefreshProperties.All)]
		IModelClass Type { get; set; }
		[Browsable(false)]
		string TypeId{ get; set; }
		
		[Browsable(false)]
		IModelList<IModelClass> Types { get; }
	}

	[DomainLogic(typeof(IModelNotificationEmailType))]
	public static class ModelNotificationEmailTypeLogic {
		internal static IModelNotificationEmail EmailModel(this IModelApplication application)
			=> (IModelNotificationEmail)((IModelJobSchedulerNotification)application
				.ToReactiveModule<IModelReactiveModulesJobScheduler>().JobScheduler).Notification;
		
		public static IModelList<IModelClass> Get_Types(IModelNotificationEmailType emailType) 
			=> emailType.Application.BOModel
				.Where(c => c.AllMembers.Any(member => member.MemberInfo.MemberType==typeof(string)))
				.ToCalculatedModelNodeList();
	}


	
}
