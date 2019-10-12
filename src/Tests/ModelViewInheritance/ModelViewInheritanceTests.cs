using System;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using NUnit.Framework;
using Shouldly;
using TestsLib;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.CloneModelView;
using Xpand.XAF.Modules.ModelViewInheritance.Tests.BOModel;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;


namespace Xpand.XAF.Modules.ModelViewInheritance.Tests{
    [NonParallelizable]
    public class ModelViewInheritanceTests:BaseTest {
        
        [TestCase(ViewType.DetailView,false,nameof(Platform.Win))]
        [TestCase(ViewType.DetailView,true,nameof(Platform.Win))]
        [TestCase(ViewType.DetailView,false,nameof(Platform.Web))]
        [TestCase(ViewType.DetailView,true,nameof(Platform.Web))]
        
        [TestCase(ViewType.ListView,false,nameof(Platform.Win))]
        [TestCase(ViewType.ListView,true,nameof(Platform.Win))]
        [TestCase(ViewType.ListView,false,nameof(Platform.Web))]
        [TestCase(ViewType.ListView,true,nameof(Platform.Web))]
        public void Inherit_And_Modify_A_BaseView(ViewType viewType, bool attribute,string platformName){
            var platform = GetPlatform(platformName);
            ModelViewInheritanceUpdater.Disabled = true;
            var models = GetModels(viewType, attribute, platform);

            var application = platform.NewApplication<ModelViewInheritanceModule>();
            var module = CreateModelViewIneritanceModule(viewType, attribute, application,false);
            var testModule1 = new TestModule1{DiffsStore = new StringModelStore(models[0])};
            var baseBoTypes = new[]{typeof(ABaseMvi), typeof(TagMvi)};
            var boTypes = new[]{typeof(AMvi), typeof(FileMvi)};
            testModule1.AdditionalExportedTypes.AddRange(baseBoTypes);
            var testModule2 = new TestModule2{DiffsStore = new StringModelStore(models[1])};
            testModule2.AdditionalExportedTypes.AddRange(boTypes);

            application.SetupDefaults(module, testModule1, testModule2,
                new TestModule3{DiffsStore = new StringModelStore(models[2])});
            var inheritAndModifyBaseView = new InheritAndModifyBaseView(application, viewType, attribute);

            inheritAndModifyBaseView.Verify(application.Model);
            application.Dispose();
        }

        private  string[] GetModels(ViewType viewType, bool attribute, Platform platform){
            var application = platform.NewApplication<ModelViewInheritanceModule>();
            CreateModelViewIneritanceModule(viewType, attribute, application);
            var inheritAndModifyBaseView = new InheritAndModifyBaseView(application, viewType, attribute);
            var models = inheritAndModifyBaseView.GetModels().ToArray();
            ModelViewInheritanceUpdater.Disabled = false;
            application.Dispose();
            return models;
        }


        private ModelViewInheritanceModule CreateModelViewIneritanceModule(ViewType viewType, bool attribute, XafApplication application,bool setup=true){
            CustomizeTypesInfo(viewType, attribute,application);
            var module = DefaultModelViewInheritancerModule(application,setup,typeof(ReactiveModule));
            return module;
        }

        private void CustomizeTypesInfo(ViewType viewType, bool attribute, XafApplication application){
            if (attribute){
                application.Modules.Add(new ReactiveModule());
                application.WhenCustomizingTypesInfo()
                    .FirstAsync(_=> {
                        _.FindTypeInfo(typeof(AMvi))
                            .AddAttribute(new ModelMergedDifferencesAttribute($"{nameof(AMvi)}_{viewType}",
                                $"{nameof(ABaseMvi)}_{viewType}"));
                        return true;
                    })
                    .Subscribe();
            }
        }

        private ModelViewInheritanceModule DefaultModelViewInheritancerModule(XafApplication application,bool setup=true,params Type[] modules){
            var baseBoTypes = new[]{typeof(ABaseMvi), typeof(TagMvi),typeof(Element)};
            var boTypes = new[]{typeof(AMvi), typeof(FileMvi)};
            var modelViewInheritanceModule = new ModelViewInheritanceModule();
            modelViewInheritanceModule.RequiredModuleTypes.AddRange(modules);
            application.AddModule(modelViewInheritanceModule,null,setup,baseBoTypes.Concat(boTypes).ToArray());
            return modelViewInheritanceModule;
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Chained_Cloned_listview_merging(bool deepMerge){
            string GetModel(){
                string xml;
                using (var xafApplication = Platform.Win.NewApplication<ModelViewInheritanceModule>()){
                    var inheritanceModule = DefaultModelViewInheritancerModule(xafApplication, true, typeof(CloneModelViewModule));
                    var model = inheritanceModule.Application.Model;
                    var modelApplication = ((ModelApplicationBase) model).CreatorInstance.CreateModelApplication();
                    ModelApplicationHelper.AddLayer(((ModelApplicationBase) model), modelApplication);

                    var elementListView = model.BOModel.GetClass(typeof(Element)).DefaultListView;
                    elementListView.Columns[nameof(Element.Street)].Index = -1;
                    var lvBase = ((IModelListView) model.Views[Element.ListViewBase]);
                    var mergedDifferences = ((IModelObjectViewMergedDifferences) lvBase).MergedDifferences;
                    var mergedDifference = mergedDifferences.AddNode<IModelMergedDifference>();
                    mergedDifference.View = elementListView;
                    mergedDifferences = ((IModelObjectViewMergedDifferences) model.Views[Element.ListViewBaseNested]).MergedDifferences;
                    mergedDifference = mergedDifferences.AddNode<IModelMergedDifference>();
                    mergedDifference.View = elementListView;
                    mergedDifference.DeepMerge = deepMerge;
                    if (!deepMerge){
                        mergedDifference = mergedDifferences.AddNode<IModelMergedDifference>();
                        mergedDifference.View = lvBase;
                    }
                    xml = modelApplication.Xml;
                    ModelApplicationHelper.RemoveLayer((ModelApplicationBase) xafApplication.Model);
                }

                return xml;
            }

            
            var modelXml = GetModel();

            using (var newApplication = Platform.Win.NewApplication<ModelViewInheritanceModule>()){
                var module = DefaultModelViewInheritancerModule(newApplication,false, typeof(CloneModelViewModule));
                var testModule1 = new TestModule1{DiffsStore = new StringModelStore(modelXml)};
                testModule1.AdditionalExportedTypes.Add(typeof(Element));
                newApplication.SetupDefaults(module, testModule1,new CloneModelViewModule());

                var listViewBase = ((IModelListView) newApplication.Model.Views[Element.ListViewBase]);
                listViewBase.Columns[nameof(Element.Street)].Index.ShouldBe(-1);
                ((IModelListView) newApplication.Model.Views[Element.ListViewBaseNested]).Columns[nameof(Element.Street)].Index.ShouldBe(-1);
            }
        }
    }

}