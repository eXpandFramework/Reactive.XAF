using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.SystemModule;
using Shouldly;
using Tests.Modules.ModelViewInheritance.BOModel;
using Xpand.Source.Extensions.XAF.Model;
using Xpand.XAF.Modules.ModelViewInheritance;

namespace Tests.Modules.ModelViewInheritance{
    class InheritAndModifyBaseView{
        private readonly XafApplication _application;
        private readonly ViewType _viewType;
        private readonly bool _attribute;

        public InheritAndModifyBaseView(XafApplication application,ViewType viewType,bool attribute){
            _application = application;
            _viewType = viewType;
            _attribute = attribute;
        }

        public IEnumerable<string> GetModels(){
            var modelApplicationBase = ((ModelApplicationBase) _application.Model);
            var objectViewBase = modelApplicationBase.Application.Views[$"{nameof(ABaseMvi)}_{_viewType}"].AsObjectView;
            yield return Layer1ObjectViewA();
            yield return Layer2ObjectViewA();
            yield return RegisterRules();

            string Layer2ObjectViewA(){
                var modelApplication = modelApplicationBase.CreatorInstance.CreateModelApplication();
                ModelApplicationHelper.AddLayer(modelApplicationBase, modelApplication);
                ModifyAttributes(objectViewBase);
                if (_viewType == ViewType.ListView){
                    ModifyColumns((IModelListView) objectViewBase);
                    var modelListViewFilter = (IModelViewHiddenActions) objectViewBase;
                    var hiddenAction = modelListViewFilter.HiddenActions.AddNode<IModelActionLink>();
                    hiddenAction.Action = _application.Model.ActionDesign.Actions["Save"];
                }
                else{
                    ModifyLayout((IModelDetailView) objectViewBase);
                }
                ModelApplicationHelper.RemoveLayer((ModelApplicationBase)_application.Model);
                return modelApplication.Xml;
            }

            string Layer1ObjectViewA(){
                var modelApplication = modelApplicationBase.CreatorInstance.CreateModelApplication();
                ModelApplicationHelper.AddLayer(modelApplicationBase, modelApplication);
                objectViewBase.AllowDelete = false;
                var applicationXml = modelApplication.Xml;
                ModelApplicationHelper.RemoveLayer((ModelApplicationBase)_application.Model);
                return applicationXml;
            }

            void ModifyAttributes(IModelObjectView modelObjectView) {
                modelObjectView.Caption = "Changed";
            }

            void ModifyLayout(IModelDetailView modelDetailView) {
                var node = modelDetailView.Layout.GetNodeByPath($"Main/SimpleEditors/{nameof(ABaseMvi)}/{nameof(ABaseMvi.Name)}");
                ((IModelNode) node).Remove();
                var oidItem = modelDetailView.Layout.GetNode("Main").AddNode<IModelLayoutViewItem>("Oid");
                oidItem.ViewItem = oidItem.ViewItems.First(item => item.Id == "Oid");
                oidItem.Index = 1;
                modelDetailView.Layout.GetNodeByPath("Main/Tags_Group").Index = 2;
            }

            void ModifyColumns(IModelListView modelListView) {
                modelListView.Columns[nameof(ABaseMvi.Description)].Caption = "New";
                modelListView.Columns[nameof(ABaseMvi.Name)].Remove();
                modelListView.Columns["Oid"].Index = 100;
            }

            string RegisterRules(){
                var modelApplication = modelApplicationBase.CreatorInstance.CreateModelApplication();
                ModelApplicationHelper.AddLayer(modelApplicationBase, modelApplication);
                var objectViewA = modelApplication.Application.Views[$"{nameof(AMvi)}_{_viewType}"].AsObjectView;
                var differences = ((IModelObjectViewMergedDifferences) objectViewA).MergedDifferences;
                var difference = _attribute ? differences.First() : differences.AddNode<IModelMergedDifference>();
                difference.View = objectViewBase;
                var xml = modelApplication.Xml;
                ModelApplicationHelper.RemoveLayer((ModelApplicationBase) _application.Model);
                return xml;
            }

        }

        public void Verify(IModelApplication modelApplication){
            var modelClassB = modelApplication.BOModel.GetClass(typeof(AMvi));
            var viewB =_viewType==ViewType.ListView? modelClassB.DefaultListView.AsObjectView:modelClassB.DefaultDetailView;    

            viewB.Caption.ShouldBe("Changed");
            viewB.AllowDelete.ShouldBe(false);
            if (viewB is IModelListView modelListView) {
                modelListView.Columns[nameof(ABaseMvi.Description)].Caption.ShouldBe("New");
                modelListView.Columns[nameof(ABaseMvi.Name)].ShouldBeNull();
                modelListView.Columns[nameof(ABaseMvi.Oid)].Index.ShouldBe(100);
                ((IModelViewHiddenActions) modelListView).HiddenActions.Any().ShouldBeTrue();
            }
            else {
                var modelDetailView = ((IModelDetailView) viewB);
                modelDetailView.Layout.GetNodeByPath($"Main/SimpleEditors/{nameof(AMvi)}/{nameof(ABaseMvi.Name)}").ShouldBeNull();
                modelDetailView.Layout.GetNodeByPath($"Main/SimpleEditors/{nameof(ABaseMvi)}/{nameof(ABaseMvi.Description)}").ShouldNotBeNull();
                modelDetailView.Layout.GetNodeByPath("Main/Oid").ShouldNotBeNull();
                modelDetailView.Layout.GetNodeByPath("Main/Tags_Groups").ShouldBeNull();
                modelDetailView.Layout.GetNodeByPath("Main/Tabs/FileMvis/FileMvis").ShouldNotBeNull();
                modelDetailView.Layout.GetNodeByPath("Main/Tabs/Tags/Tags");
                modelDetailView.Layout.GetNode("Main").NodeCount.ShouldBe(3);
            }

        }
    }
}