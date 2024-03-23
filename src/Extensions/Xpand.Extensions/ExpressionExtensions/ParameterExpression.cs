using System;
using System.Linq.Expressions;

namespace Xpand.Extensions.ExpressionExtensions {
    public static partial class ExpressionExtensions {
        public static ParameterExpression ParameterExpression(this Type type, string name)
            => Expression.Parameter(type, name);
    }
}