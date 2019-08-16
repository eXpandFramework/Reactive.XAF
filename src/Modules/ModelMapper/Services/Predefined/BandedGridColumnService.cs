using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reflection;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.ModelMapper.Services.Predefined{
    class BandedGridColumnService{
        private static void BandedGridColumnPropertyRules((Type declaringType, List<ModelMapperPropertyInfo> propertyInfos) data){
            var typeName = PredefinedMap.BandedGridColumn.GetTypeName();
            if (data.declaringType.FullName == typeName){
                string[] names = {"ColVIndex", "RowIndex"};
                foreach (var info in data.propertyInfos.Where(info =>names.Contains(info.Name)).ToArray()){
                    info.RemoveAttribute(typeof(BrowsableAttribute));
                    info.RemoveAttribute(typeof(DesignerSerializationVisibilityAttribute));
                    info.AddAttributeData(typeof(CategoryAttribute),new CustomAttributeTypedArgument("Appearance"));
                    var propertyInfo = data.propertyInfos.First(_ => _.Name == info.Name);
                    data.ReplacePropertyInfo(propertyInfo, info);
                }
            }
        }

        public static IObservable<Unit> Connect(){
            TypeMappingService.PropertyMappingRules.Insert(0,(nameof(BandedGridColumnPropertyRules),BandedGridColumnPropertyRules));
            return Unit.Default.AsObservable();
        }

    }
}