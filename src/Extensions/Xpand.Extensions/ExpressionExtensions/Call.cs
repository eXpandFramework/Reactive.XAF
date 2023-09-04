using System;
using System.Linq.Expressions;

namespace Xpand.Extensions.ExpressionExtensions {
    public static partial class ExpressionExtensions {
        public static MethodCallExpression Call(this Type type, string methodName, Type[] typeArguments,
            params Expression[] arguments)
            => Expression.Call(type, methodName, typeArguments, arguments);
    }
}