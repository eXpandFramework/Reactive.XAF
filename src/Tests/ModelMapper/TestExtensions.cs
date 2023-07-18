using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Dashboards;
using DevExpress.ExpressApp.Dashboards.Win;
using DevExpress.ExpressApp.Model;
using Fasterflect;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;

namespace Xpand.XAF.Modules.ModelMapper.Tests{
    internal static class TestExtensions{
        internal static IObservable<Type> ModelInterfaces(this IObservable<Type> source){
            return (IObservable<Type>) typeof(TypeMappingService).Methods(Flags.StaticAnyDeclaredOnly,nameof(ModelInterfaces)).First(info => {
                var parameterInfos = info.Parameters();
                return parameterInfos.Count==1&&parameterInfos.First().ParameterType==typeof(IObservable<Type>);
            }).Call(null,source);
        }

        public static IEnumerable<Type> Modules(this PredefinedMap predefinedMap){
            if (predefinedMap == PredefinedMap.DashboardViewer){
                return new[]{typeof(DashboardsModule), typeof(DashboardsWindowsFormsModule)};
            }
            return Enumerable.Empty<Type>();
        }


        public static ModelMapperTestModule Extend(this PredefinedMap map, ModelMapperTestModule testModule = null, Action<ModelMapperConfiguration> configure = null){
            testModule ??= new ModelMapperTestModule();
            return new[]{map}.Extend(testModule, configure);
        }

        public static ModelMapperTestModule Extend(this PredefinedMap[] maps, ModelMapperTestModule testModule = null, Action<ModelMapperConfiguration> configure = null){
            testModule ??= new ModelMapperTestModule();
            testModule.ApplicationModulesManager.TakeFirst(_ => _.module==testModule)
                .SelectMany(_ => maps.Select(map => {
                    _.manager.Extend(map, configure);
                    return Unit.Default;
                }))
                .Subscribe();
            return testModule;
        }

        public static ModelMapperTestModule Extend(this Type extenderType, Action<ModelMapperConfiguration> configure, ModelMapperTestModule testModule = null){
            testModule ??= new ModelMapperTestModule();
            testModule.ApplicationModulesManager.TakeFirst()
                .Do(_ => {
                    var configuration = new ModelMapperConfiguration(extenderType);
                    configure?.Invoke(configuration);
                    _.manager.Extend(configuration);
                })
                .Subscribe();
            return testModule;
        }

        public static ModelMapperTestModule Extend<T>(this Type extenderType, ModelMapperTestModule testModule = null,
            Action<ModelMapperConfiguration> configure = null) where T : IModelNode{
            testModule ??= new ModelMapperTestModule();
            testModule.ApplicationModulesManager.TakeFirst()
                .Do(_ => {
                    var configuration = new ModelMapperConfiguration(extenderType, typeof(T));
                    configure?.Invoke(configuration);
                    _.manager.Extend(configuration);
                })
                .Subscribe();
            return testModule;
        }
    }
}