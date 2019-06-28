using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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

        private static AssemblyDefinition[] AssemblyDefinitions(this Type type, Type[] types){
            return types
                .Select(_ => _.Assembly)
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
            return type.PublicProperties()
                .GetItems<PropertyInfo>(_ => _.PropertyType.PublicProperties(), info => info.PropertyType)
                .ToArray();
        }
        
        private static IEnumerable<PropertyInfo> PublicProperties(this Type type){
            return type.Properties(Flags.AllMembers)
                .Where(info => {
                    if (info.PropertyType.IsValueType || info.PropertyType == typeof(string)){
                        return info.CanRead && info.CanWrite;
                    }

                    return true;
                })
                .Where(IsValid)
                .Where(info => {
                    if (info.PropertyType == typeof(string) || info.PropertyType.IsNullableType()) return true;
                    return !info.PropertyType.IsGenericType && info.PropertyType != type &&
                           info.PropertyType != typeof(object) && ReservedPropertyTypes.Any(_ => info.PropertyType!=_);
                })
                .DistinctBy(info => info.Name);
        }

        private static bool IsValid(this PropertyInfo info){
            return info.AccessModifier() == AccessModifier.Public && !ReservedPropertyNames.Contains(info.Name) &&
                   (info.PropertyType == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(info.PropertyType));
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