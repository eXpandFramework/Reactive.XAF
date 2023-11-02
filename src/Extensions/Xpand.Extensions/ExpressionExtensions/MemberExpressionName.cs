using System;
using System.Linq.Expressions;

namespace Xpand.Extensions.ExpressionExtensions{
    public static partial class ExpressionExtensions{
        public static string MemberExpressionName<TObject, TMemberValue>(this Expression<Func<TObject, TMemberValue>> memberName) 
            => memberName.Body switch{
                MemberExpression memberExpression => memberExpression.Member.Name,
                UnaryExpression{ Operand: MemberExpression operand } => operand.Member.Name,
                _ => throw new ArgumentException("Invalid expression type", nameof(memberName))
            };

    }
}