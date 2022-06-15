using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;

using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.ViewItemValue{
	public interface IModelReactiveModulesViewItemValue : IModelReactiveModule{
		IModelViewItemValue ViewItemValue{ get; }
	}

	public static class ModelViewItemValueLogic{
		
		
		public static IObservable<IModelViewItemValue> ViewItemValue(this IObservable<IModelReactiveModules> source) => source
			.Select(modules => modules.ViewItemValue());

		public static IModelViewItemValue ViewItemValue(this IModelReactiveModules reactiveModules) => 
			((IModelReactiveModulesViewItemValue) reactiveModules).ViewItemValue;
		
		internal static IModelViewItemValue ModelViewItemValue(this IModelApplication modelApplication) => modelApplication
			.ToReactiveModule<IModelReactiveModulesViewItemValue>().ViewItemValue;
	}
	public interface IModelViewItemValue:IModelNode{
		IModelViewItemValueItems Items{ get; }
	}

	public interface IModelViewItemValueItems : IModelNode, IModelList<IModelViewItemValueItem>{
	}

	[KeyProperty(nameof(ObjectViewId))]
	public interface IModelViewItemValueItem:IModelNode{
		[Browsable(false)]
		string ObjectViewId{ get; set; }
		[Required][DataSourceProperty(nameof(ObjectViews))][RefreshProperties(RefreshProperties.All)]
		IModelObjectView ObjectView{ get; set; }
		[Browsable(false)]
		IEnumerable<IModelObjectView> ObjectViews{ get; }
		IModelViewItemValueObjectViewItems  Members{ get; }
	}

	[DomainLogic(typeof(IModelViewItemValueItem))]
	public class ModelViewItemValueItemLogic{
		
		public IModelList<IModelObjectView> Get_ObjectViews(IModelViewItemValueItem itemValueItem) =>
			itemValueItem.Application.Views.OfType<IModelDetailView>().Where(view => view.DomainComponentItems().Any()).Cast<IModelObjectView>()
				.ToCalculatedModelNodeList();
		
		public static IModelObjectView Get_ObjectView(IModelViewItemValueItem itemValueItem) => itemValueItem
			.ObjectViews.FirstOrDefault(viewItem => viewItem.Id==itemValueItem.ObjectViewId);

		
		public static void Set_ObjectView(IModelViewItemValueItem itemValueItem, IModelObjectView modelObjectView) => 
			itemValueItem.ObjectViewId = modelObjectView.Id;
	}
	
	public interface IModelViewItemValueObjectViewItems:IModelNode,IModelList<IModelViewItemValueObjectViewItem>{
	}

	[KeyProperty(nameof(MemberViewItemId))]
	public interface IModelViewItemValueObjectViewItem:IModelNode{
		[Browsable(false)]
		string MemberViewItemId{ get; set; }
		[Required][DataSourceProperty(nameof(MemberViewItems))]
		IModelMemberViewItem MemberViewItem{ get; set; }
		[Browsable(false)]
		IEnumerable<IModelMemberViewItem> MemberViewItems{ get; }
	}
	
	[DomainLogic(typeof(IModelViewItemValueObjectViewItem))]
	public static class ModelViewItemValueObjectViewIteLogicm{
		
		public static IModelMemberViewItem Get_MemberViewItem(IModelViewItemValueObjectViewItem item) => item
			.MemberViewItems.FirstOrDefault(viewItem => viewItem.Id==item.MemberViewItemId);

		
		public static void Set_MemberViewItem(IModelViewItemValueObjectViewItem item, IModelMemberViewItem memberViewItem) => 
			item.MemberViewItemId = memberViewItem.Id;
		
		
		public static IModelList<IModelMemberViewItem> Get_MemberViewItems(this IModelViewItemValueObjectViewItem item) =>
			item == null ? new CalculatedModelNodeList<IModelMemberViewItem>()
				: item.GetParent<IModelViewItemValueItem>().ObjectView.DomainComponentItems().ToCalculatedModelNodeList();
	}
}
