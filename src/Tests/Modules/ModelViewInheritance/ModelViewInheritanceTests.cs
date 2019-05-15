using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using AppDomainToolkit;
using DevExpress.ExpressApp;
using Tests.Artifacts;
using Tests.Modules.ModelViewInheritance.BOModel;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.ModelViewInheritance;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xunit;

namespace Tests.Modules.ModelViewInheritance{
//    [Collection(nameof(XafTypesInfo))]
    public class ModelViewInheritanceTests:BaseTest {
        [Theory]
        [ClassData(typeof(ModelViewInheritanceTestData))]
        internal void Inherit_And_Modify_A_BaseView(ViewType viewType, bool attribute,Platform platform){
            string[] models;
            using (var appDomainContext = AppDomainContext.Create()){
                models = RemoteFunc.Invoke(appDomainContext.Domain, viewType,attribute,platform, (t, a, p) => {
                    ModelViewInheritanceUpdater.Disabled = true;
                    using (var application = p.NewApplication()){
                        var modelViewIneritanceModule = CreateModelViewIneritanceModule(t, a, application);
                        var baseBoTypes = new[]{typeof(ABaseMvi),typeof(TagMvi)};
                        var boTypes = new[]{typeof(AMvi),typeof(FileMvi)};
                        modelViewIneritanceModule.AdditionalExportedTypes.AddRange(baseBoTypes.Concat(boTypes));
                        application.SetupDefaults(modelViewIneritanceModule);
                        var inheritAndModifyBaseView = new InheritAndModifyBaseView(application, t, a);
                        var m = inheritAndModifyBaseView.GetModels().ToArray();
                        ModelViewInheritanceUpdater.Disabled = false;
                        return m;
                    }
                });
            }

            
            RemoteFunc.Invoke(Domain, viewType, attribute, platform, models, (t, a, p, m) => {
                using (var application = p.NewApplication()){
                    var modelViewIneritanceModule = CreateModelViewIneritanceModule(t, a, application);
                    var testModule1 = new TestModule1{DiffsStore = new StringModelStore(m[0])};
                    var baseBoTypes = new[]{typeof(ABaseMvi), typeof(TagMvi)};
                    var boTypes = new[]{typeof(AMvi), typeof(FileMvi)};
                    testModule1.AdditionalExportedTypes.AddRange(baseBoTypes);
                    var testModule2 = new TestModule2{DiffsStore = new StringModelStore(m[1])};
                    testModule2.AdditionalExportedTypes.AddRange(boTypes);

                    application.SetupDefaults(modelViewIneritanceModule, testModule1, testModule2,
                        new TestModule3{DiffsStore = new StringModelStore(m[2])});
                    var inheritAndModifyBaseView = new InheritAndModifyBaseView(application, t, a);

                    inheritAndModifyBaseView.Verify(application.Model);
                }

                return Unit.Default;
            });
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