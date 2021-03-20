using System;
using System.Linq.Expressions;
using DevExpress.ExpressApp.DC;
using Xpand.Extensions.ExpressionExtensions;

namespace Xpand.Extensions.XAF.TypesInfoExtensions{
	public static partial class TypesInfoExtensions{
        public static IMemberInfo FindMember<T>(this ITypeInfo typeInfo,Expression<Func<T, object>> memberName) 
            => typeInfo.FindMember(memberName.MemberExpressionName());
	}
}