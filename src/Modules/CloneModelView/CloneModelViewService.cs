using System;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.NodeGenerators;

using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.CloneModelView{
	public static class CloneModelViewService{
        internal static IObservable<IModelViews> Connect(this ApplicationModulesManager manager) 
			=> manager.WhenGeneratingModelNodes(modelApplication => modelApplication.Views)
				.Do(views => {
					foreach (var bo in views.Application.BOModel.Where(m =>
						m.TypeInfo.FindAttributes<CloneModelViewAttribute>(false).Any())){
						views.GenerateModel(bo);
					}
				});

		private static IModelObjectView NewModelView(this IModelViews modelViews, CloneModelViewAttribute cloneViewAttribute,
			IModelClass modelClass){
			if (cloneViewAttribute.ViewType == CloneViewType.ListView){
				var listView = modelViews.AddNode<IModelListView>(cloneViewAttribute.ViewId);
				listView.ModelClass = modelClass;
				ModelListViewNodesGenerator.GenerateNodes(listView, modelClass);
				return listView;
			}

			if (cloneViewAttribute.ViewType == CloneViewType.LookupListView){
				var listViewModel = modelViews.AddNode<IModelListView>(cloneViewAttribute.ViewId);
				listViewModel.ModelClass = modelClass;
				listViewModel.SetValue("IsLookupView", true);
				if (!modelClass.TypeInfo.IsPersistent){
					listViewModel.DataAccessMode = CollectionSourceDataAccessMode.Client;
				}

				listViewModel.IsGroupPanelVisible = false;
				listViewModel.AutoExpandAllGroups = false;
				listViewModel.IsFooterVisible = false;
				return listViewModel;
			}

			if (cloneViewAttribute.ViewType == CloneViewType.DetailView){
				var detailView = modelViews.AddNode<IModelDetailView>(cloneViewAttribute.ViewId);
				detailView.ModelClass = modelClass;
				return detailView;
			}

			throw new NotImplementedException();
		}

		static void AssignAsDefaultView(this CloneModelViewAttribute cloneModelViewAttribute, IModelObjectView modelView){
			if (cloneModelViewAttribute.IsDefault){
				if (modelView is IModelListView view){
					if (cloneModelViewAttribute.ViewType != CloneViewType.LookupListView){
						view.ModelClass.DefaultListView = view;
					}
					else{
						view.ModelClass.DefaultLookupListView = view;
					}
				}
				else{
					modelView.ModelClass.DefaultDetailView = (IModelDetailView) modelView;
				}
			}
		}

		static void GenerateModel(this IModelViews views, IModelClass classInfo){
			var cloneViewAttributes = classInfo.TypeInfo.FindAttributes<CloneModelViewAttribute>(false)
				.Where(attribute => views[attribute.ViewId]==null)
				.OrderBy(viewAttribute => viewAttribute.ViewType);
			foreach (var cloneViewAttribute in cloneViewAttributes){
				var modelView = views.NewModelView( cloneViewAttribute, classInfo);
				cloneViewAttribute.AssignAsDefaultView( modelView);
				if (modelView is IModelListView listView && !(string.IsNullOrEmpty(cloneViewAttribute.DetailView))){
					var modelDetailView = listView.Application.Views.OfType<IModelDetailView>()
						.FirstOrDefault(view => view.Id == cloneViewAttribute.DetailView);
					if (modelDetailView == null)
						throw new NullReferenceException(cloneViewAttribute.DetailView);
					listView.DetailView = modelDetailView;
				}
			}
		}

		
		public static Type ModelViewType(this CloneViewType viewType) 
            => viewType == CloneViewType.ListView || viewType == CloneViewType.LookupListView
                ? typeof(IModelListView)
                : typeof(IModelDetailView);
    }
}