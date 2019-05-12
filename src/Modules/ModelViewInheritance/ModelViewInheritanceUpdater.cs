using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Xpand.Source.Extensions.Linq;
using Xpand.Source.Extensions.XAF.Model;
using Xpand.Source.Extensions.XAF.TypesInfo;

namespace Xpand.XAF.Modules.ModelViewInheritance{
    public class ModelViewInheritanceUpdater : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator> {
        public static bool Disabled;

        public override void UpdateCachedNode(ModelNode node){
            UpdateNodeCore(node);
        }

        private void UpdateNodeCore(ModelNode node){
            if (Disabled )
                return;
            var master = ((ModelApplicationBase) node.Application).Master;
            var modules = ((IModelSources)node.Application).Modules.ToArray();
            var modulesDifferences = modules
                .Select((module, i) => ModuleApplication(node, module,i==modules.Length-1))
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

        private void UpdateModel(ModelNode master,(int index, IModelMergedDifference difference, string sourceViewId) info,IModelApplication[] modulesDifferences){
            var newViewId = info.difference.GetParent<IModelView>().Id;
            var modelApplications = modulesDifferences
                .Where(application => application.Views!=null)
                .Select(application => UpdateViewModel(info,application,master))
                .Where(view => view!=null)
                .ToArray();
            for (var index = 0; index < modelApplications.Length; index++){
                var application = modelApplications[index];
                var modelApplication = master.CreatorInstance.CreateModelApplication();
                modelApplication.Id = $"{index}. {application.Id}";
                var modelObjectView = application.Application.Views[info.sourceViewId];
                CreateViewInLayer(modelApplication, modelObjectView, newViewId);
                ((ModelApplicationBase) master).InsertLayer(info.index+index, modelApplication);
            }
        }

        private static IModelView UpdateViewModel((int index, IModelMergedDifference difference, string sourceViewId) info, IModelApplication application,ModelNode master){
            var sourceModelView = application.Views[info.sourceViewId];
            var targetObjectView = info.difference.GetParent<IModelObjectView>();
            if (sourceModelView is IModelDetailView sourceDetailView &&sourceDetailView.Layout!=null){
                var sourceView = master.Application.Views[sourceDetailView.Id];
                if (sourceView != null){
                    var sourceModelClass = sourceView.AsObjectView.ModelClass;
                    var targetModelClass = master.Application.Views[targetObjectView.Id].AsObjectView.ModelClass;
                    if (sourceModelClass.OwnMembers.Count(member => member.MemberInfo.IsList) == 1 && targetModelClass.OwnMembers.Count(member => member.MemberInfo.IsList) > 0){
                        var allGroups = sourceDetailView.Layout.GetItems<IModelViewLayoutElement>(node => node is IModelLayoutGroup layoutGroup
                            ? layoutGroup: Enumerable.Empty<IModelViewLayoutElement>()).OfType<IModelLayoutGroup>();
                        foreach (var group in allGroups){
                            if (group.Id.EndsWith(ModelDetailViewLayoutNodesGenerator.LayoutGroupNameSuffix)){
                                var tabs = group.Parent.AddNode<IModelTabbedGroup>(ModelDetailViewLayoutNodesGenerator.TabsLayoutGroupName);
                                group.Id = group.Id.Replace(ModelDetailViewLayoutNodesGenerator.LayoutGroupNameSuffix, "");
                                ModelEditorHelper.AddCloneNode((ModelNode) tabs, (ModelNode) group, group.Id);
                                group.Remove();
                            }
                            else if (group.Id == sourceModelClass.TypeInfo.Name){
                                group.Id = targetModelClass.TypeInfo.Name;
                            }
                        }
                    }   
                }
            }

            return sourceModelView;
        }

        private static IModelApplication ModuleApplication(ModelNode node, ModuleBase module, bool isLastLayer){
            var creator = node.CreatorInstance;
            var application = creator.CreateModelApplication();
            application.Id = module.Name;
            if (isLastLayer && module.DiffsStore == ModelStoreBase.Empty && !XafTypesInfo.Instance.RuntimeMode()){
                var assembly = module.GetType().Assembly;
                var modelResourceName = assembly.GetManifestResourceNames().FirstOrDefault(s => s.EndsWith(".xafml"));
                using (var stream = assembly.GetManifestResourceStream(modelResourceName)){
                    using (var streamReader = new StreamReader(stream ?? throw new InvalidOperationException(module.Name))){
                        var xml = streamReader.ReadToEnd();
                        var stringModelStore = new StringModelStore(xml);
                        stringModelStore.Load(application);
                    }
                }
            }
            else{
                module.DiffsStore.Load(application);
            }
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
            switch (modelView){
                case IModelDetailView _:
                    newNode = modelViews.AddNode<IModelDetailView>();
                    break;
                case IModelListView _:
                    newNode = modelViews.AddNode<IModelListView>();
                    break;
                case IModelDashboardView _:
                    newNode = modelViews.AddNode<IModelDashboardView>();
                    break;
                default:
                    throw new NotImplementedException();
            }

            newNode.ReadFromModel( modelView);
            newNode.Id = newViewId;
        }

        public override void UpdateNode(ModelNode node){
            UpdateNodeCore(node);
        }

    }
}