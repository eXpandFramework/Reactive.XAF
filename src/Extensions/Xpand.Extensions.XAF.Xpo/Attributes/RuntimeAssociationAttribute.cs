using System;
using System.Collections.Generic;
using System.Reflection;
using DevExpress.ExpressApp.DC;
using DevExpress.Xpo;
using DevExpress.Xpo.Metadata;
using AggregatedAttribute = DevExpress.Xpo.AggregatedAttribute;

namespace Xpand.Extensions.XAF.Xpo.Attributes {
    public enum RuntimeRelationType {
        ManyToMany,
        OneToMany
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class RuntimeAssociationAttribute : Attribute {
        public RuntimeAssociationAttribute(string associationName, RuntimeRelationType runtimeRelationType)
            : this(associationName, null, runtimeRelationType, null) {
        }
        public RuntimeAssociationAttribute(string associationName)
            : this(associationName, null,RuntimeRelationType.OneToMany, null) {
        }


        public RuntimeAssociationAttribute(string associationName, string providedPropertyName, RuntimeRelationType runtimeRelationType,
            string attributesFactory="") {
            AssociationName = associationName;
            RelationType = runtimeRelationType;
            AttributesFactoryProperty = attributesFactory;
            ProvidedPropertyName = providedPropertyName;
        }

        public string AssociationName { get; }

        public string AttributesFactoryProperty { get; }

        public string ProvidedPropertyName { get; }


        public RuntimeRelationType RelationType { get; }

        public bool IsAggregated { get; set; }
    }

    internal static class RuntimeAssociationExtensions {
        internal static void AddExtraAttributes(this XPMemberInfo memberInfo, RuntimeAssociationAttribute providedAssociationAttribute, XPMemberInfo customMemberInfo) {
            if (!(string.IsNullOrEmpty(providedAssociationAttribute.AttributesFactoryProperty)))
                foreach (var attribute in memberInfo.Owner.GetAttributes(providedAssociationAttribute.AttributesFactoryProperty)) {
                    customMemberInfo.AddAttribute(attribute);
                }
        }

        
        internal static AssociationAttribute GetAssociationAttribute(this XPMemberInfo memberInfo, RuntimeAssociationAttribute providedAssociationAttribute) {
            var associationAttribute = memberInfo.FindAttributeInfo(typeof(AssociationAttribute)) as AssociationAttribute;
            if (associationAttribute == null && !string.IsNullOrEmpty(providedAssociationAttribute.AssociationName))
                associationAttribute = new AssociationAttribute(providedAssociationAttribute.AssociationName);
            else if (associationAttribute == null)
                throw new NullReferenceException(memberInfo + " has no association attribute");
            return associationAttribute;
        }

        static IEnumerable<Attribute> GetAttributes(this XPClassInfo owner,string attributesFactoryProperty) {
            PropertyInfo memberInfo = owner.ClassType.GetProperty(attributesFactoryProperty);
            return memberInfo != null ? (IEnumerable<Attribute>)memberInfo.GetValue(null, null) : new List<Attribute>();
        }

        internal static XPMemberInfo CreateMemberInfo(this ITypesInfo typesInfo, XPMemberInfo memberInfo, RuntimeAssociationAttribute runtimeAssociationAttribute, AssociationAttribute associationAttribute) {
            var typeToCreateOn = GetTypeToCreateOn(memberInfo, associationAttribute);
            if (typeToCreateOn == null)
                throw new NotImplementedException();
            XPMemberInfo member;
            if (!(memberInfo.IsNonAssociationList) || (memberInfo.IsNonAssociationList && runtimeAssociationAttribute.RelationType == RuntimeRelationType.ManyToMany)) {
                member = typesInfo.CreateCollection(typeToCreateOn, memberInfo.Owner.ClassType, associationAttribute.Name,
                    runtimeAssociationAttribute.ProvidedPropertyName ?? memberInfo.Owner.ClassType.Name + "s", false);
                if (runtimeAssociationAttribute.IsAggregated) {
                    member.AddAttribute(new AggregatedAttribute());
                }
            } else {
                member = typesInfo.CreateMember(typeToCreateOn, memberInfo.Owner.ClassType, associationAttribute.Name,
                    runtimeAssociationAttribute.ProvidedPropertyName ?? memberInfo.Owner.ClassType.Name, false);
            }

            if (!string.IsNullOrEmpty(runtimeAssociationAttribute.AssociationName) && !memberInfo.HasAttribute(typeof(AssociationAttribute)))
                memberInfo.AddAttribute(new AssociationAttribute(runtimeAssociationAttribute.AssociationName));
            typesInfo.RefreshInfo(typeToCreateOn);
            typesInfo.RefreshInfo(memberInfo.Owner.ClassType);

            return member;
        }

        private static Type GetTypeToCreateOn(XPMemberInfo memberInfo, AssociationAttribute associationAttribute) {
            return !memberInfo.MemberType.IsGenericType
                       ? (string.IsNullOrEmpty(associationAttribute.ElementTypeName)
                              ? memberInfo.MemberType
                              : Type.GetType(associationAttribute.ElementTypeName))
                       : memberInfo.MemberType.GetGenericArguments()[0];
        }    
    }
}