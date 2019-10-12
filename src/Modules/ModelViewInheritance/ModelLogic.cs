using System;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.Persistent.Base;
using Xpand.Extensions.XAF.Model;


namespace Xpand.XAF.Modules.ModelViewInheritance {

    [ModelAbstractClass]
    public interface IModelObjectViewMergedDifferences : IModelView {
        IModelMergedDifferences MergedDifferences { get; }
    }

    [ModelNodesGenerator(typeof(MergedDifferencesGenerator))]
    public interface IModelMergedDifferences : IModelNode, IModelList<IModelMergedDifference> {

    }
    [AttributeUsage(AttributeTargets.Class,AllowMultiple = true)]
    public class ModelMergedDifferencesAttribute : Attribute {

        public ModelMergedDifferencesAttribute(string targetView, string sourceView,bool deepMerge=false) {
            TargetView = targetView;
            SourceView = sourceView;
            DeepMerge = deepMerge;
        }

        public string TargetView { get; }
        public string SourceView { get; }
        public bool DeepMerge{ get; }
    }

    public class MergedDifferencesGenerator : ModelNodesGeneratorBase {
        protected override void GenerateNodesCore(ModelNode node){

            var modelObjectView = node.GetParent<IModelObjectView>();
            
            var typeInfo = modelObjectView.ModelClass.TypeInfo;
            var infos = typeInfo.FindAttributes<ModelMergedDifferencesAttribute>();
            foreach (var info in infos.Where(_ => _.TargetView==modelObjectView.Id&&node[_.TargetView]==null)){
                var difference = node.AddNode<IModelMergedDifference>(info.TargetView);
                difference.View = node.Application.Views[info.SourceView].AsObjectView;
                difference.DeepMerge = info.DeepMerge;
            }
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
    public class ModelMergedDViifferenceDomainLogic {

        public static IModelList<IModelObjectView> Get_Views(IModelMergedDifference differences) {
            var modelObjectView = ((IModelObjectView)differences.Parent.Parent);
            if (modelObjectView.ModelClass == null)
                return new CalculatedModelNodeList<IModelObjectView>(differences.Application.Views.OfType<IModelObjectView>());
            var modelObjectViews = differences.Application.Views.OfType<IModelObjectView>().Where(view
                => view.ModelClass != null && (view.ModelClass.TypeInfo.Type.IsAssignableFrom(modelObjectView.ModelClass.TypeInfo.Type)));
            return new CalculatedModelNodeList<IModelObjectView>(modelObjectViews);
        }
    }
}