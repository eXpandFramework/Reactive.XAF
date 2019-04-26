using System;
using System.Linq;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;

namespace Xpand.XAF.Modules.CloneModelView{
    public class ModelViewClonerUpdater : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator> {
        public override void UpdateNode(ModelNode node) {
            var modelClasses = node.Application.BOModel.Where(modelClass => modelClass.TypeInfo.FindAttribute<CloneModelViewAttribute>() != null);

            foreach (var modelClass in modelClasses) {
                var cloneViewAttributes = modelClass.TypeInfo.FindAttributes<CloneModelViewAttribute>(false).OrderBy(viewAttribute => viewAttribute.ViewType);
                foreach (var cloneViewAttribute in cloneViewAttributes) {
                    if (node.Application.Views[cloneViewAttribute.ViewId]==null) {
                        var tuple = GetModelView(modelClass, cloneViewAttribute);
                        var cloneNodeFrom = ((ModelNode)tuple.objectView).Clone(cloneViewAttribute.ViewId);
                        AssignAsDefaultView(cloneViewAttribute, (IModelObjectView) cloneNodeFrom,tuple.isLookup);
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