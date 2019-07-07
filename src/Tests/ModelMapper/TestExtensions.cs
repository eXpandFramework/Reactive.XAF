using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Model;
using Xpand.XAF.Modules.ModelMapper.Services;

namespace Xpand.XAF.Modules.ModelMapper.Tests{
    internal static class TestExtensions{
        public static ModelMapperTestModule Extend(this PredifinedMap map, ModelMapperTestModule testModule = null){
            testModule = testModule ?? new ModelMapperTestModule();
            return new[]{map}.Extend(testModule);
        }

        public static ModelMapperTestModule Extend(this PredifinedMap[] maps, ModelMapperTestModule testModule = null){
            testModule = testModule ?? new ModelMapperTestModule();
            testModule.ApplicationModulesManager.FirstAsync()
                .SelectMany(manager => maps.Select(map => {
                    manager.Extend(map);
                    return Unit.Default;
                }))
                .Subscribe();
            return testModule;
        }

        public static ModelMapperTestModule Extend<T>(this Type extenderType, ModelMapperTestModule testModule = null,IModelMapperConfiguration configuration=null) where T : IModelNode{
            testModule = testModule ?? new ModelMapperTestModule();
            testModule.ApplicationModulesManager.FirstAsync()
                .Do(manager => manager.Extend<T>(extenderType,configuration))
                .Subscribe();
            return testModule;
        }
    }
}