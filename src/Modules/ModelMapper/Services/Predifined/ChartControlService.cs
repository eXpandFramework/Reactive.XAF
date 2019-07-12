using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reflection;
using Fasterflect;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.ModelMapper.Services.Predifined{
    class ChartControlService{
        public static IObservable<Unit> Connect(Type typeToMap){
            var propertyInfo = typeToMap.Property("Diagram");
            var genericType = typeof(IList<>).MakeGenericType(propertyInfo.PropertyType);
            TypeMappingService.AdditionalTypesList.Add(genericType);
            TypeMappingService.PropertyMappingRules.Insert(0,(nameof(ChartDiagrams),data => ChartDiagrams(data,genericType,propertyInfo.Name)));
            TypeMappingService.TypeMappingRules.Insert(0,(nameof(ChartDiagrams),type => ChartDiagrams(type,propertyInfo,typeToMap)));
            return Unit.Default.AsObservable();
        }

        private static void ChartDiagrams(ModelMapperType modelMapperType, PropertyInfo propertyInfo,Type chartControlType){
            if (modelMapperType.TypeToMap == modelMapperType.Type &&propertyInfo.PropertyType != modelMapperType.TypeToMap &&
                propertyInfo.PropertyType.IsAssignableFrom(modelMapperType.Type)){
                var modelMapName = (propertyInfo.PropertyType).ModelMapName(chartControlType);
                modelMapperType.BaseTypeFullNames.Add(modelMapName);
            }
            
        }

        private static void ChartDiagrams((Type declaringType, List<ModelMapperPropertyInfo> propertyInfos) data,Type type, string propertyInfoName){
            if (data.declaringType.FullName == PredifinedMap.ChartControl.GetTypeName()){
                data.propertyInfos.RemoveAll(info => info.Name == propertyInfoName);
                data.propertyInfos.Add(new ModelMapperPropertyInfo("Diagrams",type,type.DeclaringType));
            }
        }
    }
}