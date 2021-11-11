using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.Persistent.Base;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Notification{
	public interface IModelJobSchedulerNotification : IModelNode {
		IModelNotification Notification { get; }
	}

	[ModelNodesGenerator(typeof(ModelJobSchedulerNotificationTypesModelGenerator))]
	public interface IModelNotificationTypes :IModelList<IModelNotificationType>, IModelNode {
		
	}
	public class ModelJobSchedulerNotificationTypesModelGenerator:ModelNodesGeneratorBase {
		protected override void GenerateNodesCore(ModelNode node) { }
	}

	class NotificationType:INotificationType {
		public NotificationType(INotificationType notificationType) {
			Type = notificationType.Type.TypeInfo.Type;
			Member = notificationType.ObjectIndexMember.MemberInfo;
		}
		public IMemberInfo Member { get; }
		public Type Type { get; }
		
		public override string ToString() => $"{Type?.Name}-{Member?.Name}";

		IModelClass INotificationType.Type { get; set; }
		IModelMember INotificationType.ObjectIndexMember { get; set; }
	}

	public interface INotificationType {
		[DataSourceProperty(nameof(IModelNotificationType.Types))]
		[Required][RefreshProperties(RefreshProperties.All)]
		IModelClass Type { get; set; }
		[Required]
		[DataSourceProperty(nameof(IModelNotificationType.ObjectIndexMembers))]
		IModelMember ObjectIndexMember { get; set; }
	}

	[KeyProperty(nameof(TypeId))]
	public interface IModelNotificationType : IModelNode, INotificationType {
		[Browsable(false)]
		string TypeId{ get; set; }

		[Browsable(false)]
		IEnumerable<IModelMember> ObjectIndexMembers { get; }
		[Browsable(false)]
		IEnumerable<IModelClass> Types { get; }
	}

	public interface IModelNotification:IModelNode{
		IModelNotificationTypes Types { get; }
    }

	
	[DomainLogic(typeof(IModelNotificationType))]
	public static class ModelNotificationTypeLogic {
		public static IModelClass Get_Type(IModelNotificationType item) 
			=> item.Application.BOModel[item.TypeId];

		public static void Set_Type(IModelNotificationType item, IModelClass modelClass) => item.TypeId = modelClass.Id();

		internal static NotificationType Type(this IEnumerable<NotificationType> types, Type objectType)
			=> types.First(type => type.Type==objectType);

		internal static NotificationType[] NotificationTypes(this IModelApplication application) 
			=> application.JobSchedulerNotification().Types.Select(type => new NotificationType(type)).ToArray();

		public static IModelNotification JobSchedulerNotification(this IModelApplication application) 
			=> ((IModelJobSchedulerNotification)application.ToReactiveModule<IModelReactiveModulesJobScheduler>().JobScheduler).Notification;

		public static IModelList<IModelMember> Get_ObjectIndexMembers(this IModelNotificationType notification) 
			=> notification.Application.BOModel.Where(c => c==notification.Type&&notification.Type!=null)
				.SelectMany(c => c.AllMembers).Where(IsIndex).ToCalculatedModelNodeList();
		
		public static IModelList<IModelClass> Get_Types(this IModelNotificationType notification) 
			=> notification.Application.BOModel.SelectMany(c => c.AllMembers.Where(IsIndex)
					.Select(_ => c).Distinct()).ToCalculatedModelNodeList();

		private static bool IsIndex(this IModelMember member) 
			=> new[] { typeof(Int16), typeof(Int32), typeof(Int64) }.Contains(member.MemberInfo.MemberType);
	}
}
