using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using Mono.Cecil;
using Xpand.Source.Extensions.System.String;

namespace Xpand.XAF.Modules.ModelMapper.Services.TypeMapping{
    public class CustomizeAttribute{
        public IList<(Attribute attribute,CustomAttribute customAttribute)> Attributes{ get; set; }
        public PropertyInfo PropertyInfo{ get; set; }
    }

    public static partial class TypeMappingService{

        private static void DefaultValueRule(CustomizeAttribute customizeAttribute){
            var defaultValueAttributes = customizeAttribute.Attributes.Where(_ => _.attribute is DefaultValueAttribute).ToArray();
            foreach (var descriptionAttribute in defaultValueAttributes){
                customizeAttribute.Attributes.Remove(descriptionAttribute);
            }
        }

        private static void PrivateDescriptionRule(CustomizeAttribute customizeAttribute){
            var privateDescriptionAttributes = customizeAttribute.Attributes
                .Where(_ => _.attribute is DescriptionAttribute && _.attribute.GetType().IsNotPublic).ToArray();
            foreach (var descriptionAttribute in privateDescriptionAttributes){
                customizeAttribute.Attributes.Remove(descriptionAttribute);
                var description = ((DescriptionAttribute) descriptionAttribute.attribute).Description;
                descriptionAttribute.customAttribute.SetArgumentValue( descriptionAttribute.customAttribute.ConstructorArguments.First(), description);
                customizeAttribute.Attributes.Add((new DescriptionAttribute(description),descriptionAttribute.customAttribute));
            }
        }

        private static void ConnectCustomizationRules(){
            CustomizeAttributes
                .SelectMany(customizeAttribute => AttributeMappingRules
                    .Select(_ => {
                        _.action(customizeAttribute);
                        return Unit.Default;
                    } ))
                .Subscribe();
            CustomizePropertySelection
                .SelectMany(propertyInfos => PropertyMappingRules
                    .Select(_ => {
                        _.action(propertyInfos);
                        return Unit.Default;
                    }))
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
                    return $"typeof({argument.Value})";
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

        static string ModelCode(this IEnumerable<(Attribute attribute,CustomAttribute customAttribute)> attributeData){
            var modelCode = string.Join(Environment.NewLine, attributeData
                .Where(_ => _.attribute.CanBeMapped())
                .Select(_ => _.customAttribute.AllArgsAreValid() ? $"[{_.attribute.GetType().FullName}({_.customAttribute.AttributeCtorArguments()})]" : null));
            return modelCode;
        }

        private static bool CanBeMapped(this Attribute attribute){
            return  attribute.GetType().IsPublic ;
        }

        private static bool AllArgsAreValid(this CustomAttribute customAttribute){
            var allArgsAreValid = customAttribute.ConstructorArguments
                .All(argument => (!(argument.Value is TypeDefinition typeDefinition) ||typeDefinition.IsPublic && !typeDefinition.IsFlag()));
            return allArgsAreValid;
        }

    }
}