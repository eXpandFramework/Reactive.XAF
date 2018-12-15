using System;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.XAF.Agnostic.Specifications.Artifacts;
using DevExpress.XAF.Agnostic.Specifications.Modules.ModelViewInheritance.BOModel;
using DevExpress.XAF.Modules.ModelViewIneritance;
using DevExpress.XAF.Modules.Reactive.Services;
using Xunit;

namespace DevExpress.XAF.Agnostic.Specifications.Modules.ModelViewInheritance{
    public class ModelViewInheritanceSpecs:BaseSpecs {
        [Theory]
        [ClassData(typeof(ModelViewInheritanceTestData))]
        public void Inherit_And_Modify_A_BaseView(ViewType viewType, bool attribute) {
            if (attribute&&viewType==ViewType.DetailView)
                return;
            ModelViewInheritanceUpdater.Disabled = true;
            var application = new XafApplicationMock().Object;
            var modelViewIneritanceModule = CreateModelViewIneritanceModule(viewType, attribute, application);
            application.SetupDefaults(modelViewIneritanceModule);
            var inheritAndModifyBaseView = new InheritAndModifyBaseView(application,viewType,attribute);
            var models = inheritAndModifyBaseView.ToArray();
            ModelViewInheritanceUpdater.Disabled = false;

            application = new XafApplicationMock().Object;
            modelViewIneritanceModule = CreateModelViewIneritanceModule(viewType, attribute, application);
            application.SetupDefaults(modelViewIneritanceModule, 
                new TestModule1{DiffsStore = new StringModelStore(models[0])},
                new TestModule2{DiffsStore = new StringModelStore(models[1])},
                new TestModule3{DiffsStore = new StringModelStore(models[2])});

            inheritAndModifyBaseView.Verify(application.Model);
        }


        private static ModelViewIneritanceModule CreateModelViewIneritanceModule(ViewType viewType, bool attribute,
            XafApplication application){
            CustomizeTypesInfo(viewType, attribute, application);
            var modelViewIneritanceModule = new ModelViewIneritanceModule();
            modelViewIneritanceModule.AdditionalExportedTypes.AddRange(new[]
                {typeof(ModelViewInheritanceClassA), typeof(ModelViewInheritanceClassB)});
            return modelViewIneritanceModule;
        }

        private static void CustomizeTypesInfo(ViewType viewType, bool attribute, XafApplication application){
            if (attribute){
                application.WhenCustomizingTypesInfo()
                    .Do(_ => {
                        _.FindTypeInfo(typeof(ModelViewInheritanceClassB))
                            .AddAttribute(new ModelMergedDifferencesAttribute($"ModelViewInheritanceClassB_{viewType}",
                                $"ModelViewInheritanceClassA_{viewType}"));
                    })
                    .Subscribe();
            }
        }
    }

}