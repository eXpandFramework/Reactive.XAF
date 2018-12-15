using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Utils;

namespace DevExpress.XAF.Extensions.TypeInfo{
    public static class TypesInfoExtensions {
        public static IEnumerable<ITypeInfo> BaseInfos(this ITypeInfo typeInfo) {
            var baseInfo = typeInfo.Base;
            while (baseInfo != null) {
                yield return baseInfo;
                baseInfo = baseInfo.Base;
            }
        }

        public static IEnumerable<ITypeInfo> DomainSealedInfos(this ITypesInfo typesInfo,Type type){
            var typeInfo = typesInfo.FindTypeInfo(type);
            var infos = typeInfo.IsInterface ? typeInfo.Implementors : typeInfo.Descendants;
            var typeInfos = infos.Where(info => !info.IsAbstract).Reverse().ToArray();
            return typeInfos.Except(typeInfos.SelectMany(BaseInfos));
        }

        public static IEnumerable<ITypeInfo> DomainSealedInfos<T>(this ITypesInfo typesInfo){
            return typesInfo.DomainSealedInfos(typeof(T));
        }

        public static ITypeInfo GetTypeInfo(this object obj){
            return obj.GetType().GetTypeInfo();
        }

        public static ExpressApp.DC.TypeInfo ToTypeInfo(this ITypeInfo typeinfo) {
            return ((ExpressApp.DC.TypeInfo) typeinfo);
        }

        public static ExpressApp.DC.TypeInfo ToTypeInfo(this Type type) {
            return type.GetTypeInfo().ToTypeInfo();
        }

        public static ITypeInfo GetTypeInfo(this Type type){
            return XafTypesInfo.Instance.FindTypeInfo(type);
        }

        public static IModelClass ModelClass(this ITypeInfo typeInfo){
            return CaptionHelper.ApplicationModel.BOModel.GetClass(typeInfo.Type);
        }

        public static Type FindBussinessObjectType(this ITypesInfo typesInfo,Type type){
            if (!(type.IsInterface))
                return type;
            var implementors = typesInfo.FindTypeInfo(type).Implementors.ToArray();
            var objectType = implementors.FirstOrDefault();
            if (objectType == null)
                throw new ArgumentException("Add a business object that implements " +
                                            type.FullName + " at your AdditionalBusinessClasses (module.designer.cs)");
            if (implementors.Length > 1) {
                var typeInfos = implementors.Where(implementor => implementor.Base != null && !(type.IsAssignableFrom(implementor.Base.Type)));
                foreach (ITypeInfo implementor in typeInfos) {
                    return implementor.Type;
                }

                throw new ArgumentNullException("More than 1 objects implement " + type.FullName);
            }
            return objectType.Type;

        }

        public static Type FindBussinessObjectType<T>(this ITypesInfo typesInfo){
            return typesInfo.FindBussinessObjectType(typeof(T));
        }

        public static ITypeInfo FindTypeInfo<T>(this ITypesInfo typesInfo) {
            return typesInfo.FindTypeInfo(typeof(T));
        }



    }
}