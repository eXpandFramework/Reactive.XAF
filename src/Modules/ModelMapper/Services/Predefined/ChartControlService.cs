using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reflection;
using Fasterflect;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;

namespace Xpand.XAF.Modules.ModelMapper.Services.Predefined{
    static class ChartControlService{
        public static IObservable<Unit> Connect(Type typeToMap){
            var propertyInfo = typeToMap.Property("Diagram");
            var genericType = typeof(IList<>).MakeGenericType(propertyInfo.PropertyType);
            TypeMappingService.AdditionalTypesList.Add(genericType);
            TypeMappingService.PropertyMappingRules.Insert(0,(nameof(ChartDiagrams),data => ChartDiagrams(data,genericType,propertyInfo.Name)));
            TypeMappingService.TypeMappingRules.Insert(0,(nameof(ChartDiagrams),type => ChartDiagrams(type,propertyInfo,typeToMap)));
            return Unit.Default.ReturnObservable();
        }

        private static void ChartDiagrams(ModelMapperType modelMapperType, PropertyInfo propertyInfo,Type chartControlType){
            if (modelMapperType.TypeToMap == modelMapperType.Type){
                if (propertyInfo.PropertyType != modelMapperType.TypeToMap && propertyInfo.PropertyType.IsAssignableFrom(modelMapperType.Type)){
                    var modelMapName = (propertyInfo.PropertyType).ModelTypeName(chartControlType);
                    modelMapperType.BaseTypeFullNames.Add(modelMapName);
                }
            }
            
        }

        private static void ChartDiagrams((Type declaringType, List<ModelMapperPropertyInfo> propertyInfos) data,Type type, string propertyInfoName){
            if (data.declaringType.FullName == PredefinedMap.ChartControl.GetTypeName()){
                data.propertyInfos.RemoveAll(info => info.Name == propertyInfoName);
                data.propertyInfos.First(info => info.Name=="Series").RemoveAttribute(typeof(DesignerSerializationVisibilityAttribute));
                data.propertyInfos.Add(new ModelMapperPropertyInfo("Diagrams",type,type.DeclaringType));
            }
        }
    }
}