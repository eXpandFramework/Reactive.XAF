using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.XAF.Agnostic.Specifications.Modules.ModelViewInheritance.BOModel;
using DevExpress.XAF.Extensions.Model;
using DevExpress.XAF.Modules.ModelViewIneritance;
using Shouldly;

namespace DevExpress.XAF.Agnostic.Specifications.Modules.ModelViewInheritance{
    class InheritAndModifyBaseView:IEnumerable<string>{
        private readonly XafApplication _application;
        private readonly ViewType _viewType;
        private readonly bool _attribute;

        public InheritAndModifyBaseView(XafApplication application,ViewType viewType,bool attribute){
            _application = application;
            _viewType = viewType;
            _attribute = attribute;
        }

        public IEnumerator<string> GetEnumerator(){
            var modelApplicationBase = ((ModelApplicationBase) _application.Model);
            var objectViewA = modelApplicationBase.Application.Views[$"{nameof(ModelViewInheritanceClassA)}_{_viewType}"].AsObjectView;
            yield return Layer1ObjectViewA();
            yield return Layer2ObjectViewA();
            yield return RegisterRules();

            string Layer2ObjectViewA(){
                var modelApplication = modelApplicationBase.CreatorInstance.CreateModelApplication();
                ModelApplicationHelper.AddLayer(modelApplicationBase, modelApplication);
                ModifyAttributes(objectViewA);
                if (_viewType == ViewType.ListView){
                    ModifyColumns((IModelListView) objectViewA);
                    var modelListViewFilter = (IModelViewHiddenActions) objectViewA;
                    var hiddenAction = modelListViewFilter.HiddenActions.AddNode<IModelActionLink>();
                    hiddenAction.Action = _application.Model.ActionDesign.Actions["Save"];
                }
                else{
                    ModifyLayout((IModelDetailView) objectViewA);
                }
                ModelApplicationHelper.RemoveLayer((ModelApplicationBase)_application.Model);
                return modelApplication.Xml;
            }

            string Layer1ObjectViewA(){
                var modelApplication = modelApplicationBase.CreatorInstance.CreateModelApplication();
                ModelApplicationHelper.AddLayer(modelApplicationBase, modelApplication);
                objectViewA.AllowDelete = false;
                var applicationXml = modelApplication.Xml;
                ModelApplicationHelper.RemoveLayer((ModelApplicationBase)_application.Model);
                return applicationXml;
            }

            void ModifyAttributes(IModelObjectView modelObjectView) {
                modelObjectView.Caption = "Changed";
            }

            void ModifyLayout(IModelDetailView modelDetailView) {
                var node = modelDetailView.Layout.GetNodeByPath($"Main/SimpleEditors/{nameof(ModelViewInheritanceClassA)}/{nameof(ModelViewInheritanceClassA.Test2)}");
                ((IModelNode) node).Remove();
            }

            void ModifyColumns(IModelListView modelListView) {
                modelListView.Columns[nameof(ModelViewInheritanceClassA.Test1)].Caption = "New";
                modelListView.Columns[nameof(ModelViewInheritanceClassA.Test2)].Remove();
            }
            string RegisterRules(){
                var modelApplication = modelApplicationBase.CreatorInstance.CreateModelApplication();
                ModelApplicationHelper.AddLayer(modelApplicationBase, modelApplication);
                var objectViewB = modelApplication.Application.Views[$"{nameof(ModelViewInheritanceClassB)}_{_viewType}"].AsObjectView;
                var differences = ((IModelObjectViewMergedDifferences) objectViewB).MergedDifferences;
                var difference = _attribute ? differences.First() : differences.AddNode<IModelMergedDifference>();
                difference.View = objectViewA;
                var xml = modelApplication.Xml;
                ModelApplicationHelper.RemoveLayer((ModelApplicationBase) _application.Model);
                return xml;
            }

        }

        IEnumerator IEnumerable.GetEnumerator(){
            return GetEnumerator();
        }


        public void Verify(IModelApplication modelApplication){
            var modelClassB = modelApplication.BOModel.GetClass(typeof(ModelViewInheritanceClassB));
            var viewB =_viewType==ViewType.ListView? modelClassB.DefaultListView.AsObjectView:modelClassB.DefaultDetailView;    
//            var viewB =modelApplication.Views[$"{nameof(ModelViewInheritanceClassB)}_{_viewType}"];

            viewB.Caption.ShouldBe("Changed");
            viewB.AllowDelete.ShouldBe(false);
            if (viewB is IModelListView modelListView) {
                modelListView.Columns[nameof(ModelViewInheritanceClassA.Test1)].Caption.ShouldBe("New");
                modelListView.Columns[nameof(ModelViewInheritanceClassA.Test2)].ShouldBeNull();
                ((IModelViewHiddenActions) modelListView).HiddenActions.Any().ShouldBeTrue();
            }
            else {
                
                var node = ((IModelDetailView) viewB).Layout.GetNodeByPath($"Main/SimpleEditors/{nameof(ModelViewInheritanceClassB)}");
                node.GetNode(nameof(ModelViewInheritanceClassA.Test2)).ShouldBeNull();
                node.GetNode(nameof(ModelViewInheritanceClassA.Test1)).ShouldNotBeNull();
            }

        }
    }
}