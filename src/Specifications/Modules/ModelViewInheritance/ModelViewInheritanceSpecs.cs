using System;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.XAF.Agnostic.Specifications.Artifacts;
using DevExpress.XAF.Agnostic.Specifications.Modules.ModelViewInheritance.BOModel;
using DevExpress.XAF.Modules.ModelViewInheritance;
using DevExpress.XAF.Modules.Reactive.Services;
using Xunit;

namespace DevExpress.XAF.Agnostic.Specifications.Modules.ModelViewInheritance{
    public class ModelViewInheritanceSpecs:BaseSpecs {
        [Theory]
        [ClassData(typeof(ModelViewInheritanceTestData))]
        public void Inherit_And_Modify_A_BaseView(ViewType viewType, bool attribute) {
            
            ModelViewInheritanceUpdater.Disabled = true;
            var application = new XafApplicationMock().Object;
            var modelViewIneritanceModule = CreateModelViewIneritanceModule(viewType, attribute, application);
            var baseBoTypes = new[]{typeof(ABaseMvi),typeof(TagMvi)};
            var boTypes = new[]{typeof(AMvi),typeof(FileMvi)};
            modelViewIneritanceModule.AdditionalExportedTypes.AddRange(baseBoTypes.Concat(boTypes));
            application.SetupDefaults(modelViewIneritanceModule);
            var inheritAndModifyBaseView = new InheritAndModifyBaseView(application,viewType,attribute);
            var models = inheritAndModifyBaseView.GetModels().ToArray();
            ModelViewInheritanceUpdater.Disabled = false;

            application = new XafApplicationMock().Object;
            modelViewIneritanceModule = CreateModelViewIneritanceModule(viewType, attribute, application);
            var testModule1 = new TestModule1{DiffsStore = new StringModelStore(models[0])};
            testModule1.AdditionalExportedTypes.AddRange(baseBoTypes);
            var testModule2 = new TestModule2{DiffsStore = new StringModelStore(models[1])};
            testModule2.AdditionalExportedTypes.AddRange(boTypes);

            application.SetupDefaults(modelViewIneritanceModule,testModule1, testModule2,
                new TestModule3{DiffsStore = new StringModelStore(models[2])});

            inheritAndModifyBaseView.Verify(application.Model);
        }


        private static ModelViewInheritanceModule CreateModelViewIneritanceModule(ViewType viewType, bool attribute,
            XafApplication application){
            CustomizeTypesInfo(viewType, attribute, application);
            var modelViewIneritanceModule = new ModelViewInheritanceModule();
            
            return modelViewIneritanceModule;
        }

        private static void CustomizeTypesInfo(ViewType viewType, bool attribute, XafApplication application){
            if (attribute){
                application.WhenCustomizingTypesInfo()
                    .Do(_ => {
                        _.FindTypeInfo(typeof(AMvi))
                            .AddAttribute(new ModelMergedDifferencesAttribute($"{nameof(AMvi)}_{viewType}",
                                $"{nameof(ABaseMvi)}_{viewType}"));
                    })
                    .Subscribe();
            }
        }
    }

}