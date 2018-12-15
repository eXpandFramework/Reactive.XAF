using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.XAF.Extensions.Model;

namespace DevExpress.XAF.Modules.ModelViewIneritance{
    public class ModelViewInheritanceUpdater : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator> {
        public static bool Disabled;

        public override void UpdateCachedNode(ModelNode node){
            UpdateNodeCore(node);
        }

        private void UpdateNodeCore(ModelNode node){
            if (Disabled )
                return;
            var master = ((ModelApplicationBase) node.Application).Master;
            var modulesDifferences = ((IModelSources)node.Application).Modules
                .Select(module => ModuleApplication(node, module))
                .ToArray();
            foreach (var info in ModelInfos(modulesDifferences).Concat(AttributeInfos(node, modulesDifferences))){
                UpdateModel(master, info,modulesDifferences);
            }
        }

        private static IEnumerable<(int index, IModelMergedDifference difference, string objectViewId)> ModelInfos(IModelApplication[] modulesDifferences){
            return modulesDifferences
                .SelectMany(ModelViews)
                .SelectMany(_ => _.objectView.MergedDifferences.Select(difference => (_.index, difference, ViewId(difference))));
        }

        private static string ViewId(IModelMergedDifference difference){
            var regexObj = new Regex("View=\"([^\"]*)\"");
            return regexObj.Match(difference.Xml()).Groups[1].Value;
        }

        private static IEnumerable<(int index, IModelMergedDifference mergedDifference, string objectViewId)> AttributeInfos(ModelNode node, IModelApplication[] modulesDifferences){
            return node.Application.Views?
                       .OfType<IModelObjectViewMergedDifferences>()
                       .SelectMany(differences => differences.MergedDifferences)
                       .SelectMany(mergedDifference => modulesDifferences
                           .Select((application, index) => {
                               if (application.Views != null){
                                   var modelView = application.Views[mergedDifference.View.Id];
                                   return modelView != null? (index: (index + 1), mergedDifference,  modelView.Id): default;
                               }
                               return default;
                           })
                           .Where(tuple => tuple != default)
                       ) ?? Enumerable.Empty<(int index, IModelMergedDifference difference, string objectViewId)>();
        }

        private void UpdateModel(ModelNode master,(int index, IModelMergedDifference difference, string objectViewId) info,IModelApplication[] modulesDifferences){
            var newViewId = info.difference.GetParent<IModelView>().Id;
            var modelApplications = modulesDifferences
                .Where(application => application.Views!=null)
                .Select(application => application.Views[info.objectViewId])
                .Where(view => view!=null)
                .ToArray();
            for (var index = 0; index < modelApplications.Length; index++){
                var application = modelApplications[index];
                var modelApplication = master.CreatorInstance.CreateModelApplication();
                modelApplication.Id = $"{index}. {application.Id}";
                var modelObjectView = application.Application.Views[info.objectViewId];
                CreateViewInLayer(modelApplication, modelObjectView, newViewId);
                ((ModelApplicationBase) master).InsertLayer(info.index+index, modelApplication);
            }
        }

        private static IModelApplication ModuleApplication(ModelNode node, ModuleBase module){
            var creator = node.CreatorInstance;
            var application = creator.CreateModelApplication();
            application.Id = module.Name;
            module.DiffsStore.Load(application);
            return application.Application;
        }

        private static IEnumerable<(int index, IModelObjectViewMergedDifferences objectView)> ModelViews(IModelApplication application, int index){
            return application.Views?.OfType<IModelObjectViewMergedDifferences>()
                .Where(differences => differences.MergedDifferences != null)
                .Select(mergedDifferences => (index: index + 1, mergedDifferences))??Enumerable.Empty<(int index, IModelObjectViewMergedDifferences objectView)>();
        }

        void CreateViewInLayer(ModelApplicationBase modelApplication, IModelView modelView, string newViewId) {
            var modelViews =modelApplication.Application.Views?? modelApplication.AddNode<IModelViews>();
            if (modelViews[modelView.Id]!=null)
                throw new NotSupportedException($"{modelView.Id} already exists");
            IModelView newNode;
            if (modelView is IModelDetailView)
                newNode = modelViews.AddNode<IModelDetailView>();
            else
            if (modelView is IModelListView)
                newNode = modelViews.AddNode<IModelListView>();
            else
            if (modelView is IModelDashboardView)
                newNode = modelViews.AddNode<IModelDashboardView>();
            else
                throw new NotImplementedException();

            new ModelXmlReader().ReadFromModel(newNode, modelView);
            newNode.Id = newViewId;
        }

        public override void UpdateNode(ModelNode node){
            UpdateNodeCore(node);
        }

    }
}