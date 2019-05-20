using System;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using Tests.Artifacts;
using Tests.Modules.ModelViewInheritance.BOModel;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.ModelViewInheritance;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xunit;

namespace Tests.Modules.ModelViewInheritance{
    [Collection(nameof(XafTypesInfo))]
    public class ModelViewInheritanceTests:BaseTest {
        [Theory]
        [ClassData(typeof(ModelViewInheritanceTestData))]
        internal void Inherit_And_Modify_A_BaseView(ViewType viewType, bool attribute,Platform platform){
            string[] models;
            ModelViewInheritanceUpdater.Disabled = true;
            using (var application = platform.NewApplication()){
                var modelViewIneritanceModule = CreateModelViewIneritanceModule(viewType, attribute, application);
                var baseBoTypes = new[]{typeof(ABaseMvi),typeof(TagMvi)};
                var boTypes = new[]{typeof(AMvi),typeof(FileMvi)};
                modelViewIneritanceModule.AdditionalExportedTypes.AddRange(baseBoTypes.Concat(boTypes));
                application.SetupDefaults(modelViewIneritanceModule);
                var inheritAndModifyBaseView = new InheritAndModifyBaseView(application, viewType, attribute);
                models = inheritAndModifyBaseView.GetModels().ToArray();
                ModelViewInheritanceUpdater.Disabled = false;
            }

            
            using (var application = platform.NewApplication()){
                var modelViewIneritanceModule = CreateModelViewIneritanceModule(viewType, attribute, application);
                var testModule1 = new TestModule1{DiffsStore = new StringModelStore(models[0])};
                var baseBoTypes = new[]{typeof(ABaseMvi), typeof(TagMvi)};
                var boTypes = new[]{typeof(AMvi), typeof(FileMvi)};
                testModule1.AdditionalExportedTypes.AddRange(baseBoTypes);
                var testModule2 = new TestModule2{DiffsStore = new StringModelStore(models[1])};
                testModule2.AdditionalExportedTypes.AddRange(boTypes);

                application.SetupDefaults(modelViewIneritanceModule, testModule1, testModule2,
                    new TestModule3{DiffsStore = new StringModelStore(models[2])});
                var inheritAndModifyBaseView = new InheritAndModifyBaseView(application, viewType, attribute);

                inheritAndModifyBaseView.Verify(application.Model);
            }
        }


        private static ModelViewInheritanceModule CreateModelViewIneritanceModule(ViewType viewType, bool attribute,
            XafApplication application){
            CustomizeTypesInfo(viewType, attribute, application);
            var modelViewInheritanceModule = new ModelViewInheritanceModule();
            modelViewInheritanceModule.RequiredModuleTypes.Add(typeof(ReactiveModule));
            return modelViewInheritanceModule;
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