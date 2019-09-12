using System;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using Shouldly;
using TestsLib;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.CloneModelView;
using Xpand.XAF.Modules.ModelViewInheritance.Tests.BOModel;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xunit;

namespace Xpand.XAF.Modules.ModelViewInheritance.Tests{
    [Collection(nameof(ModelViewInheritanceModule))]
    public class ModelViewInheritanceTests:BaseTest {
        [Theory]
        [ClassData(typeof(ModelViewInheritanceTestData))]
        internal void Inherit_And_Modify_A_BaseView(ViewType viewType, bool attribute,Platform platform){
            ModelViewInheritanceUpdater.Disabled = true;
            var models = GetModels(viewType, attribute, platform);

            var application = platform.NewApplication();
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
            var application = platform.NewApplication();
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

        [Fact]
        public void Chained_Cloned_listview_merging(){
            string GetModel(){
                var xafApplication = Platform.Win.NewApplication();
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
                mergedDifference = mergedDifferences.AddNode<IModelMergedDifference>();
                mergedDifference.View = lvBase;
                var xml = modelApplication.Xml;
                ModelApplicationHelper.RemoveLayer((ModelApplicationBase) xafApplication.Model);
                return xml;
            }

            
            var modelXml = GetModel();

            var newApplication = Platform.Win.NewApplication();
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