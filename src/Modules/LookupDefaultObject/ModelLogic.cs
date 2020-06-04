using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using JetBrains.Annotations;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.LookupDefaultObject{
	public interface IModelReactiveModulesLookupDefaultObject : IModelReactiveModule{
		IModelLookupDefaultObject LookupDefaultObject{ get; }
	}

	public static class ModelLookupDefaultObjectLogic{
		
		[PublicAPI]
		public static IObservable<IModelLookupDefaultObject> LookupDefaultObject(this IObservable<IModelReactiveModules> source) => source
			.Select(modules => modules.LookupDefaultObject());

		public static IModelLookupDefaultObject LookupDefaultObject(this IModelReactiveModules reactiveModules) => 
			((IModelReactiveModulesLookupDefaultObject) reactiveModules).LookupDefaultObject;
		
		internal static IModelLookupDefaultObject ModelLookupDefaultObject(this IModelApplication modelApplication) => modelApplication
			.ToReactiveModule<IModelReactiveModulesLookupDefaultObject>().LookupDefaultObject;
	}
	public interface IModelLookupDefaultObject:IModelNode{
		IModelLookupDefaultObjectItems Items{ get; }
	}

	public interface IModelLookupDefaultObjectItems : IModelNode, IModelList<IModelLookupDefaultObjectItem>{
	}

	[KeyProperty(nameof(ObjectViewId))]
	public interface IModelLookupDefaultObjectItem:IModelNode{
		[Browsable(false)]
		string ObjectViewId{ get; set; }
		[Required][DataSourceProperty(nameof(ObjectViews))][RefreshProperties(RefreshProperties.All)]
		IModelObjectView ObjectView{ get; set; }
		[Browsable(false)]
		IEnumerable<IModelObjectView> ObjectViews{ get; }
		IModelLookupDefaultObjectObjectViewItems  Members{ get; }
	}

	[DomainLogic(typeof(IModelLookupDefaultObjectItem))]
	public class ModelLookupDefaultObjectItemLogic{
		[UsedImplicitly]
		public IModelList<IModelObjectView> Get_ObjectViews(IModelLookupDefaultObjectItem item) =>
			item.Application.Views.OfType<IModelDetailView>().Where(view => view.DomainComponentItems().Any()).Cast<IModelObjectView>()
				.ToCalculatedModelNodeList();
		[UsedImplicitly]
		public static IModelObjectView Get_ObjectView(IModelLookupDefaultObjectItem item) => item
			.ObjectViews.FirstOrDefault(viewItem => viewItem.Id==item.ObjectViewId);

		[UsedImplicitly]
		public static void Set_ObjectView(IModelLookupDefaultObjectItem item, IModelObjectView modelObjectView) => 
			item.ObjectViewId = modelObjectView.Id;
	}
	
	public interface IModelLookupDefaultObjectObjectViewItems:IModelNode,IModelList<IModelLookupDefaultObjectObjectViewItem>{
	}

	[KeyProperty(nameof(MemberViewItemId))]
	public interface IModelLookupDefaultObjectObjectViewItem:IModelNode{
		[Browsable(false)]
		string MemberViewItemId{ get; set; }
		[Required][DataSourceProperty(nameof(MemberViewItems))]
		IModelMemberViewItem MemberViewItem{ get; set; }
		[Browsable(false)]
		IEnumerable<IModelMemberViewItem> MemberViewItems{ get; }
	}
	
	[DomainLogic(typeof(IModelLookupDefaultObjectObjectViewItem))]
	public static class ModelLookupDefaultObjectObjectViewIteLogicm{
		[UsedImplicitly]
		public static IModelMemberViewItem Get_MemberViewItem(IModelLookupDefaultObjectObjectViewItem item) => item
			.MemberViewItems.FirstOrDefault(viewItem => viewItem.Id==item.MemberViewItemId);

		[UsedImplicitly]
		public static void Set_MemberViewItem(IModelLookupDefaultObjectObjectViewItem item, IModelMemberViewItem memberViewItem) => 
			item.MemberViewItemId = memberViewItem.Id;
		
		[UsedImplicitly]
		public static IModelList<IModelMemberViewItem> Get_MemberViewItems(this IModelLookupDefaultObjectObjectViewItem item) =>
			item == null ? new CalculatedModelNodeList<IModelMemberViewItem>()
				: item.GetParent<IModelLookupDefaultObjectItem>().ObjectView.DomainComponentItems().ToCalculatedModelNodeList();
	}
}
