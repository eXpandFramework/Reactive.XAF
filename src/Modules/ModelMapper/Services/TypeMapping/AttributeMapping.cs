using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using EnumsNET.NonGeneric;
using Fasterflect;
using Xpand.Source.Extensions.System.String;

namespace Xpand.XAF.Modules.ModelMapper.Services.TypeMapping{
    public static partial class TypeMappingService{
        private static void TypeConverterWithDXDesignTimeType((Type declaringType, List<ModelMapperPropertyInfo> propertyInfos) tuple){
            
            foreach (var propertyInfo in tuple.propertyInfos.ToArray()){
                var typeConverterDatas = propertyInfo.GetCustomAttributesData().Where(_ => typeof(TypeConverterAttribute).IsAssignableFrom(_.AttributeType));
                var customAttributeDatas = typeConverterDatas.Where(_ => _.ConstructorArguments.Any(argument => {
                    var value = $"{argument.Value}";
                    return value.StartsWith("DevExpress") && value.Contains("Design");
                })).ToArray();
                foreach (var attributeData in customAttributeDatas){
                    propertyInfo.RemoveAttributeData(attributeData);
                }
            }
        }

        internal static void ReplacePropertyInfo(this (Type declaringType, List<ModelMapperPropertyInfo> propertyInfos) tuple, ModelMapperPropertyInfo propertyInfo,
            ModelMapperPropertyInfo modelMapperPropertyInfo){
            tuple.propertyInfos.Remove(propertyInfo);
            tuple.propertyInfos.Add(modelMapperPropertyInfo);
        }

        private static ModelMapperCustomAttributeData[] WithNonPublicAttributeParameters(this IList<ModelMapperCustomAttributeData> attributeDatas){
            var nonPublicArgumentDatas = attributeDatas.Where(_ => !_.ConstructorArguments.All(argument =>argument.ArgumentType == typeof(Type)
                    ? (((Type) argument.Value).IsPublic ||(((Type) argument.Value).IsNested && ((Type) argument.Value).IsNestedPublic))
                    : argument.ArgumentType.IsPublic)).ToArray();
            return nonPublicArgumentDatas;
        }

        private static void DefaultValueRule((Type declaringType, List<ModelMapperPropertyInfo> propertyInfos) tuple){
            foreach (var propertyInfo in tuple.propertyInfos.ToArray()){
                var defaultValueDatas = propertyInfo.GetCustomAttributesData().Where(_ =>typeof(DefaultValueAttribute).IsAssignableFrom(_.AttributeType)).ToArray();
                foreach (var attributeData in defaultValueDatas){
                    propertyInfo.RemoveAttributeData(attributeData);
                }
            }
        }

        private static void PrivateDescriptionRule((Type declaringType, List<ModelMapperPropertyInfo> propertyInfos) tuple){
            foreach (var propertyInfo in tuple.propertyInfos.ToArray()){
                var customAttributeDatas = propertyInfo.GetCustomAttributesData();
                var descriptionAttributes = customAttributeDatas.Where(_ =>_.AttributeType.Name != "WebSysDescriptionAttribute" &&
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
                .SelectMany(mapperType => {
                    return TypeMappingRules
                        .Select(_ => {
                            _.action(mapperType);
                            return Unit.Default;
                        });
                })
                .Subscribe();
        }

        static string AttributeCtorArguments(this ModelMapperCustomAttributeData customAttributeData){
            var ctorArguments = string.Join(",", customAttributeData.ConstructorArguments.Select(argument => {
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
            return ctorArguments;
        }

        static string ModelCode(this IEnumerable<ModelMapperCustomAttributeData> customAttributeDatas){
            var modelCode = string.Join(Environment.NewLine, customAttributeDatas.Where(_ => _.AttributeType.IsPublic)
                .Select(_ => _.AllArgsAreValid() ? $"[{_.AttributeType.FullName}({_.AttributeCtorArguments()})]" : null));
            return modelCode;
        }

        private static bool AllArgsAreValid(this ModelMapperCustomAttributeData customAttributeData){
            var allArgsAreValid = customAttributeData.ConstructorArguments
                .All(argument => {
                    var type = argument.ArgumentType;
                    return argument.Value is Type || !type.IsEnum || !NonGenericFlagEnums.IsFlagEnum(type) ||
                           NonGenericFlagEnums.GetFlagMembers(type, argument.Value).Count() == 1;
                });
            return allArgsAreValid;
        }

        private static void NonPublicAttributeParameters(ModelMapperType modelMapperType){
            foreach (var data in modelMapperType.CustomAttributeDatas.WithNonPublicAttributeParameters()){
                modelMapperType.CustomAttributeDatas.Remove(data);
            }
        }

        private static void NonPublicAttributeParameters((Type declaringType, List<ModelMapperPropertyInfo> propertyInfos) tuple){
            foreach (var propertyInfo in tuple.propertyInfos.ToArray()){
                foreach (var argumentData in propertyInfo.GetCustomAttributesData().WithNonPublicAttributeParameters()){
                    propertyInfo.RemoveAttributeData(argumentData);
                }
            }
        }

        private static ModelMapperCustomAttributeData[] WithGenericTypeArguments(this IList<ModelMapperCustomAttributeData> attributeDatas){
            return attributeDatas.Where(_ => _.ConstructorArguments.Any(argument =>
                argument.ArgumentType == typeof(Type) && ((Type) argument.Value).IsGenericType)).ToArray();
            
        }

        private static void GenericTypeArguments(ModelMapperType modelMapperType){
            foreach (var argument in modelMapperType.CustomAttributeDatas.WithGenericTypeArguments()){
                modelMapperType.CustomAttributeDatas.Remove(argument);
            }
        }

        private static void GenericTypeArguments((Type declaringType, List<ModelMapperPropertyInfo> propertyInfos) data){
            foreach (var propertyInfo in data.propertyInfos.ToArray()){
                foreach (var argumentData in propertyInfo.GetCustomAttributesData().WithGenericTypeArguments()){
                    propertyInfo.RemoveAttributeData(argumentData);
                }
            }
        }
    }
}