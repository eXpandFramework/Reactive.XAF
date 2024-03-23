using System;
using System.Linq.Expressions;

namespace Xpand.Extensions.ExpressionExtensions {
    public static partial class ExpressionExtensions {
        public static InvocationExpression Invoke(this Expression expression, params Expression[] expressions)
            => Expression.Invoke(expression, expressions);
    }
}