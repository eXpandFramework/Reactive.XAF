using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using EnumsNET;
using Fasterflect;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.StringExtensions;

namespace Xpand.XAF.Modules.ModelMapper.Services.TypeMapping{
    public static partial class TypeMappingService{
        private static void CompilerIsReadOnly((Type declaringType, List<ModelMapperPropertyInfo> propertyInfos) tuple){
            foreach (var propertyInfo in tuple.propertyInfos.ToArray()){
                var isReadOnlyData = propertyInfo.GetCustomAttributesData().Where(_ => _.AttributeType.FullName=="System.Runtime.CompilerServices.IsReadOnlyAttribute");
                foreach (var attributeData in isReadOnlyData){
                    propertyInfo.RemoveAttributeData(attributeData);
                }
            }

        }
        private static void CompilerIsReadOnly(GenericEventArgs<ModelMapperType> e) {
            var modelMapperType = e.Instance;
            foreach (var argument in modelMapperType.CustomAttributeData.Where(data => data.AttributeType.FullName=="System.Runtime.CompilerServices.IsReadOnlyAttribute").ToArray()){
                modelMapperType.CustomAttributeData.Remove(argument);
            }
        }

        private static void TypeConverterWithDXDesignTimeType((Type declaringType, List<ModelMapperPropertyInfo> propertyInfos) tuple){
            
            foreach (var propertyInfo in tuple.propertyInfos.ToArray()){
                var typeConverterData = propertyInfo.GetCustomAttributesData().Where(_ => typeof(TypeConverterAttribute).IsAssignableFrom(_.AttributeType));
                var customAttributeData = typeConverterData.Where(_ => _.ConstructorArguments.Any(argument => {
                    var value = $"{argument.Value}";
                    return value.StartsWith("DevExpress") && value.Contains("Design");
                })).ToArray();
                foreach (var attributeData in customAttributeData){
                    propertyInfo.RemoveAttributeData(attributeData);
                }
            }
        }

        internal static void ReplacePropertyInfo(this (Type declaringType, List<ModelMapperPropertyInfo> propertyInfos) tuple, ModelMapperPropertyInfo propertyInfo,
            ModelMapperPropertyInfo modelMapperPropertyInfo){
            tuple.propertyInfos.Remove(propertyInfo);
            tuple.propertyInfos.Add(modelMapperPropertyInfo);
        }

        private static ModelMapperCustomAttributeData[] WithNonPublicAttributeParameters(this IList<ModelMapperCustomAttributeData> attributeData) 
            => attributeData.Where(_ => !_.ConstructorArguments.All(argument =>argument.ArgumentType == typeof(Type)
                ? (((Type) argument.Value).IsPublic ||(((Type) argument.Value).IsNested && ((Type) argument.Value).IsNestedPublic))
                : argument.ArgumentType.IsPublic)).ToArray();

        private static void DefaultValueRule((Type declaringType, List<ModelMapperPropertyInfo> propertyInfos) tuple){
            foreach (var propertyInfo in tuple.propertyInfos.ToArray()){
                var defaultValueData = propertyInfo.GetCustomAttributesData().Where(_ =>typeof(DefaultValueAttribute).IsAssignableFrom(_.AttributeType)).ToArray();
                foreach (var attributeData in defaultValueData){
                    propertyInfo.RemoveAttributeData(attributeData);
                }
            }
        }

        private static void PrivateDescriptionRule((Type declaringType, List<ModelMapperPropertyInfo> propertyInfos) tuple){
            foreach (var propertyInfo in tuple.propertyInfos.ToArray()){
                var customAttributeData = propertyInfo.GetCustomAttributesData();
                var descriptionAttributes = customAttributeData.Where(_ =>_.AttributeType.Name != "WebSysDescriptionAttribute" &&
                        typeof(DescriptionAttribute).IsAssignableFrom(_.AttributeType) && _.AttributeType.IsNotPublic).ToArray();
                foreach (var attributeData in descriptionAttributes){
                    propertyInfo.RemoveAttributeData(attributeData);
                    var descriptionAttribute = (DescriptionAttribute)attributeData.AttributeType.TryCreateInstanceWithValues(attributeData.ConstructorArguments.First().Value);
                    propertyInfo.AddAttributeData(typeof(DescriptionAttribute), new CustomAttributeTypedArgument(descriptionAttribute.Description));
                }
            }
        }

        private static void ConnectCustomizationRules(){
            _customizeProperties
                .SelectMany(data => {
                    return PropertyMappingRules
                        .Select(_ => {
                            _.action(data);
                            return Unit.Default;
                        });
                })
                .Subscribe();
            _customizeContainerCode
                .SelectMany(data => {
                    return ContainerMappingRules
                        .Select(_ => {
                            _.action(data);
                            return Unit.Default;
                        });
                })
                .Subscribe();
            _customizeTypes
                .SelectMany(args => {
                    return TypeMappingRules
                        .Select(_ => {
                            _.action(args);
                            return Unit.Default;
                        });
                })
                .Subscribe();
        }

