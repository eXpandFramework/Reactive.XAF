using System;
using System.Collections.Generic;
using System.Reflection;

namespace Xpand.Source.Extensions.System.Refelction{
    internal static partial class ReflectionExtensions{
        public static readonly List<AccessModifier> AccessModifiers = new List<AccessModifier> {
            Refelction.AccessModifier.Private,
            Refelction.AccessModifier.Protected,
            Refelction.AccessModifier.ProtectedInternal,
            Refelction.AccessModifier.Internal,
            Refelction.AccessModifier.Public
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
                return Refelction.AccessModifier.Private;
            if (methodInfo.IsFamily)
                return Refelction.AccessModifier.Protected;
            if (methodInfo.IsFamilyOrAssembly)
                return Refelction.AccessModifier.ProtectedInternal;
            if (methodInfo.IsAssembly)
                return Refelction.AccessModifier.Internal;
            if (methodInfo.IsPublic)
                return Refelction.AccessModifier.Public;
            throw new ArgumentException("Did not find access modifier", nameof(methodInfo));
        }
    }

    internal enum AccessModifier{
        Private,
        Protected,
        ProtectedInternal,
        Internal,
        Public
    }
}