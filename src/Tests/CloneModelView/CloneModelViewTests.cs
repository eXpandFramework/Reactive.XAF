using System;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using Fasterflect;
using NUnit.Framework;
using Shouldly;
using TestsLib;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.CloneModelView.Tests.BOModel;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;


namespace Xpand.XAF.Modules.CloneModelView.Tests{
    [NonParallelizable]
    public class CloneModelViewTests : BaseTest{
        [TestCase(CloneViewType.LookupListView, nameof(Platform.Win))]
        [TestCase(CloneViewType.ListView,nameof(Platform.Win))]
        [TestCase(CloneViewType.DetailView,nameof(Platform.Win))]
        [TestCase(CloneViewType.LookupListView,nameof(Platform.Web))]
        [TestCase(CloneViewType.ListView,nameof(Platform.Web))]
        [TestCase(CloneViewType.DetailView,nameof(Platform.Web))]
        public void Clone_Model_View(CloneViewType cloneViewType, string platformName){
            var platform = GetPlatform(platformName);

            var cloneViewId = $"{nameof(Clone_Model_View)}{platform}_{cloneViewType}";

            var application = DefaultCloneModelViewModule(info => {
                var cloneModelViewAttribute = new CloneModelViewAttribute(cloneViewType, cloneViewId);
                info.FindTypeInfo(typeof(CMV)).AddAttribute(cloneModelViewAttribute);
            }, platform).Application;
            ((bool) application.GetPropertyValue("EnableModelCache")).ShouldBe(false);
                
            var modelView = application.Model.Views[cloneViewId];
            modelView.ShouldNotBeNull();
            modelView.GetType().Name.ShouldBe($"Model{cloneViewType.ToString().Replace("Lookup", "")}");
            modelView.Id.ShouldBe(cloneViewId);
            application.Dispose();
        }

        
        [TestCase(nameof(Platform.Web))]
        [TestCase(nameof(Platform.Win))]
        public void Clone_multiple_Model_Views(string platformName){
            var platform = GetPlatform(platformName);
            var cloneViewId = $"{nameof(Clone_multiple_Model_Views)}{platform}_";
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

        
        [TestCase(CloneViewType.LookupListView, nameof(Platform.Win))]
        [TestCase(CloneViewType.ListView, nameof(Platform.Win))]
        [TestCase(CloneViewType.DetailView, nameof(Platform.Win))]
        [TestCase(CloneViewType.LookupListView,nameof(Platform.Web))]
        [TestCase(CloneViewType.ListView,nameof(Platform.Web))]
        [TestCase(CloneViewType.DetailView,nameof(Platform.Web))]
        public void Clone_Model_View_and_make_it_default(CloneViewType cloneViewType, string platformName){
            var platform = GetPlatform(platformName);
            var cloneViewId = $"{nameof(Clone_Model_View_and_make_it_default)}_{cloneViewType}{platform}";

            var application = DefaultCloneModelViewModule(info => {
                var cloneModelViewAttribute = new CloneModelViewAttribute(cloneViewType, cloneViewId, true);
                info.FindTypeInfo(typeof(CMV)).AddAttribute(cloneModelViewAttribute);
            }, platform).Application;
            var modelView = application.Model.Views[cloneViewId].AsObjectView;

            ((IModelView) modelView.ModelClass.GetPropertyValue($"Default{cloneViewType}")).Id
                .ShouldBe(cloneViewId);
            application.Dispose();        }

        


        [TestCase(CloneViewType.LookupListView, nameof(Platform.Win))]
        [TestCase(CloneViewType.LookupListView,nameof(Platform.Web))]
        [TestCase(CloneViewType.ListView, nameof(Platform.Win))]
        [TestCase(CloneViewType.ListView,nameof(Platform.Web))]
        public void Clone_Model_ListView_and_change_its_detailview(CloneViewType cloneViewType, string platform){
            var cloneViewId = $"{nameof(Clone_Model_ListView_and_change_its_detailview)}{platform}_";
            var listViewId = $"{cloneViewId}{cloneViewType}";
            var detailViewId = $"{cloneViewType}DetailView";
            var application = DefaultCloneModelViewModule(info => {
                var typeInfo = info.FindTypeInfo(typeof(CMV));
                typeInfo.AddAttribute(new CloneModelViewAttribute(CloneViewType.DetailView, detailViewId));
                typeInfo.AddAttribute(new CloneModelViewAttribute(cloneViewType, listViewId)
                    {DetailView = detailViewId});
            }, (Platform) Enum.Parse(typeof(Platform),platform)).Application;
            var modelListView = (IModelListView) application.Model.Views[listViewId];
            modelListView.DetailView.Id.ShouldBe(detailViewId);
            application.Dispose();
        }

        private static CloneModelViewModule DefaultCloneModelViewModule(Action<ITypesInfo> customizeTypesInfo,Platform platform){
            var cloneModelViewModule = new CloneModelViewModule();
            var application = platform.NewApplication<CloneModelViewModule>();
            application.Modules.Add(new ReactiveModule());
            application.WhenCustomizingTypesInfo().FirstAsync(info => {
                    customizeTypesInfo(info);
                    return true;
                })
                .Subscribe();


            cloneModelViewModule.RequiredModuleTypes.Add(typeof(ReactiveModule));
            return (CloneModelViewModule) application.AddModule(cloneModelViewModule,null,true, typeof(CMV));
        }
    }
}