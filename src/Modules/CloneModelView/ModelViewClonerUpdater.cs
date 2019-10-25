using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Xpand.Extensions.XAF.Model;

namespace Xpand.XAF.Modules.CloneModelView{
    public class ModelViewClonerUpdater : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator> {
        public override void UpdateNode(ModelNode node) {
            var modelClasses = node.Application.BOModel.Where(modelClass => modelClass.TypeInfo.FindAttribute<CloneModelViewAttribute>() != null);
            
            var master = node.Application.Master();
            var cloneHomeLayer = master.NewModelApplication(nameof(ModelViewClonerUpdater));
            foreach (var modelClass in modelClasses) {
                var cloneViewAttributes = modelClass.TypeInfo.FindAttributes<CloneModelViewAttribute>(false).OrderBy(viewAttribute => viewAttribute.ViewType);
                foreach (var cloneViewAttribute in cloneViewAttributes) {
                    if (node.Application.Views[cloneViewAttribute.ViewId]==null) {
                        var tuple = GetModelView(modelClass, cloneViewAttribute);
                        var objectView = (IModelView)tuple.objectView;
                        var cloneNodeFrom = (IModelObjectView)node.AddNode(ModelViewType(cloneViewAttribute.ViewType), cloneViewAttribute.ViewId);
                        cloneNodeFrom.ModelClass = objectView.AsObjectView.ModelClass;
                        cloneNodeFrom.Xml();
                        AssignAsDefaultView(cloneViewAttribute, cloneNodeFrom,tuple.isLookup);
                        if (tuple.objectView is IModelListView && !(string.IsNullOrEmpty(cloneViewAttribute.DetailView))) {
                            var modelDetailView =node.Application.Views.OfType<IModelDetailView>().FirstOrDefault(view 
                                => view.Id == cloneViewAttribute.DetailView);
                            if (modelDetailView == null)
                                throw new NullReferenceException(cloneViewAttribute.DetailView);
                            ((IModelListView) cloneNodeFrom).DetailView = modelDetailView;
                        }
                    }
                }
            }
            master.InsertLayer(cloneHomeLayer);
        }

        public static Type ModelViewType( CloneViewType viewType){
            if (viewType == CloneViewType.ListView||viewType == CloneViewType.LookupListView) return typeof(IModelListView);
            return typeof(IModelDetailView);
        }

        void AssignAsDefaultView(CloneModelViewAttribute cloneModelViewAttribute, IModelObjectView modelView,bool isLookup) {
            if (cloneModelViewAttribute.IsDefault) {
                if (modelView is IModelListView view) {
                    if (!isLookup){
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

        (IModelObjectView objectView,bool isLookup) GetModelView(IModelClass modelClass, CloneModelViewAttribute cloneModelViewAttribute) {
            if (cloneModelViewAttribute.ViewType == CloneViewType.LookupListView)
                return (modelClass.DefaultLookupListView,true);
            if (cloneModelViewAttribute.ViewType == CloneViewType.DetailView)
                return (modelClass.DefaultDetailView,false);
            return (modelClass.DefaultListView,false);
        }
    }
}