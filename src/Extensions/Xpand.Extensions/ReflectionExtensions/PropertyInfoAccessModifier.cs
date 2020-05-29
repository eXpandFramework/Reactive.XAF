using System;
using System.Collections.Generic;
using System.Reflection;

namespace Xpand.Extensions.ReflectionExtensions{
    public static partial class ReflectionExtensions{
        public static readonly List<AccessModifier> AccessModifiers = new List<AccessModifier> {
            Extensions.ReflectionExtensions.AccessModifier.Private,
            Extensions.ReflectionExtensions.AccessModifier.Protected,
            Extensions.ReflectionExtensions.AccessModifier.ProtectedInternal,
            Extensions.ReflectionExtensions.AccessModifier.Internal,
            Extensions.ReflectionExtensions.AccessModifier.Public
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
                return Extensions.ReflectionExtensions.AccessModifier.Private;
            if (methodInfo.IsFamily)
                return Extensions.ReflectionExtensions.AccessModifier.Protected;
            if (methodInfo.IsFamilyOrAssembly)
                return Extensions.ReflectionExtensions.AccessModifier.ProtectedInternal;
            if (methodInfo.IsAssembly)
                return Extensions.ReflectionExtensions.AccessModifier.Internal;
            if (methodInfo.IsPublic)
                return Extensions.ReflectionExtensions.AccessModifier.Public;
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