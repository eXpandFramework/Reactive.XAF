using System;
using System.Collections.Generic;
using System.Reflection;

namespace Xpand.Extensions.Reflection{
    public static partial class ReflectionExtensions{
        public static readonly List<AccessModifier> AccessModifiers = new List<AccessModifier> {
            Reflection.AccessModifier.Private,
            Reflection.AccessModifier.Protected,
            Reflection.AccessModifier.ProtectedInternal,
            Reflection.AccessModifier.Internal,
            Reflection.AccessModifier.Public
        };

        public static AccessModifier AccessModifier(this PropertyInfo propertyInfo){
            if (propertyInfo.SetMethod == null)
                return propertyInfo.GetMethod.AccessModifier();
            if (propertyInfo.GetMethod == null)
                return propertyInfo.SetMethod.AccessModifier();
            var max = Math.Max(AccessModifiers.IndexOf(propertyInfo.GetMethod.AccessModifier()),
                AccessModifiers.IndexOf(propertyInfo.SetMethod.AccessModifier()));
            return AccessModifiers[max];
        }

        public static AccessModifier AccessModifier(this MethodInfo methodInfo){
            if (methodInfo.IsPrivate)
                return Reflection.AccessModifier.Private;
            if (methodInfo.IsFamily)
                return Reflection.AccessModifier.Protected;
            if (methodInfo.IsFamilyOrAssembly)
                return Reflection.AccessModifier.ProtectedInternal;
            if (methodInfo.IsAssembly)
                return Reflection.AccessModifier.Internal;
            if (methodInfo.IsPublic)
                return Reflection.AccessModifier.Public;
            throw new ArgumentException("Did not find access modifier", nameof(methodInfo));
        }
    }

    public enum AccessModifier{
        Private,
        Protected,
        ProtectedInternal,
        Internal,
        Public
    }
}