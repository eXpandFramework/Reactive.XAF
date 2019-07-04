using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reflection;
using Fasterflect;
using Mono.Cecil;
using Xpand.Source.Extensions.MonoCecil;
using Xpand.Source.Extensions.System.Refelction;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.ModelMapper.Services{
    class BandedGridColumnService{
        private static void BandedGridColumnPropertyRules((Type declaringType, List<PropertyInfo> propertyInfos) data){
            var typeName = PredifinedMap.BandedGridColumn.GetTypeName();
            if (data.declaringType.FullName == typeName){
                var dynamicPropertyInfos = new[]{"ColVIndex", "RowIndex"}.Select(name => new DynamicPropertyInfo(data.declaringType.Property(name)));
                foreach (var info in dynamicPropertyInfos){
                    info.RemoveAttribute(new BrowsableAttribute(false));
                    info.AddAttribute(new CategoryAttribute("Appearance"));
                    data.propertyInfos.Remove(data.propertyInfos.First(_ => _.Name == info.Name));
                    data.propertyInfos.Add(info);
                }
            }
        }

        public static IObservable<Unit> Connect(){
            TypeMappingService.PropertyMappingRules.Insert(0,(nameof(BandedGridColumnPropertyRules),BandedGridColumnPropertyRules));
            TypeMappingService.AttributeMappingRules.Insert(0,(nameof(BandedGridColumnAttributeRules),BandedGridColumnAttributeRules));
            return Unit.Default.AsObservable();
        }

        private static void BandedGridColumnAttributeRules((PropertyDefinition propertyDefinition, List<CustomAttribute> customAttributes) tuple){
            var typeName = PredifinedMap.BandedGridColumn.GetTypeName();
            if (tuple.propertyDefinition.DeclaringType.FullName == typeName&&new[]{"ColVIndex","RowIndex"}.Contains(tuple.propertyDefinition.Name)){
                var customAttribute = tuple.customAttributes.FirstOrDefault(_ => _.AttributeType.ToType()==typeof(BrowsableAttribute));
                if (customAttribute != null) tuple.customAttributes.Remove(customAttribute);
            }

        }
    }
}