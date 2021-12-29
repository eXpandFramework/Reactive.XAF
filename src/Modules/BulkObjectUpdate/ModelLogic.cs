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
	[AttributeUsage(AttributeTargets.Class)]
	public class BulkObjectUpdateAttribute:Attribute {
		public BulkObjectUpdateAttribute(string detailViewId) {
			DetailViewId = detailViewId;
		}

		public BulkObjectUpdateAttribute(){
		}

		public string DetailViewId { get; }
	}
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
		protected override void GenerateNodesCore(ModelNode node) {
			var attributes = node.Application.BOModel.SelectMany(c => c.TypeInfo.FindAttributes<BulkObjectUpdateAttribute>().Select(attribute => (attribute,c)));
			var modelListViews = node.Application.Views.OfType<IModelListView>().ToArray();
			foreach (var t in attributes) {
				foreach (var modelListView in modelListViews.Where(view => view.ModelClass == t.c)) {
					var rule = node.AddNode<IModelBulkObjectUpdateRule>($"{t.c.Name}-{modelListView.Id()}");
					rule.ListView=modelListView;
					rule.DetailView = (IModelDetailView)node.Application.Views[t.attribute.DetailViewId??t.c.DefaultDetailView.Id()];	
				}
			}
		}
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