using System;
using System.Linq.Expressions;
using Fasterflect;
using Xpand.Extensions.ExpressionExtensions;
using Xpand.Extensions.ObjectExtensions;

namespace Xpand.Extensions.ReflectionExtensions {
    public partial class ReflectionExtensions {
        public static bool SetIfDefault<T, TValue>(this T obj, Expression<Func<T, TValue>> memberName, Func<TValue> value) {
            var memberExpressionName = memberName.MemberExpressionName();
            var propertyValue = obj.GetPropertyValue(memberExpressionName);
            if (propertyValue.IsDefaultValue()) {
                obj.SetPropertyValue(memberExpressionName, value());
                return true;
            }

            return false;
        }

        public static bool SetIfDefault<T, TValue>(this T obj, Expression<Func<T, TValue>> memberName, TValue value) 
            => obj.SetIfDefault(memberName, () => value);
    }
}