using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Xpand.Extensions.Linq;
using Xpand.Extensions.XAF.Model;
using Xpand.Extensions.XAF.TypesInfo;

namespace Xpand.XAF.Modules.ModelViewInheritance{
    internal static class ModelViewInheritanceExtensions{
        internal static IEnumerable<(int index, (int? index, IModelView parentView,bool deepMerge) diffData, string objectViewId)>
            ModelInfos(this IModelApplication[] modelApplications){
            return modelApplications.ModelViews()
                
                .SelectMany(_ => _.objectView.MergedDifferences.Select(difference => {
                    var diffData = (difference.Index,difference.GetParent<IModelView>(),difference.DeepMerge);
                    var tuple = (_.index, diffData, ViewId(difference));
                    return tuple;
                }))
                .ToDeepMergeInfos(modelApplications);
        }
        private static IEnumerable<(int index, IModelObjectViewMergedDifferences objectView)> ModelViews(this IEnumerable<IModelApplication> source){
            return source.SelectMany((application, index) => {
                return application.Views?.OfType<IModelObjectViewMergedDifferences>()
                           .Where(differences => differences.MergedDifferences != null)
                           .Select(mergedDifferences => (index: index + 1, mergedDifferences)) ??
                       Enumerable.Empty<(int index, IModelObjectViewMergedDifferences objectView)>();
            });

        }

        internal static IEnumerable<(int index, (int? index, IModelView parentView, bool deepMerge) diffData, string objectViewId)>
            ToDeepMergeInfos(this IEnumerable<(int index, (int? index, IModelView parentView, bool deepMerge) diffData, string objectViewId)> source, 
                IModelApplication[] modelApplications){
            return source.SelectMany(_ => {
                if (_.diffData.deepMerge){
                    return modelApplications.Take(_.index)
                        .Select(application => application.Views?[_.objectViewId])
                        .Where(view => view!=null).OfType<IModelObjectViewMergedDifferences>().Where(differences => differences.MergedDifferences!=null)
                        .SelectMany(differences => differences.MergedDifferences)
                        .Select((difference, i) => (_.index,(_.diffData.index+i-1000,_.diffData.parentView,false),difference.ViewId()))
                        .Concat(new []{_});
                    
                }

                return new[]{_};
            });
        }

        private static string ViewId(this IModelMergedDifference difference){
            var regexObj = new Regex("View=\"([^\"]*)\"");
            return regexObj.Match(difference.Xml()).Groups[1].Value;
        }

        static void CreateViewInLayer(this ModelApplicationBase modelApplication, IModelView modelView, string newViewId) {
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

        internal static void UpdateModel(this (int index, (int? index, IModelView parentView, bool deepMerge) diffData, string objectViewId) info,
            IModelApplication[] modulesDifferences, ModelNode master){
            var newViewId = info.diffData.parentView.Id;
            var modelApplications = modulesDifferences
                .Where(application => application.Views!=null)
                .Select(application => UpdateDetailViewModel(info,application,master))
                .Where(view => view!=null)
                .ToArray();
            for (var index = 0; index < modelApplications.Length; index++){
                var application = modelApplications[index];
                var modelApplication = master.CreatorInstance.CreateModelApplication();
                modelApplication.Id = $"{index}. {application.Id}";
                var modelObjectView = application.Application.Views[info.objectViewId];
                modelApplication.CreateViewInLayer( modelObjectView, newViewId);
                ((ModelApplicationBase) master).InsertLayer(info.index+index, modelApplication);
            }
        }

        private static IModelView UpdateDetailViewModel((int index, (int? index, IModelView parentView, bool deepMerge) diffData, string objectViewId) info, IModelApplication application,ModelNode master){
            var sourceModelView = application.Views[info.objectViewId];
            var targetObjectView = info.diffData.parentView;
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

        internal static IModelApplication[] ModuleApplications(this ModuleBase[] source, ModelNode node){
            return source
                .Select((module, i) => ModuleApplication(node, module,i==source.Length-1))
                .ToArray();
            
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
    }
}