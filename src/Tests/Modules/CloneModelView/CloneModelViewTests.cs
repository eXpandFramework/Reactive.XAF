using System;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using Fasterflect;
using Shouldly;
using Xpand.XAF.Agnostic.Tests.Artifacts;
using Xpand.XAF.Agnostic.Tests.Modules.CloneModelView.BOModel;
using Xpand.XAF.Modules.CloneModelView;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xunit;

namespace Xpand.XAF.Agnostic.Tests.Modules.CloneModelView{
    [Collection(nameof(XafTypesInfo))]
    public class CloneModelViewTests : BaseTest{
        [Theory]
        [InlineData(CloneViewType.LookupListView)]
        [InlineData(CloneViewType.ListView)]
        [InlineData(CloneViewType.DetailView)]
        public void Clone_Model_View(CloneViewType cloneViewType){
            var cloneViewId = $"test_{cloneViewType}";

            using (var application = DefaultCloneModelViewModule(info => {
                var cloneModelViewAttribute = new CloneModelViewAttribute(cloneViewType, cloneViewId);
                info.FindTypeInfo(typeof(CMV)).AddAttribute(cloneModelViewAttribute);
            }).Application){
                var modelView = application.Model.Views[cloneViewId];
                modelView.ShouldNotBeNull();
                modelView.GetType().Name.ShouldBe($"Model{cloneViewType.ToString().Replace("Lookup", "")}");
                modelView.Id.ShouldBe(cloneViewId);
            }
        } 

        [Fact]
        public void Clone_multiple_Model_Views(){
            var cloneViewId = "test_";
            var cloneViewTypes = Enum.GetValues(typeof(CloneViewType)).Cast<CloneViewType>();
            using (var application = DefaultCloneModelViewModule(info => {
                foreach (var cloneViewType in cloneViewTypes){
                    var cloneModelViewAttribute = new CloneModelViewAttribute(cloneViewType, $"{cloneViewId}{cloneViewType}");
                    info.FindTypeInfo(typeof(CMV)).AddAttribute(cloneModelViewAttribute);    
                }
                
            }).Application){
                foreach (var cloneViewType in cloneViewTypes){
                    var viewId = $"{cloneViewId}{cloneViewType}";
                    var modelView = application.Model.Views[viewId];
                    modelView.ShouldNotBeNull();
                    modelView.GetType().Name.ShouldBe($"Model{cloneViewType.ToString().Replace("Lookup", "")}");
                    modelView.Id.ShouldBe(viewId);    
                }
            }
        } 

        [Theory]
        [InlineData(CloneViewType.LookupListView)]
        [InlineData(CloneViewType.ListView)]
        [InlineData(CloneViewType.DetailView)]
        public void Clone_Model_View_and_make_it_default(CloneViewType cloneViewType){
            var cloneViewId = $"test_{cloneViewType}";

            using (var application = DefaultCloneModelViewModule(info => {
                var cloneModelViewAttribute = new CloneModelViewAttribute(cloneViewType, cloneViewId,true);
                info.FindTypeInfo(typeof(CMV)).AddAttribute(cloneModelViewAttribute);
            }).Application){
                var modelView = application.Model.Views[cloneViewId].AsObjectView;
                
                ((IModelView) modelView.ModelClass.GetPropertyValue($"Default{cloneViewType}")).Id.ShouldBe(cloneViewId);
            }
        }

        [Theory]
        [InlineData(CloneViewType.LookupListView)]
        [InlineData(CloneViewType.ListView)]
        public void Clone_Model_ListView_and_change_its_detailview(CloneViewType cloneViewType){
            var cloneViewId = "test_";
            var listViewId = $"{cloneViewId}{cloneViewType}";
            var detailViewId = $"{cloneViewType}DetailView";
            using (var application = DefaultCloneModelViewModule(info => {
                var typeInfo = info.FindTypeInfo(typeof(CMV));
                typeInfo.AddAttribute(new CloneModelViewAttribute(CloneViewType.DetailView, detailViewId));
                typeInfo.AddAttribute(new CloneModelViewAttribute(cloneViewType, listViewId){DetailView = detailViewId});
            }).Application){
                var modelListView = (IModelListView) application.Model.Views[listViewId];
                modelListView.DetailView.Id.ShouldBe(detailViewId);
            }
        }

        private CloneModelViewModule DefaultCloneModelViewModule(Action<ITypesInfo> customizeTypesInfo){
            var application = new XafApplicationMock().Object;
            application.WhenCustomizingTypesInfo().FirstAsync()
                .Do(customizeTypesInfo)
                .Subscribe();
            var cloneModelViewModule = new CloneModelViewModule();
            cloneModelViewModule.AdditionalExportedTypes.AddRange(new[]{typeof(CMV)});
            cloneModelViewModule.RequiredModuleTypes.Add(typeof(ReactiveModule));
            application.SetupDefaults(cloneModelViewModule);
            return cloneModelViewModule;
        }
    }
}