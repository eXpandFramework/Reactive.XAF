using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.Persistent.Base;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;
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

	[ModelNodesGenerator(typeof(ModelViewItemValueItemsUpdater))]
	public interface IModelViewItemValueItems : IModelNode, IModelList<IModelViewItemValueItem>{
	}

	public class ModelViewItemValueItemsUpdater:ModelNodesGeneratorBase {
		protected override void GenerateNodesCore(ModelNode node) 
			=> node.Application.BOModel.Select(c => c.TypeInfo).AttributedMembers<ViewItemValueAttribute>()
				.SelectMany(t => node.Application.Views.OfType<IModelDetailView>()
					.SelectMany(view => view.PropertyEditorItems().Where(editor => editor.ModelMember.MemberInfo==t.memberInfo)
						.Select(editor => (editor,t.attribute))))
				.GroupBy(t => t.editor.GetParent<IModelDetailView>())
				.ForEach(views => {
					var item = node.Cast<IModelViewItemValueItems>().AddNode<IModelViewItemValueItem>();
					item.ObjectView = views.Key.AsObjectView;
					views.Where(t => item.Members[t.editor.ModelMember.Id()] == null).ForEach(t => {
						var viewItem = item.Members.AddNode<IModelViewItemValueObjectViewItem>();
						viewItem.MemberViewItem = viewItem.MemberViewItems
							.First(memberViewItem => memberViewItem.ModelMember==t.editor.ModelMember,() => t.editor.ModelMember.ToString());
						viewItem.SaveViewItemValueStrategy = t.attribute.SaveViewItemValueStrategy;
					});
				});
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
		
		public IModelList<IModelObjectView> Get_ObjectViews(IModelViewItemValueItem itemValueItem) 
			=> itemValueItem.Application.Views.OfType<IModelDetailView>().Where(view => view.DomainComponentItems().Any()).Cast<IModelObjectView>()
				.ToCalculatedModelNodeList();
		
		public static IModelObjectView Get_ObjectView(IModelViewItemValueItem itemValueItem) 
			=> itemValueItem.ObjectViews.FirstOrDefault(viewItem => viewItem.Id==itemValueItem.ObjectViewId);

		
		public static void Set_ObjectView(IModelViewItemValueItem itemValueItem, IModelObjectView modelObjectView) 
			=> itemValueItem.ObjectViewId = modelObjectView.Id;
	}
	
	
	public interface IModelViewItemValueObjectViewItems:IModelNode,IModelList<IModelViewItemValueObjectViewItem>{
	}

	[KeyProperty(nameof(MemberViewItemId))]
	public interface IModelViewItemValueObjectViewItem:IModelNode{
		[Browsable(false)]
		string MemberViewItemId{ get; set; }
		SaveViewItemValueStrategy SaveViewItemValueStrategy { get; set; }
		[Required][DataSourceProperty(nameof(MemberViewItems))]
		IModelMemberViewItem MemberViewItem{ get; set; }
		[Browsable(false)]
		IEnumerable<IModelMemberViewItem> MemberViewItems{ get; }
	}

	public enum SaveViewItemValueStrategy { Default,OnCommit,OnChanged}

	[DomainLogic(typeof(IModelViewItemValueObjectViewItem))]
	public static class ModelViewItemValueObjectViewIteLogic{
		
		public static IModelMemberViewItem Get_MemberViewItem(IModelViewItemValueObjectViewItem item) 
			=> item.MemberViewItems.FirstOrDefault(viewItem => viewItem.Id==item.MemberViewItemId);

		public static void Set_MemberViewItem(IModelViewItemValueObjectViewItem item, IModelMemberViewItem memberViewItem) 
			=> item.MemberViewItemId = memberViewItem.Id;

		public static IModelList<IModelMemberViewItem> Get_MemberViewItems(this IModelViewItemValueObjectViewItem item) 
			=> item == null ? new CalculatedModelNodeList<IModelMemberViewItem>()
				: item.Application.Views[item.GetParent<IModelViewItemValueItem>().ObjectViewId].MemberViewItems().ToCalculatedModelNodeList();
	}
}
