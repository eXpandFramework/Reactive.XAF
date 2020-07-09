using System.Linq;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;

namespace Xpand.XAF.Modules.ModelViewInheritance{
    public class ModelViewInheritanceUpdater : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator> {
        public static bool Disabled;

        public override void UpdateCachedNode(ModelNode node) => UpdateNodeCore(node);

        private void UpdateNodeCore(ModelNode node){
            if (Disabled )
                return;
            var master = ((ModelApplicationBase) node.Application).Master;
            var modelApplications = ((IModelSources)node.Application).Modules.ToArray().ModuleApplications(node);
            var infos = modelApplications.Concat(new[]{node.Application}).ToArray().ModelInfos().ToArray();
            foreach (var info in infos){
                info.UpdateModel(modelApplications,master);
            }
        }

        public override void UpdateNode(ModelNode node) => UpdateNodeCore(node);
    }
}