using System;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using Fasterflect;
using Shouldly;
using TestsLib;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.CloneModelView.Tests.BOModel;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xunit;

namespace Xpand.XAF.Modules.CloneModelView.Tests{
    [Collection(nameof(CloneModelViewModule))]
    public class CloneModelViewTests : BaseTest{
        [Theory]
        [InlineData(CloneViewType.LookupListView,Platform.Win)]
        [InlineData(CloneViewType.ListView,Platform.Win)]
        [InlineData(CloneViewType.DetailView,Platform.Win)]
        [InlineData(CloneViewType.LookupListView,Platform.Web)]
        [InlineData(CloneViewType.ListView,Platform.Web)]
        [InlineData(CloneViewType.DetailView,Platform.Web)]
        internal void Clone_Model_View(CloneViewType cloneViewType,Platform platform){
            var cloneViewId = $"{nameof(Clone_Model_View)}_{cloneViewType}";

            var application = DefaultCloneModelViewModule(info => {
                var cloneModelViewAttribute = new CloneModelViewAttribute(cloneViewType, cloneViewId);
                info.FindTypeInfo(typeof(CMV)).AddAttribute(cloneModelViewAttribute);
            }, platform).Application;
            var modelView = application.Model.Views[cloneViewId];
            modelView.ShouldNotBeNull();
            modelView.GetType().Name.ShouldBe($"Model{cloneViewType.ToString().Replace("Lookup", "")}");
            modelView.Id.ShouldBe(cloneViewId);
            application.Dispose();
        } 

        [Theory]
        [InlineData(Platform.Web)]
        [InlineData(Platform.Win)]
        internal void Clone_multiple_Model_Views(Platform platform){
            var cloneViewId = $"{nameof(Clone_multiple_Model_Views)}_";
            var cloneViewTypes = Enum.GetValues(typeof(CloneViewType)).Cast<CloneViewType>();
            var application = DefaultCloneModelViewModule(info => {
                foreach (var cloneViewType in cloneViewTypes){
                    var cloneModelViewAttribute =
                        new CloneModelViewAttribute(cloneViewType, $"{cloneViewId}{cloneViewType}");
                    info.FindTypeInfo(typeof(CMV)).AddAttribute(cloneModelViewAttribute);
                }

            }, platform).Application;
            foreach (var cloneViewType in cloneViewTypes){
                var viewId = $"{cloneViewId}{cloneViewType}";
                var modelView = application.Model.Views[viewId];
                modelView.ShouldNotBeNull();
                modelView.GetType().Name.ShouldBe($"Model{cloneViewType.ToString().Replace("Lookup", "")}");
                modelView.Id.ShouldBe(viewId);    
            }
            application.Dispose();
        } 

        [Theory]
        [InlineData(CloneViewType.LookupListView,Platform.Win)]
        [InlineData(CloneViewType.ListView,Platform.Win)]
        [InlineData(CloneViewType.DetailView,Platform.Win)]
        [InlineData(CloneViewType.LookupListView,Platform.Web)]
        [InlineData(CloneViewType.ListView,Platform.Web)]
        [InlineData(CloneViewType.DetailView,Platform.Web)]
        internal void Clone_Model_View_and_make_it_default(CloneViewType cloneViewType,Platform platform){
            var cloneViewId = $"{nameof(Clone_Model_View_and_make_it_default)}_{cloneViewType}";

            var application = DefaultCloneModelViewModule(info => {
                var cloneModelViewAttribute = new CloneModelViewAttribute(cloneViewType, cloneViewId, true);
                info.FindTypeInfo(typeof(CMV)).AddAttribute(cloneModelViewAttribute);
            }, platform).Application;
            var modelView = application.Model.Views[cloneViewId].AsObjectView;
                
            ((IModelView) modelView.ModelClass.GetPropertyValue($"Default{cloneViewType}")).Id.ShouldBe(cloneViewId);
            application.Dispose();
            
        }

        [Theory]
        [InlineData(CloneViewType.LookupListView,Platform.Win)]
        [InlineData(CloneViewType.ListView,Platform.Win)]
        [InlineData(CloneViewType.LookupListView,Platform.Web)]
        [InlineData(CloneViewType.ListView,Platform.Web)]
        internal void Clone_Model_ListView_and_change_its_detailview(CloneViewType cloneViewType,Platform platform){
            var cloneViewId = $"{nameof(Clone_Model_ListView_and_change_its_detailview)}_";
            var listViewId = $"{cloneViewId}{cloneViewType}";
            var detailViewId = $"{cloneViewType}DetailView";
            var application = DefaultCloneModelViewModule(info => {
                var typeInfo = info.FindTypeInfo(typeof(CMV));
                typeInfo.AddAttribute(new CloneModelViewAttribute(CloneViewType.DetailView, detailViewId));
                typeInfo.AddAttribute(new CloneModelViewAttribute(cloneViewType, listViewId)
                    {DetailView = detailViewId});
            }, platform).Application;
            var modelListView = (IModelListView) application.Model.Views[listViewId];
            modelListView.DetailView.Id.ShouldBe(detailViewId);
            application.Dispose();
        }

        private static CloneModelViewModule DefaultCloneModelViewModule(Action<ITypesInfo> customizeTypesInfo,Platform platform){
            var application = platform.NewApplication();
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