using System;
using System.Linq.Expressions;
using Fasterflect;

namespace Xpand.Extensions.TypeExtensions {
    public static partial class TypeExtensions {
        public static Expression<Action> CallExpression(this Type type, string method)
            => Expression.Lambda<Action>(Expression.Call(Expression.New(type), type.Method(method)));
    }
}