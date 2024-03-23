using System.Linq.Expressions;

namespace Xpand.Extensions.ExpressionExtensions {
    public static partial class ExpressionExtensions {
        public static Expression<TDelegate> Lambda<TDelegate>(this Expression body,
            params ParameterExpression[] parameters)
            => Expression.Lambda<TDelegate>(body, parameters);
    }
}