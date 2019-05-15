using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using AppDomainToolkit;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using Fasterflect;
using Shouldly;
using Tests.Artifacts;
using Tests.Modules.CloneModelView.BOModel;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.CloneModelView;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xunit;

namespace Tests.Modules.CloneModelView{
//    [Collection(nameof(XafTypesInfo))]
    public class CloneModelViewTests : BaseTest{
        [Theory]
        [InlineData(CloneViewType.LookupListView,Platform.Win)]
        [InlineData(CloneViewType.ListView,Platform.Win)]
        [InlineData(CloneViewType.DetailView,Platform.Win)]
        [InlineData(CloneViewType.LookupListView,Platform.Web)]
        [InlineData(CloneViewType.ListView,Platform.Web)]
        [InlineData(CloneViewType.DetailView,Platform.Web)]
        internal void Clone_Model_View(CloneViewType cloneViewType,Platform platform){
            RemoteFunc.Invoke(Domain, platform,cloneViewType, (p, t) => {
                var cloneViewId = $"test_{t}";

                using (var application = DefaultCloneModelViewModule(info => {
                    var cloneModelViewAttribute = new CloneModelViewAttribute(t, cloneViewId);
                    info.FindTypeInfo(typeof(CMV)).AddAttribute(cloneModelViewAttribute);
                },p).Application){
                    var modelView = application.Model.Views[cloneViewId];
                    modelView.ShouldNotBeNull();
                    modelView.GetType().Name.ShouldBe($"Model{t.ToString().Replace("Lookup", "")}");
                    modelView.Id.ShouldBe(cloneViewId);
                }
                return Unit.Default;
            });
            
        } 

        [Theory]
        [InlineData(Platform.Web)]
        [InlineData(Platform.Win)]
        internal void Clone_multiple_Model_Views(Platform platform){
            RemoteFunc.Invoke(Domain, platform, _ => {
                var cloneViewId = "test_";
                var cloneViewTypes = Enum.GetValues(typeof(CloneViewType)).Cast<CloneViewType>();
                using (var application = DefaultCloneModelViewModule(info => {
                    foreach (var cloneViewType in cloneViewTypes){
                        var cloneModelViewAttribute = new CloneModelViewAttribute(cloneViewType, $"{cloneViewId}{cloneViewType}");
                        info.FindTypeInfo(typeof(CMV)).AddAttribute(cloneModelViewAttribute);    
                    }
                
                },_).Application){
                    foreach (var cloneViewType in cloneViewTypes){
                        var viewId = $"{cloneViewId}{cloneViewType}";
                        var modelView = application.Model.Views[viewId];
                        modelView.ShouldNotBeNull();
                        modelView.GetType().Name.ShouldBe($"Model{cloneViewType.ToString().Replace("Lookup", "")}");
                        modelView.Id.ShouldBe(viewId);    
                    }
                }
                return Unit.Default;
            });
            
        } 

        [Theory]
        [InlineData(CloneViewType.LookupListView,Platform.Win)]
        [InlineData(CloneViewType.ListView,Platform.Win)]
        [InlineData(CloneViewType.DetailView,Platform.Win)]
        [InlineData(CloneViewType.LookupListView,Platform.Web)]
        [InlineData(CloneViewType.ListView,Platform.Web)]
        [InlineData(CloneViewType.DetailView,Platform.Web)]
        internal void Clone_Model_View_and_make_it_default(CloneViewType cloneViewType,Platform platform){
            RemoteFunc.Invoke(Domain, platform,cloneViewType, (p, t) => {
                var cloneViewId = $"test_{t}";

                using (var application = DefaultCloneModelViewModule(info => {
                    var cloneModelViewAttribute = new CloneModelViewAttribute(t, cloneViewId,true);
                    info.FindTypeInfo(typeof(CMV)).AddAttribute(cloneModelViewAttribute);
                },p).Application){
                    var modelView = application.Model.Views[cloneViewId].AsObjectView;
                
                    ((IModelView) modelView.ModelClass.GetPropertyValue($"Default{t}")).Id.ShouldBe(cloneViewId);
                }
                return Unit.Default;

            } );
            
        }

        [Theory]
        [InlineData(CloneViewType.LookupListView,Platform.Win)]
        [InlineData(CloneViewType.ListView,Platform.Win)]
        [InlineData(CloneViewType.LookupListView,Platform.Web)]
        [InlineData(CloneViewType.ListView,Platform.Web)]
        internal void Clone_Model_ListView_and_change_its_detailview(CloneViewType cloneViewType,Platform platform){
            RemoteFunc.Invoke(Domain, platform,cloneViewType, (p, t) => {
                var cloneViewId = "test_";
                var listViewId = $"{cloneViewId}{t}";
                var detailViewId = $"{t}DetailView";
                using (var application = DefaultCloneModelViewModule(info => {
                    var typeInfo = info.FindTypeInfo(typeof(CMV));
                    typeInfo.AddAttribute(new CloneModelViewAttribute(CloneViewType.DetailView, detailViewId));
                    typeInfo.AddAttribute(new CloneModelViewAttribute(t, listViewId){DetailView = detailViewId});
                },p).Application){
                    var modelListView = (IModelListView) application.Model.Views[listViewId];
                    modelListView.DetailView.Id.ShouldBe(detailViewId);
                }
                return Unit.Default;

            } );
            
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