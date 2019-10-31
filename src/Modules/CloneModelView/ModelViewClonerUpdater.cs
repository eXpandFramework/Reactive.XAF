using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using HarmonyLib;

namespace Xpand.XAF.Modules.CloneModelView{
    [HarmonyPatch(typeof(ModelViewsNodesGenerator))]
    [HarmonyPatch(nameof(GenerateNodesCore))]
    public class ModelViewsNodesGeneratorPatch{
        [HarmonyPostfix]
        public static void GenerateNodesCore(ModelNode node){
            foreach (var modelClass in node.Application.BOModel){
                var cloneViewAttributes = modelClass.TypeInfo.FindAttributes<CloneModelViewAttribute>(false).OrderBy(viewAttribute => viewAttribute.ViewType);
                foreach (var cloneViewAttribute in cloneViewAttributes){
                
                    var modelView = NewModelView(modelClass.Application.Views, cloneViewAttribute, modelClass);
                    AssignAsDefaultView(cloneViewAttribute, modelView);
                    if (modelView is IModelListView && !(string.IsNullOrEmpty(cloneViewAttribute.DetailView))){
                        var modelDetailView = modelView.Application.Views.OfType<IModelDetailView>().FirstOrDefault(view
                            => view.Id == cloneViewAttribute.DetailView);
                        if (modelDetailView == null)
                            throw new NullReferenceException(cloneViewAttribute.DetailView);
                        ((IModelListView) modelView).DetailView = modelDetailView;
                    }
                }
            }
        }

        private static IModelObjectView NewModelView(IModelViews modelViews, CloneModelViewAttribute cloneViewAttribute, IModelClass modelClass){
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
                if(!modelClass.TypeInfo.IsPersistent) {
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

        public static Type ModelViewType( CloneViewType viewType){
            if (viewType == CloneViewType.ListView||viewType == CloneViewType.LookupListView) return typeof(IModelListView);
            return typeof(IModelDetailView);
        }

        static void AssignAsDefaultView(CloneModelViewAttribute cloneModelViewAttribute, IModelObjectView modelView) {
            if (cloneModelViewAttribute.IsDefault) {
                if (modelView is IModelListView view) {
                    if (cloneModelViewAttribute.ViewType!=CloneViewType.LookupListView){
                        view.ModelClass.DefaultListView = view;
                    }
                    else{
                        view.ModelClass.DefaultLookupListView = view;
                    }
                }
                else {
                    modelView.ModelClass.DefaultDetailView = (IModelDetailView) modelView;
                }
            }
        }


    }
}