using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Fasterflect;
using Mono.Cecil;
using Xpand.Source.Extensions.MonoCecil;
using Xpand.Source.Extensions.System.String;

namespace Xpand.XAF.Modules.ModelMapper.Services.TypeMapping{
    public static partial class TypeMappingService{
        private static void TypeConverterWithDXDesignTimeType((PropertyDefinition propertyDefinition, List<CustomAttribute> customAttributes) tuple){
            var typeConverterAttributes = tuple.customAttributes.Where(_ =>typeof(TypeConverterAttribute).IsAssignableFrom(_.AttributeType.ToType())  )
                .ToArray();

            void RemoveNonPublicAttribute((Type type, object value)[] argumentsValues, Type type,
                CustomAttribute customAttribute){
                var converterTypeName = $"{argumentsValues.FirstOrDefault(_ => _.type==type).value}";
                if (converterTypeName.StartsWith("DevExpress") && converterTypeName.Contains("Design")){
                    tuple.customAttributes.Remove(customAttribute);
                }
            }

            foreach (var attribute in typeConverterAttributes){
                var argumentsValues = attribute.ConstructorArgumentsValues();
                RemoveNonPublicAttribute(argumentsValues,typeof(Type), attribute);
                RemoveNonPublicAttribute(argumentsValues,typeof(string), attribute);
            }
        }

        private static void NonPublicAttributeParameters((PropertyDefinition propertyDefinition, List<CustomAttribute> customAttributes) tuple){
            var attributes = tuple.customAttributes.Where(attribute => attribute.ConstructorArgumentsValues().Any(_ =>
                    _.type == typeof(Type) && _.value != null ? ((Type) _.value).IsNotPublic : _.type.IsNotPublic)).ToArray();
            foreach (var customAttribute in attributes){
                tuple.customAttributes.Remove(customAttribute);
            }
        }

        private static void DefaultValueRule((PropertyDefinition propertyDefinition, List<CustomAttribute> customAttributes) tuple){
            var defaultValueAttributes = tuple.customAttributes.Where(_ => typeof(DefaultValueAttribute).IsAssignableFrom(_.AttributeType.ToType()))
                .ToArray();
            foreach (var descriptionAttribute in defaultValueAttributes){
                tuple.customAttributes.Remove(descriptionAttribute);
            }
        }

        private static void PrivateDescriptionRule((PropertyDefinition propertyDefinition, List<CustomAttribute> customAttributes) tuple){
            var privateDescriptionAttributes = tuple.customAttributes
                .Where(_ => {
                    var type = _.AttributeType.ToType();
                    return type.Name != "WebSysDescriptionAttribute" &&(typeof(DescriptionAttribute).IsAssignableFrom(type) && type.IsNotPublic);
                }).ToArray();
            var constructor = typeof(DescriptionAttribute).GetConstructor(new []{typeof(string)});
            var reference = tuple.propertyDefinition.Module.ImportReference(constructor);
            foreach (var descriptionAttribute in privateDescriptionAttributes){
                tuple.customAttributes.Remove(descriptionAttribute);
                var attributeType = descriptionAttribute.AttributeType.ToType();
                if (attributeType.Constructor(typeof(string)) != null){
                    var text = (string) (descriptionAttribute.ConstructorArgumentsValues().First(_ => _.type == typeof(string))).value;
                    var attribute = (DescriptionAttribute)Activator.CreateInstance(attributeType,text);
                    var customAttribute = new CustomAttribute(reference);
                    customAttribute.ConstructorArguments.Add(new CustomAttributeArgument(tuple.propertyDefinition.Module.TypeSystem.String,attribute.Description));
                    tuple.customAttributes.Add(customAttribute);
                }
                
            }
        }

        private static void ConnectCustomizationRules(){
            CustomizeAttributes
                .SelectMany(tuple => AttributeMappingRules
                    .Select(_ => {
                        _.action(tuple);
                        return Unit.Default;
                    } ))
                .Subscribe();
            CustomizePropertySelection
                .SelectMany(propertyInfos => {
                    return PropertyMappingRules
                        .Select(_ => {
                            _.action(propertyInfos);
                            return Unit.Default;
                        });
                })
                .Subscribe();
        }

        static string AttributeCtorArguments(this CustomAttribute attribute){
            var ctorArguments = string.Join(",", attribute.ConstructorArguments.Select(argument => {
                if (argument.Value == null){
                    return "null";
                }
                if (argument.Type.FullName == typeof(string).FullName && argument.Value != null){
                    var literal = argument.Value.ToString().ToLiteral();
                    return literal;
                }
                if (argument.Type.FullName == typeof(Type).FullName){
                    var argumentValue = $"{argument.Value}";
                    return $"typeof({argumentValue.Replace("/",".")})";
                }
                if (argument.Type.FullName == typeof(bool).FullName){
                    return argument.Value?.ToString().ToLower();
                }
                if (argument.Value is CustomAttributeArgument customAttributeArgument){
                    var typeReference = customAttributeArgument.Type;
                    var value = customAttributeArgument.Value;
                    return typeReference.Resolve().IsEnum ? GetEnums(typeReference, value) : value ?? "null";
                }
                var resolvedType = argument.Type.Resolve();
                return resolvedType.IsEnum ? GetEnums(argument.Type, argument.Value) : argument.Value;
            }));
            return ctorArguments;
        }

        static string ModelCode(this IEnumerable<CustomAttribute> attributeData){
            var modelCode = string.Join(Environment.NewLine, attributeData.Where(_ => _.AttributeType.ToType().IsPublic)
                .Select(_ => _.AllArgsAreValid() ? $"[{_.AttributeType.FullName}({_.AttributeCtorArguments()})]" : null));
            return modelCode;
        }

//        private static bool CanBeMapped(this Attribute attribute){
//            return  attribute.GetType().IsPublic ;
//        }

        private static bool AllArgsAreValid(this CustomAttribute customAttribute){
            var allArgsAreValid = customAttribute.ConstructorArguments
                .All(argument => {
                    if (argument.Value is TypeDefinition)
                        return true;
                    var type = argument.Type.ToType();
                    if (type.IsEnum){
                        if (!EnumsNET.NonGeneric.NonGenericFlagEnums.IsFlagEnum(type)){
                            return true;
                        }
                        return EnumsNET.NonGeneric.NonGenericFlagEnums.GetFlagMembers(type, argument.Value).Count() == 1;
                    }

                    return true;

                });
            return allArgsAreValid;
        }

    }
}