using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Fasterflect;
using Xpand.Extensions.AssemblyExtensions;

namespace Xpand.Extensions.TypeExtensions {
    public static partial class TypeExtensions {
        public static Expression<Action> CallExpression(this Type type, MethodInfo method, params Expression[] arguments)
            => Expression.Lambda<Action>(Expression.Call(Expression.New(type), method, arguments));

        public static Expression<Action> CallExpression(this Type type, string method, params Expression[] arguments)
            => type.CallExpression(type.Method(method), arguments);
    }
}