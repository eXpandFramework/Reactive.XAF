using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Fasterflect;
using Mono.Cecil;
using Xpand.Source.Extensions.Linq;
using Xpand.Source.Extensions.MonoCecil;
using Xpand.Source.Extensions.System.Refelction;

namespace Xpand.XAF.Modules.ModelMapper.Services.TypeMapping{
    public static partial class TypeMappingService{
        private static Version _modelMapperModuleVersion;
        private static IEnumerable<Assembly> AllAssemblies(Type type, PropertyInfo[] propertyInfos){
            return propertyInfos.Select(info => info.PropertyType)
                .Concat(new[]{type,typeof(DescriptionAttribute),typeof(ModelMapperServiceAttribute)})
                .Concat(propertyInfos.SelectMany(info =>
                    info.GetCustomAttributes(typeof(Attribute), false).Select(o => o.GetType())))
                .Select(_ => _.Assembly)
                .Distinct();
        }

        private static Type GetRealType(this Type type){
            if (type.Name == "AnnotationShapePosition"){
                Debug.WriteLine("");
            }
            if (type != typeof(string)){
                if (typeof(IEnumerable).IsAssignableFrom(type)){
                    if (type.IsGenericType){
                        return type.GenericTypeArguments.First();
                    }
                    if (type.IsArray){
                        return type.GetElementType();
                    }

                    if (typeof(ICollection).IsAssignableFrom(type)){
                        var result = GetParameterType(type, "Add");
                        if (result == null){
                            result = GetParameterType(type, "CopyTo");
                            if (result != null)
                                return result;
                        }
                    }
                    var interfaceType = type.GetInterfaces().FirstOrDefault(_ => _.IsGenericType&&typeof(IEnumerable).IsAssignableFrom(_));
                    if (interfaceType!=null){
                        return interfaceType.GenericTypeArguments.First();
                    }

                    return typeof(object);
                }
            }
            return type;
        }

        private static Type GetParameterType(Type type,string name){
            var methodInfo = type.Methods(name).FirstOrDefault(info => info.Parameters().Any(parameterInfo => parameterInfo.ParameterType!=typeof(object)));
            if (methodInfo != null){
                var parameterType = methodInfo.Parameters().First().ParameterType;
                if (parameterType!=type&&parameterType!=typeof(Array)){
                    return parameterType.GetRealType();
                }

                return parameterType;
            }

            return null;
        }

        private static AssemblyDefinition[] AssemblyDefinitions(this Type type, Type[] types){
            return types
                .Select(_ => {
                    var realType = _.GetRealType();
                    return realType.Assembly;
                })
                .Concat(new[]{type}.Select(_ => _.Assembly))
                .Distinct()
                .Select(assembly => AssemblyDefinition.ReadAssembly(assembly.Location))
                .ToArray();
        }

        private static Type[] AdditionalTypes(this PropertyInfo[] propertyInfos,Type type){
            return propertyInfos
                .Where(_ => !_.PropertyType.IsValueType && typeof(string) != _.PropertyType && _.PropertyType != type)
                .Select(_ => _.PropertyType)
                .DistinctBy(_ => (_,type).ModelName())
                .ToArray();
        }

        private static PropertyInfo[] PropertyInfos(this Type type){
            return type.PublicProperties(true)
                .GetItems<PropertyInfo>(_ => {
                    var publicProperties = _.PropertyType.GetRealType().PublicProperties(true).ToArray();
                    return publicProperties;
                }, info => info.PropertyType)
                .ToArray();
        }
        
        private static IEnumerable<PropertyInfo> PublicProperties(this Type type,bool includeCollections=false){
            return type.Properties(Flags.AllMembers)
                .Where(info => !info.PropertyType.IsValueType && info.PropertyType != typeof(string) ||info.CanRead && info.CanWrite)
                .Where(info => info.IsValid(includeCollections))
                .Where(info => {
                    if (info.PropertyType == typeof(string) || info.PropertyType.IsNullableType()) return true;
                    var propertyTypeIsReserved = ReservedPropertyTypes.Any(_ => info.PropertyType!=_);
                    if (includeCollections && typeof(IEnumerable).IsAssignableFrom(info.PropertyType))
                        return true;
                    return !info.PropertyType.IsGenericType && info.PropertyType != type &&info.PropertyType != typeof(object) && propertyTypeIsReserved;
                })
                .DistinctBy(info => info.Name);
        }

        private static bool IsValid(this PropertyInfo info,bool includeCollections){
            var isValid = info.AccessModifier() == AccessModifier.Public && !ReservedPropertyNames.Contains(info.Name);
            if (!isValid) return false;
            if (includeCollections&&typeof(IEnumerable).IsAssignableFrom(info.PropertyType)){
                return true;
            }
            return !(typeof(IEnumerable).IsAssignableFrom(info.PropertyType) && info.PropertyType != typeof(string));
        }


        private static object GetEnums(this TypeReference typeReference, object value){
            var enumType = typeReference.ToType();
            if (EnumsNET.NonGeneric.NonGenericFlagEnums.IsFlagEnum(enumType)&&EnumsNET.NonGeneric.NonGenericFlagEnums.HasAnyFlags(enumType,value)){
                return string.Join("|", EnumsNET.NonGeneric.NonGenericFlagEnums.GetFlagMembers(enumType, value)
                    .Select(member => $"{enumType.FullName}.{member.Name}"));
            }

            var name = Enum.GetName(enumType, value);
            return $"{enumType.FullName}.{name}";
        }

        private static bool IsFlag(this TypeDefinition typeDefinition){
            return (typeDefinition.IsEnum&&typeDefinition.CustomAttributes.Any(attribute1 => attribute1.AttributeType.FullName==typeof(FlagsAttribute).FullName));
        }

    }
}