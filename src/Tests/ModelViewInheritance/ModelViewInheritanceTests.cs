using System;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.CloneModelView;
using Xpand.XAF.Modules.ModelViewInheritance.Tests.BOModel;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;


namespace Xpand.XAF.Modules.ModelViewInheritance.Tests{
    [NonParallelizable]
    public class ModelViewInheritanceTests:BaseTest {
        [XpandTest]
        [TestCase(ViewType.DetailView,false,nameof(Platform.Win))]
        [TestCase(ViewType.DetailView,true,nameof(Platform.Win))]
        [TestCase(ViewType.ListView,false,nameof(Platform.Win))]
        [TestCase(ViewType.ListView,true,nameof(Platform.Win))]
        public void Inherit_And_Modify_A_BaseView(ViewType viewType, bool attribute,string platformName){

            var platform = GetPlatform(platformName);
            var application = platform.NewApplication<ModelViewInheritanceModule>();
            var module = CreateModelViewInheritanceModule(viewType, attribute, application,false);
            var testModule1 = new TestModule1{DiffsStore = new ResourcesModelStore(GetType().Assembly,"Model0")};
            var baseBoTypes = new[]{typeof(ABaseMvi), typeof(TagMvi)};
            var boTypes = new[]{typeof(AMvi), typeof(FileMvi)};
            testModule1.AdditionalExportedTypes.AddRange(baseBoTypes);
            var testModule2 = new TestModule2{DiffsStore = new ResourcesModelStore(GetType().Assembly,"Model1")};
            testModule2.AdditionalExportedTypes.AddRange(boTypes);

            application.SetupDefaults(module, testModule1, testModule2,
                new TestModule3{DiffsStore = new ResourcesModelStore(GetType().Assembly,"Model2")});

            var modelClassB = application.Model.BOModel.GetClass(typeof(AMvi));
            var viewB =viewType==ViewType.ListView? modelClassB.DefaultListView.AsObjectView:modelClassB.DefaultDetailView;
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
            application.Dispose();
        }



        private ModelViewInheritanceModule CreateModelViewInheritanceModule(ViewType viewType, bool attribute, XafApplication application,bool setup=true){
            CustomizeTypesInfo(viewType, attribute,application);
            var module = DefaultModelViewInheritanceModule(application,setup,typeof(ReactiveModule));
            return module;
        }

        private void CustomizeTypesInfo(ViewType viewType, bool attribute, XafApplication application){
            if (attribute){
                application.Modules.Add(new ReactiveModule());
                application.WhenApplicationModulesManager().WhenCustomizeTypesInfo()
                    .FirstAsync(_=> {
                        _.e?.TypesInfo.FindTypeInfo(typeof(AMvi))
                            .AddAttribute(new ModelMergedDifferencesAttribute($"{nameof(AMvi)}_{viewType}",
                                $"{nameof(ABaseMvi)}_{viewType}"));
                        return true;
                    })
                    .Subscribe();
            }
        }

        private ModelViewInheritanceModule DefaultModelViewInheritanceModule(XafApplication application,bool setup=true,params Type[] modules){
            var baseBoTypes = new[]{typeof(ABaseMvi), typeof(TagMvi),typeof(Element)};
            var boTypes = new[]{typeof(AMvi), typeof(FileMvi)};
            var modelViewInheritanceModule = new ModelViewInheritanceModule();
            modelViewInheritanceModule.RequiredModuleTypes.AddRange(modules);
            application.AddModule(modelViewInheritanceModule,null,setup,baseBoTypes.Concat(boTypes).ToArray());
            return modelViewInheritanceModule;
        }
         
        [XpandTest]
        [Test]
        public void Chained_Cloned_ListView_merging(){
            using var newApplication = Platform.Win.NewApplication<ModelViewInheritanceModule>();
            var module = DefaultModelViewInheritanceModule(newApplication,false, typeof(CloneModelViewModule));
            var testModule1 = new TestModule1{DiffsStore = new ResourcesModelStore(GetType().Assembly,"ChainedListView")};
            testModule1.AdditionalExportedTypes.Add(typeof(Element));
            newApplication.SetupDefaults(module, testModule1,new CloneModelViewModule());

            var listViewBase = ((IModelListView) newApplication.Model.Views[Element.ListViewBase]);
            listViewBase.Columns[nameof(Element.Street)].Index.ShouldBe(-1);
            ((IModelListView) newApplication.Model.Views[Element.ListViewBaseNested]).Columns[nameof(Element.Street)].Index.ShouldBe(-1);
        }
    }

}