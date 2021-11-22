using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.Persistent.Base;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.BulkObjectUpdate{
	public interface IModelReactiveModulesBulkObjectUpdate : IModelReactiveModule{
		IModelBulkObjectUpdate BulkObjectUpdate{ get; }
	}

	public interface IModelBulkObjectUpdate : IModelNode{
		IModelBulkObjectUpdateRules Rules { get; }
	}
	
	[DomainLogic(typeof(IModelBulkObjectUpdate))]
	public static class ModelBulkObjectUpdateLogic {
		public static IObservable<IModelBulkObjectUpdate> BulkObjectUpdate(this IObservable<IModelReactiveModules> source) 
			=> source.Select(modules => modules.BulkObjectUpdate());

		public static IModelBulkObjectUpdate BulkObjectUpdate(this IModelReactiveModules reactiveModules) 
			=> ((IModelReactiveModulesBulkObjectUpdate) reactiveModules).BulkObjectUpdate;
		
		internal static IModelBulkObjectUpdate ModelObjectStateManager(this IModelApplication modelApplication) 
			=> modelApplication.ToReactiveModule<IModelReactiveModulesBulkObjectUpdate>().BulkObjectUpdate;
	}
	
	[ModelNodesGenerator(typeof(ModelBulkObjectUpdateRulesNodesGenerator))]
	public interface IModelBulkObjectUpdateRules:IModelNode,IModelList<IModelBulkObjectUpdateRule> {
	}
	
	public class ModelBulkObjectUpdateRulesNodesGenerator:ModelNodesGeneratorBase {
		protected override void GenerateNodesCore(ModelNode node) { }
	}


	[ModelDisplayName("Rule")]
	[KeyProperty(nameof(Caption))]
	public interface IModelBulkObjectUpdateRule:IModelNode {
		[Required][Localizable(true)]
		string Caption { get; set; }
		[Required]
		[DataSourceProperty(nameof(BulkListViews))]
		IModelListView ListView { get; set; }
		[Required]
		[DataSourceProperty(nameof(BulkDetailViews))]
		IModelDetailView DetailView { get; set; }
		[Browsable(false)]
		IModelList<IModelListView> BulkListViews { get; }
		[Browsable(false)]
		IModelList<IModelDetailView> BulkDetailViews { get; }
	}

	[DomainLogic(typeof(IModelBulkObjectUpdateRule))]
	public class ModelBulkObjectUpdateRuleLogic {

		public static IModelList<IModelListView> Get_BulkListViews(IModelBulkObjectUpdateRule rule)
			=> rule.Application.Views.OfType<IModelListView>().ToCalculatedModelNodeList();
		
		public static IModelList<IModelDetailView> Get_BulkDetailViews(IModelBulkObjectUpdateRule rule)
			=> rule.Application.Views.OfType<IModelDetailView>().ToCalculatedModelNodeList();
	
		
		public static IModelDetailView Get_DetailView(IModelBulkObjectUpdateRule rule) 
			=> rule.ListView?.ModelClass.DefaultDetailView;
	}
}