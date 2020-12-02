using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.Persistent.Base;
using DevExpress.Xpo.DB;
using JetBrains.Annotations;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.PositionInListView{
	public interface IModelReactiveModulesPositionInListView : IModelReactiveModule{
		IModelPositionInListView PositionInListView{ get; }
	}

	public static class ModelPositionInListViewLogic{
		[PublicAPI]
		public static IObservable<IModelPositionInListView> PositionInListView(this IObservable<IModelReactiveModules> source) => source
			.Select(modules => modules.PositionInListView());

		public static IModelPositionInListView PositionInListView(this IModelReactiveModules reactiveModules) => 
			((IModelReactiveModulesPositionInListView) reactiveModules).PositionInListView;
		internal static IModelPositionInListView ModelPositionInListView(this IModelApplication modelApplication) => modelApplication
			.ToReactiveModule<IModelReactiveModulesPositionInListView>().PositionInListView;
	}
	public interface IModelPositionInListView : IModelNode{
		IModelPositionInListViewModelClassItems ModelClassItems{ get; }
		IModelPositionInListViewListViewItems ListViewItems{ get; }
	}

	[ModelNodesGenerator(typeof(PositionInListViewModelClassItemsNodesGenerator))]
	public interface IModelPositionInListViewModelClassItems : IModelNode,
		IModelList<IModelPositionInListViewModelClassItem>{
	}

	public interface IModelPositionInListViewModelClassItem : IModelNode{
		[DataSourceProperty(nameof(ModelClasses))]
		[Required]
		IModelClass ModelClass{ get; set; }

		[Required]
		[DataSourceProperty(nameof(ModelMembers))]
		IModelMember ModelMember{ get; [UsedImplicitly] set; }

		PositionInListViewNewObjectsStrategy NewObjectsStrategy{ get; set; }

		[Browsable(false)]
		IEnumerable<IModelClass> ModelClasses{ get; }

		[Browsable(false)]
		IEnumerable<IModelMember> ModelMembers{ get; }
	}

	[DomainLogic(typeof(IModelPositionInListViewModelClassItem))]
	public static class ModelPositionInListViewModelClassItemLogic{
		[UsedImplicitly]
		public static CalculatedModelNodeList<IModelClass> Get_ModelClasses(this IModelPositionInListViewModelClassItem item) => item
			.Application.BOModel.Where(m => m.AllMembers.Any(member => member.Type == typeof(int))).ToCalculatedModelNodeList();

		[UsedImplicitly]
		public static CalculatedModelNodeList<IModelMember> Get_ModelMembers(this IModelPositionInListViewModelClassItem item) => item
			.ModelClass != null ? item.ModelClass.AllMembers.Where(member => member.Type == typeof(int)).ToCalculatedModelNodeList()
				: new CalculatedModelNodeList<IModelMember>();

		[UsedImplicitly]
		public static IModelMember Get_ModelMember(this IModelPositionInListViewModelClassItem item) =>
			item.ModelClass?.AllMembers.First(member => member.Type == typeof(int));
	}

	public enum PositionInListViewNewObjectsStrategy{
		Last,
		First
	}

	public class PositionInListViewModelClassItemsNodesGenerator : ModelNodesGeneratorBase{
		protected override void GenerateNodesCore(ModelNode node){
		}
	}

	[ModelNodesGenerator(typeof(PositionInListViewListViewItemsNodesGenerator))]
	public interface IModelPositionInListViewListViewItems : IModelList<IModelPositionInListViewListViewItem>, IModelNode{
	}

	public class PositionInListViewListViewItemsNodesGenerator : ModelNodesGeneratorBase{
		protected override void GenerateNodesCore(ModelNode node){
		}
	}


	[KeyProperty(nameof(ListViewId))]
	public interface IModelPositionInListViewListViewItem : IModelNode{
		[Browsable(false)]
		string ListViewId{ get; set; }

		[DataSourceProperty(nameof(ListViews))]
		[Required]
		IModelListView ListView{ get; set; }

		[DataSourceProperty(nameof(PositionMembers))]
		[Required]
		[PublicAPI]
		IModelMember PositionMember{ get; set; }

		[Browsable(false)]
		IEnumerable<IModelListView> ListViews{ get; }

		[Browsable(false)]
		IEnumerable<IModelMember> PositionMembers{ get; }

		[DefaultValue(SortingDirection.Ascending)]
		SortingDirection SortingDirection{ get; set; }
	}

	[DomainLogic(typeof(IModelPositionInListViewListViewItem))]
	public static class ModelPositionInListViewListViewItemLogic{
		[UsedImplicitly]
		public static IModelListView Get_ListView(IModelPositionInListViewListViewItem item) => (IModelListView) item.Application.Views[item.ListViewId];

		[UsedImplicitly]
		public static void Set_ListView(IModelPositionInListViewListViewItem item, IModelListView listView) => item.ListViewId = listView.Id;

		[UsedImplicitly]
		public static IModelList<IModelListView> Get_ListViews(this IModelPositionInListViewListViewItem positionInListView) =>
			positionInListView.Application.Views.OfType<IModelListView>().Where(view => view.ModelClass.AllMembers.Any(member => member.Type == typeof(int)))
				.ToCalculatedModelNodeList();

		[UsedImplicitly]
		public static IModelList<IModelMember> Get_PositionMembers(this IModelPositionInListViewListViewItem positionInListView){
			var modelListView = positionInListView.ListView;
			return modelListView != null ? modelListView.ModelClass.AllMembers.Where(member => member.Type == typeof(int))
					.ToCalculatedModelNodeList() : Enumerable.Empty<IModelMember>().ToCalculatedModelNodeList();
		}

		[UsedImplicitly]
		public static IModelMember Get_PositionMember(this IModelPositionInListViewListViewItem positionInListView) => positionInListView
			.PositionMembers.FirstOrDefault();

		
	}
}