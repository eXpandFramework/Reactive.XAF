using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Xpand.Extensions.ExpressionExtensions {
    public static partial class ExpressionExtensions {
        public static MethodCallExpression MethodCallExpression(this MethodInfo methodInfo,params Expression[] expressions) 
            => Expression.Call(methodInfo, expressions);

        public static MethodCallExpression MethodCallExpression(this Type type, string methodName, Type[] typeArguments,
            params Expression[] arguments)
            => Expression.Call(type, methodName, typeArguments, arguments);
    }
}