        static string AttributeCtorArguments(this ModelMapperCustomAttributeData customAttributeData) 
            => string.Join(",", customAttributeData.ConstructorArguments.Select(argument => {
                if (argument.Value == null){
                    return "null";
                }
                if (argument.ArgumentType== typeof(string)&& argument.Value != null){
                    var literal = argument.Value.ToString().ToLiteral();
                    return literal;
                }
                if (argument.ArgumentType== typeof(Type)){
                    var argumentValue = $"{argument.Value}";
                    return $"typeof({argumentValue.Replace("+",".")})";
                }
                if (argument.ArgumentType== typeof(bool)){
                    return argument.Value?.ToString().ToLower();
                }
                return argument.ArgumentType.IsEnum ? argument.ArgumentType.GetEnums( argument.Value) : argument.Value;
            }));

        static string ModelCode(this IEnumerable<ModelMapperCustomAttributeData> customAttributeData) 
            => string.Join(Environment.NewLine, customAttributeData.Where(_ => _.AttributeType.IsPublic)
                .Select(_ => _.AllArgsAreValid() ? $"[{_.AttributeType.FullName}({_.AttributeCtorArguments()})]" : null));

        private static bool AllArgsAreValid(this ModelMapperCustomAttributeData customAttributeData) 
            => customAttributeData.ConstructorArguments
                .All(argument => {
                    var type = argument.ArgumentType;
                    return argument.Value is Type || !type.IsEnum || !FlagEnums.IsFlagEnum(type) ||
                           FlagEnums.GetFlagMembers(type, argument.Value).Count == 1;
                });

        private static void NonPublicAttributeParameters(this GenericEventArgs<ModelMapperType> e){
            foreach (var data in e.Instance.CustomAttributeData.WithNonPublicAttributeParameters()){
                e.Instance.CustomAttributeData.Remove(data);
            }
        }

        private static void NonPublicAttributeParameters((Type declaringType, List<ModelMapperPropertyInfo> propertyInfos) tuple){
            foreach (var propertyInfo in tuple.propertyInfos.ToArray()){
                foreach (var argumentData in propertyInfo.GetCustomAttributesData().WithNonPublicAttributeParameters()){
                    propertyInfo.RemoveAttributeData(argumentData);
                }
            }
        }

        private static ModelMapperCustomAttributeData[] WithGenericTypeArguments(this IList<ModelMapperCustomAttributeData> attributeData) 
            => attributeData.Where(_ => _.ConstructorArguments.Any(argument =>
                argument.ArgumentType == typeof(Type) && ((Type) argument.Value).IsGenericType)).ToArray();

        private static void GenericTypeRule((Type declaringType, List<ModelMapperPropertyInfo> propertyInfos) info)
            => info.propertyInfos.RemoveAll(propertyInfo => propertyInfo.PropertyType.GenericTypeFilter());

        private static bool GenericTypeFilter(this Type propertyType) 
            => typeof(IEnumerable).IsAssignableFrom(propertyType) &&
               propertyType.IsGenericType &&
               propertyType.GenericTypeArguments.Any(type => type.IsGenericType);

        private static void BrowsableRule((Type declaringType, List<ModelMapperPropertyInfo> propertyInfos) info){
            for (var index = info.propertyInfos.Count - 1; index >= 0; index--){
                var propertyInfo = info.propertyInfos[index];
                var isNotBrowsable = propertyInfo.GetCustomAttributesData().Any(data =>
                    (typeof(BrowsableAttribute).IsAssignableFrom(data.AttributeType) &&
                     data.ConstructorArguments.Any(argument => false.Equals(argument.Value)))||typeof(ObsoleteAttribute).IsAssignableFrom(data.AttributeType));
                if (isNotBrowsable){
                    info.propertyInfos.Remove(propertyInfo);
                }
            }
        }

        private static void DesignerSerializationVisibilityAttribute((Type declaringType, List<ModelMapperPropertyInfo> propertyInfos) data){
            var infos = data.propertyInfos.Where(info => info.GetCustomAttributesData().Any(_ =>typeof(DesignerSerializationVisibilityAttribute).IsAssignableFrom(_.AttributeType) &&
                _.ConstructorArguments.Any(argument => argument.ArgumentType==typeof(DesignerSerializationVisibility)&&(int)argument.Value== (int)DesignerSerializationVisibility.Hidden)))
                .ToArray();
            foreach (var info in infos){
                info.AddAttributeData(typeof(BrowsableAttribute),new CustomAttributeTypedArgument(false));
            }
        }

        private static void GenericTypeArguments(GenericEventArgs<ModelMapperType> e) {
            var modelMapperType = e.Instance;
            foreach (var argument in modelMapperType.CustomAttributeData.WithGenericTypeArguments()){
                modelMapperType.CustomAttributeData.Remove(argument);
            }
        }

        private static void GenericTypeArguments((Type declaringType, List<ModelMapperPropertyInfo> propertyInfos) data){
            foreach (var propertyInfo in data.propertyInfos.ToArray()){
                foreach (var argumentData in propertyInfo.GetCustomAttributesData().WithGenericTypeArguments()){
                    propertyInfo.RemoveAttributeData(argumentData);
                }
            }
        }

        private static void GenericTypeRule(GenericEventArgs<ModelMapperType> e) 
            => e.Handled = e.Instance.ModelName.Contains("`");
    }
}