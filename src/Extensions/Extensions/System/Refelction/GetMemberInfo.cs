using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Xpand.Source.Extensions.System.Refelction{
    internal static partial class ReflectionExtensions{
        public static MemberInfo GetMemberInfo(this LambdaExpression lambda) {
            if (lambda == null) throw new ArgumentException("Not a lambda expression", nameof(lambda));
            MemberExpression memberExpr = null;
            switch (lambda.Body.NodeType){
                case ExpressionType.Convert:
                    memberExpr = ((UnaryExpression)lambda.Body).Operand as MemberExpression;
                    break;
                case ExpressionType.MemberAccess:
                    memberExpr = lambda.Body as MemberExpression;
                    break;
            }
            if (memberExpr == null) throw new ArgumentException("Not a member access", nameof(lambda));
            return memberExpr.Member;
        }

    }
}