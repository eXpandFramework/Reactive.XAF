using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.Persistent.Base;
using Xpand.Extensions.XAF.ModelExtensions;


namespace Xpand.XAF.Modules.ModelViewInheritance {

    [ModelAbstractClass]
    public interface IModelObjectViewMergedDifferences : IModelView {
        IModelMergedDifferences MergedDifferences { get; }
    }

    [ModelNodesGenerator(typeof(MergedDifferencesGenerator))]
    public interface IModelMergedDifferences : IModelNode, IModelList<IModelMergedDifference> {

    }

    public class MergedDifferencesGenerator : ModelNodesGeneratorBase {
        protected override void GenerateNodesCore(ModelNode node){

            var modelObjectView = node.GetParent<IModelObjectView>();
            
            var typeInfo = modelObjectView.ModelClass.TypeInfo;
            var infos = typeInfo.FindAttributes<ModelMergedDifferencesAttribute>();
            foreach (var info in infos.Where(_ => {
                var targetView = GetTargetView(node,_,typeInfo);
                return targetView == modelObjectView.Id && node[targetView] == null;
            })){
                var difference = node.AddNode<IModelMergedDifference>(info.TargetView);
                var sourceView = GetSourceView(node, info);
                difference.View = node.Application.Views[sourceView].AsObjectView;
                difference.DeepMerge = info.DeepMerge;
            }
        }

        private string GetTargetView(ModelNode node, ModelMergedDifferencesAttribute attribute, ITypeInfo typeInfo) {
            if (attribute.TargetView != null) {
                return attribute.TargetView;
            }
            var modelClass = node.Application.BOModel.GetClass(typeInfo.Type);
            return GetViewId(attribute, modelClass);
        }

        private static string GetViewId(ModelMergedDifferencesAttribute attribute, IModelClass modelClass) 
            => (attribute.ViewType == ViewType.DetailView
                ? (IModelObjectView) modelClass.DefaultDetailView
                : modelClass.DefaultListView).Id;

        private string GetSourceView(ModelNode node, ModelMergedDifferencesAttribute attribute) {
            if (attribute.SourceView != null) {
                return attribute.SourceView;
            }
            var modelClass = node.Application.BOModel.GetClass(attribute.TargetType);
            return GetViewId(attribute, modelClass);
        }
    }

    public interface IModelMergedDifference : IModelNode {
        [DataSourceProperty("Views")]
        [Required]
        [RefreshProperties(RefreshProperties.All)]
        IModelObjectView View { get; set; }
        bool DeepMerge{ get; set; }
        [Browsable(false)]
        IModelList<IModelObjectView> Views { get; }
    }

    [DomainLogic(typeof(IModelMergedDifference))]
    public class ModelMergedDIndifferenceDomainLogic {

        public static IModelList<IModelObjectView> Get_Views(IModelMergedDifference differences) {
            var modelObjectView = ((IModelObjectView)differences.Parent.Parent);
            if (modelObjectView.ModelClass == null)
                return new CalculatedModelNodeList<IModelObjectView>(differences.Application.Views.OfType<IModelObjectView>());
            var modelObjectViews = differences.Application.Views.OfType<IModelObjectView>().Where(view => view.ModelClass != null );
            return new CalculatedModelNodeList<IModelObjectView>(modelObjectViews);
        }
    }
}