using System;
using System.Linq.Expressions;

namespace Xpand.Extensions.ExpressionExtensions {
    public static partial class ExpressionExtensions {
        public static ConstantExpression ConstantExpression(this object value, Type type)
            => Expression.Constant(value, type);
    }
}